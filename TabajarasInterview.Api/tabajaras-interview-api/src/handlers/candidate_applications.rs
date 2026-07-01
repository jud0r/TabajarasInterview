use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use sea_orm::prelude::Decimal;
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::candidate_applications;

/// Build the OpenAPI-aware router for the candidate application endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_candidate_applications))
        .routes(routes!(get_candidate_applications_by_candidate))
        .routes(routes!(get_candidate_applications_by_position))
        .routes(routes!(get_candidate_application))
        .routes(routes!(create_candidate_application))
        .routes(routes!(update_candidate_application))
        .routes(routes!(delete_candidate_application))
}

/// Possible statuses for a candidate application.
///
/// The string value is what gets stored in the
/// `candidate_applications.status` column.
#[derive(Serialize, Deserialize, Clone, Copy, Debug, PartialEq, Eq, ToSchema)]
#[serde(rename_all = "snake_case")]
pub enum CandidateApplicationStatus {
    Applied,
    Screening,
    Interviewing,
    OfferExtended,
    Hired,
    Rejected,
    Withdrawn,
}

impl CandidateApplicationStatus {
    /// Returns the canonical string stored in the database.
    pub fn as_str(self) -> &'static str {
        match self {
            CandidateApplicationStatus::Applied => "Applied",
            CandidateApplicationStatus::Screening => "Screening",
            CandidateApplicationStatus::Interviewing => "Interviewing",
            CandidateApplicationStatus::OfferExtended => "OfferExtended",
            CandidateApplicationStatus::Hired => "Hired",
            CandidateApplicationStatus::Rejected => "Rejected",
            CandidateApplicationStatus::Withdrawn => "Withdrawn",
        }
    }
}

impl std::str::FromStr for CandidateApplicationStatus {
    type Err = String;

    fn from_str(value: &str) -> Result<Self, Self::Err> {
        match value {
            "Applied" => Ok(CandidateApplicationStatus::Applied),
            "Screening" => Ok(CandidateApplicationStatus::Screening),
            "Interviewing" => Ok(CandidateApplicationStatus::Interviewing),
            "OfferExtended" => Ok(CandidateApplicationStatus::OfferExtended),
            "Hired" => Ok(CandidateApplicationStatus::Hired),
            "Rejected" => Ok(CandidateApplicationStatus::Rejected),
            "Withdrawn" => Ok(CandidateApplicationStatus::Withdrawn),
            other => Err(other.to_string()),
        }
    }
}

