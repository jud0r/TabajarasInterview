use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::{candidate_applications, candidates, positions};
use crate::handlers::positions::{PositionResponse, to_response as position_to_response};

/// Build the OpenAPI-aware router for the candidate endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_candidates))
        .routes(routes!(get_candidate))
        .routes(routes!(create_candidate))
        .routes(routes!(update_candidate))
        .routes(routes!(delete_candidate))
        .routes(routes!(get_candidate_positions))
}

#[derive(Serialize, ToSchema)]
pub struct CandidateResponse {
    pub id: i32,
    pub first_name: String,
    pub last_name: String,
    pub email: String,
    pub phone: Option<String>,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreateCandidateRequest {
    #[validate(length(min = 3, message = "first_name must be at least 3 characters"))]
    pub first_name: String,
    #[validate(length(min = 3, message = "last_name must be at least 3 characters"))]
    pub last_name: String,
    #[validate(email(message = "email must be a valid email address"))]
    pub email: String,
    pub phone: Option<String>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdateCandidateRequest {
    #[validate(length(min = 3, message = "first_name must be at least 3 characters"))]
    pub first_name: Option<String>,
    #[validate(length(min = 3, message = "last_name must be at least 3 characters"))]
    pub last_name: Option<String>,
    #[validate(email(message = "email must be a valid email address"))]
    pub email: Option<String>,
    pub phone: Option<String>,
}

pub fn to_response(model: candidates::Model) -> CandidateResponse {
    CandidateResponse {
        id: model.id,
        first_name: model.first_name,
        last_name: model.last_name,
        email: model.email,
        phone: model.phone,
        created_at: model.created_at,
        updated_at: model.updated_at,
    }
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "candidates",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List candidates", body = [CandidateResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_candidates(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<CandidateResponse>>, (StatusCode, &'static str)> {

    let candidates = candidates::Entity::find()
        .filter(candidates::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = candidates
        .into_iter()
        .map(|c| CandidateResponse {
            id: c.id,
            first_name: c.first_name,
            last_name: c.last_name,
            email: c.email,
            phone: c.phone,
            created_at: c.created_at,
            updated_at: c.updated_at,
        })
        .collect();

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "candidates",
    security(("bearer_auth" = [])),
    request_body = CreateCandidateRequest,
    responses(
        (status = 201, description = "Candidate created", body = CandidateResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn create_candidate(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Json(payload): Json<CreateCandidateRequest>,
) -> Result<(StatusCode, Json<CandidateResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let new_candidate = candidates::ActiveModel {
        first_name: Set(payload.first_name),
        last_name: Set(payload.last_name),
        email: Set(payload.email),
        phone: Set(payload.phone),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let candidate = new_candidate
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = CandidateResponse {
        id: candidate.id,
        first_name: candidate.first_name,
        last_name: candidate.last_name,
        email: candidate.email,
        phone: candidate.phone,
        created_at: candidate.created_at,
        updated_at: candidate.updated_at,
    };

    Ok((StatusCode::CREATED, Json(response)))
}

#[utoipa::path(
    get,
    path = "/get/{id}",
    tag = "candidates",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Candidate id")),
    responses(
        (status = 200, description = "Get candidate", body = CandidateResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Candidate not found")
    )
)]
#[axum::debug_handler]
pub async fn get_candidate(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<CandidateResponse>, (StatusCode, &'static str)> {

    let candidate = candidates::Entity::find_by_id(id)
        .filter(candidates::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Candidate not found"))?;

    let response = CandidateResponse {
        id: candidate.id,
        first_name: candidate.first_name,
        last_name: candidate.last_name,
        email: candidate.email,
        phone: candidate.phone,
        created_at: candidate.created_at,
        updated_at: candidate.updated_at,
    };

    Ok(Json(response))
}

#[utoipa::path(
    put,
    path = "/update/{id}",
    tag = "candidates",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Candidate id")),
    request_body = UpdateCandidateRequest,
    responses(
        (status = 200, description = "Candidate updated", body = CandidateResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Candidate not found")
    )
)]
#[axum::debug_handler]
pub async fn update_candidate(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
    Json(payload): Json<UpdateCandidateRequest>,
) -> Result<Json<CandidateResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let candidate = candidates::Entity::find_by_id(id)
        .filter(candidates::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Candidate not found".to_string()))?;

    let mut active_candidate: candidates::ActiveModel = candidate.into();

    if let Some(first_name) = payload.first_name {
        active_candidate.first_name = Set(first_name);
    }

    if let Some(last_name) = payload.last_name {
        active_candidate.last_name = Set(last_name);
    }

    if let Some(phone) = payload.phone {
        active_candidate.phone = Set(Some(phone));
    }

    if let Some(email) = payload.email {
        active_candidate.email = Set(email);
    }

    active_candidate.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let candidate = active_candidate
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = CandidateResponse {
        id: candidate.id,
        first_name: candidate.first_name,
        last_name: candidate.last_name,
        email: candidate.email,
        phone: candidate.phone,
        created_at: candidate.created_at,
        updated_at: candidate.updated_at,
    };

    Ok(Json(response))
}

#[utoipa::path(
    delete,
    path = "/delete/{id}",
    tag = "candidates",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Candidate id")),
    responses(
        (status = 204, description = "Candidate deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Candidate not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_candidate(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<StatusCode, (StatusCode, String)> {

    let candidate = candidates::Entity::find_by_id(id)
        .filter(candidates::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Candidate not found".to_string()))?;

    let mut active_candidate: candidates::ActiveModel = candidate.into();

    active_candidate.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_candidate
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}

#[utoipa::path(
    get,
    path = "/get/{id}/positions",
    tag = "candidates",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Candidate id")),
    responses(
        (status = 200, description = "List positions a candidate applied to", body = [PositionResponse]),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Candidate not found")
    )
)]
#[axum::debug_handler]
pub async fn get_candidate_positions(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<Vec<PositionResponse>>, (StatusCode, &'static str)> {

    candidates::Entity::find_by_id(id)
        .filter(candidates::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Candidate not found"))?;

    let applications = candidate_applications::Entity::find()
        .filter(candidate_applications::Column::CandidateId.eq(id))
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let position_ids: Vec<i32> = applications.iter().map(|a| a.position_id).collect();

    let positions = positions::Entity::find()
        .filter(positions::Column::Id.is_in(position_ids))
        .filter(positions::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = positions
        .into_iter()
        .map(position_to_response)
        .collect::<Result<Vec<_>, _>>()?;

    Ok(Json(response))
}