#[derive(Serialize, ToSchema)]
pub struct CandidateApplicationResponse {
    pub id: i32,
    pub candidate_id: i32,
    pub position_id: i32,
    pub status: CandidateApplicationStatus,
    pub started_at: sea_orm::prelude::DateTime,
    pub finished_at: Option<sea_orm::prelude::DateTime>,
    pub final_score: Option<String>,
    pub final_comments: Option<String>,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreateCandidateApplicationRequest {
    pub candidate_id: i32,
    pub position_id: i32,
    pub status: CandidateApplicationStatus,
    pub started_at: Option<sea_orm::prelude::DateTime>,
    pub finished_at: Option<sea_orm::prelude::DateTime>,
    pub final_score: Option<String>,
    pub final_comments: Option<String>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdateCandidateApplicationRequest {
    pub status: Option<CandidateApplicationStatus>,
    pub started_at: Option<sea_orm::prelude::DateTime>,
    pub finished_at: Option<sea_orm::prelude::DateTime>,
    pub final_score: Option<String>,
    pub final_comments: Option<String>,
}

fn parse_score(value: &str) -> Result<Decimal, (StatusCode, String)> {
    value
        .parse::<Decimal>()
        .map_err(|_| (StatusCode::BAD_REQUEST, "final_score must be a valid decimal".to_string()))
}

fn to_response(model: candidate_applications::Model) -> Result<CandidateApplicationResponse, (StatusCode, &'static str)> {
    Ok(CandidateApplicationResponse {
        id: model.id,
        candidate_id: model.candidate_id,
        position_id: model.position_id,
        status: model.status.parse::<CandidateApplicationStatus>()
            .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Unknown candidate application status"))?,
        started_at: model.started_at,
        finished_at: model.finished_at,
        final_score: model.final_score.map(|d| d.to_string()),
        final_comments: model.final_comments,
        created_at: model.created_at,
        updated_at: model.updated_at,
    })
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "candidate_applications",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List candidate applications", body = [CandidateApplicationResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_candidate_applications(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<CandidateApplicationResponse>>, (StatusCode, &'static str)> {

    let applications = candidate_applications::Entity::find()
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = applications
        .into_iter()
        .map(to_response)
        .collect::<Result<Vec<_>, _>>()?;

    Ok(Json(response))
}

#[utoipa::path(
    get,
    path = "/by_candidate/{candidate_id}",
    tag = "candidate_applications",
    security(("bearer_auth" = [])),
    params(("candidate_id" = i32, Path, description = "Candidate id")),
    responses(
        (status = 200, description = "List candidate applications for a candidate", body = [CandidateApplicationResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_candidate_applications_by_candidate(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(candidate_id): Path<i32>,
) -> Result<Json<Vec<CandidateApplicationResponse>>, (StatusCode, &'static str)> {

    let applications = candidate_applications::Entity::find()
        .filter(candidate_applications::Column::CandidateId.eq(candidate_id))
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = applications
        .into_iter()
        .map(to_response)
        .collect::<Result<Vec<_>, _>>()?;

    Ok(Json(response))
}

#[utoipa::path(
    get,
    path = "/by_position/{position_id}",
    tag = "candidate_applications",
    security(("bearer_auth" = [])),
    params(("position_id" = i32, Path, description = "Position id")),
    responses(
        (status = 200, description = "List candidate applications for a position", body = [CandidateApplicationResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_candidate_applications_by_position(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(position_id): Path<i32>,
) -> Result<Json<Vec<CandidateApplicationResponse>>, (StatusCode, &'static str)> {

    let applications = candidate_applications::Entity::find()
        .filter(candidate_applications::Column::PositionId.eq(position_id))
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = applications
        .into_iter()
        .map(to_response)
        .collect::<Result<Vec<_>, _>>()?;

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "candidate_applications",
    security(("bearer_auth" = [])),
    request_body = CreateCandidateApplicationRequest,
    responses(
        (status = 201, description = "Candidate application created", body = CandidateApplicationResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 409, description = "Candidate already applied to this position")
    )
)]
#[axum::debug_handler]
pub async fn create_candidate_application(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Json(payload): Json<CreateCandidateApplicationRequest>,
) -> Result<(StatusCode, Json<CandidateApplicationResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    // Guard against a candidate applying to the same position more than once
    // while an active (non-deleted) application already exists.
    let existing = candidate_applications::Entity::find()
        .filter(candidate_applications::Column::CandidateId.eq(payload.candidate_id))
        .filter(candidate_applications::Column::PositionId.eq(payload.position_id))
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    if existing.is_some() {
        return Err((
            StatusCode::CONFLICT,
            "Candidate already applied to this position".to_string(),
        ));
    }

    let final_score = match payload.final_score {
        Some(s) => Some(parse_score(&s)?),
        None => None,
    };

    let new_application = candidate_applications::ActiveModel {
        candidate_id: Set(payload.candidate_id),
        position_id: Set(payload.position_id),
        status: Set(payload.status.as_str().to_string()),
        started_at: Set(payload.started_at.unwrap_or_else(|| chrono::Utc::now().naive_utc())),
        finished_at: Set(payload.finished_at),
        final_score: Set(final_score),
        final_comments: Set(payload.final_comments),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let application = new_application
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = to_response(application).map_err(|(s, m)| (s, m.to_string()))?;

    Ok((StatusCode::CREATED, Json(response)))
}

#[utoipa::path(
    get,
    path = "/get/{id}",
    tag = "candidate_applications",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Candidate application id")),
    responses(
        (status = 200, description = "Get candidate application", body = CandidateApplicationResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Candidate application not found")
    )
)]
#[axum::debug_handler]
pub async fn get_candidate_application(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<CandidateApplicationResponse>, (StatusCode, &'static str)> {

    let application = candidate_applications::Entity::find_by_id(id)
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Candidate application not found"))?;

    Ok(Json(to_response(application)?))
}

#[utoipa::path(
    put,
    path = "/update/{id}",
    tag = "candidate_applications",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Candidate application id")),
    request_body = UpdateCandidateApplicationRequest,
    responses(
        (status = 200, description = "Candidate application updated", body = CandidateApplicationResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Candidate application not found")
    )
)]
#[axum::debug_handler]
pub async fn update_candidate_application(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
    Json(payload): Json<UpdateCandidateApplicationRequest>,
) -> Result<Json<CandidateApplicationResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let application = candidate_applications::Entity::find_by_id(id)
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Candidate application not found".to_string()))?;

    let mut active_application: candidate_applications::ActiveModel = application.into();

    if let Some(status) = payload.status {
        active_application.status = Set(status.as_str().to_string());
    }

    if let Some(started_at) = payload.started_at {
        active_application.started_at = Set(started_at);
    }

    if let Some(finished_at) = payload.finished_at {
        active_application.finished_at = Set(Some(finished_at));
    }

    if let Some(final_score) = payload.final_score {
        active_application.final_score = Set(Some(parse_score(&final_score)?));
    }

    if let Some(final_comments) = payload.final_comments {
        active_application.final_comments = Set(Some(final_comments));
    }

    active_application.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let application = active_application
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = to_response(application).map_err(|(s, m)| (s, m.to_string()))?;

    Ok(Json(response))
}

#[utoipa::path(
    delete,
    path = "/delete/{id}",
    tag = "candidate_applications",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Candidate application id")),
    responses(
        (status = 204, description = "Candidate application deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Candidate application not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_candidate_application(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<StatusCode, (StatusCode, String)> {

    let application = candidate_applications::Entity::find_by_id(id)
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Candidate application not found".to_string()))?;

    let mut active_application: candidate_applications::ActiveModel = application.into();

    active_application.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_application
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}
