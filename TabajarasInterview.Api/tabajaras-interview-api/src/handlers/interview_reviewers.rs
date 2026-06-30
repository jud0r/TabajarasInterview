use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use sea_orm::prelude::Decimal;
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::interview_reviewers;

/// Build the OpenAPI-aware router for the interview reviewer endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_interview_reviewers))
        .routes(routes!(get_interview_reviewer))
        .routes(routes!(create_interview_reviewer))
        .routes(routes!(update_interview_reviewer))
        .routes(routes!(delete_interview_reviewer))
}

#[derive(Serialize, ToSchema)]
pub struct InterviewReviewerResponse {
    pub id: i32,
    pub interview_id: i32,
    pub reviewer_id: i32,
    pub review_score: Option<String>,
    pub review_comments: Option<String>,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreateInterviewReviewerRequest {
    pub interview_id: i32,
    pub reviewer_id: i32,
    pub review_score: Option<String>,
    pub review_comments: Option<String>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdateInterviewReviewerRequest {
    pub review_score: Option<String>,
    pub review_comments: Option<String>,
}

fn parse_score(value: &str) -> Result<Decimal, (StatusCode, String)> {
    value
        .parse::<Decimal>()
        .map_err(|_| (StatusCode::BAD_REQUEST, "review_score must be a valid decimal".to_string()))
}

pub fn to_response(model: interview_reviewers::Model) -> InterviewReviewerResponse {
    InterviewReviewerResponse {
        id: model.id,
        interview_id: model.interview_id,
        reviewer_id: model.reviewer_id,
        review_score: model.review_score.map(|d| d.to_string()),
        review_comments: model.review_comments,
        created_at: model.created_at,
        updated_at: model.updated_at,
    }
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "interview_reviewers",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List interview reviewers", body = [InterviewReviewerResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_interview_reviewers(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<InterviewReviewerResponse>>, (StatusCode, &'static str)> {

    let interview_reviewers = interview_reviewers::Entity::find()
        .filter(interview_reviewers::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = interview_reviewers.into_iter().map(to_response).collect();

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "interview_reviewers",
    security(("bearer_auth" = [])),
    request_body = CreateInterviewReviewerRequest,
    responses(
        (status = 201, description = "Interview reviewer created", body = InterviewReviewerResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 409, description = "Reviewer already assigned to this interview")
    )
)]
#[axum::debug_handler]
pub async fn create_interview_reviewer(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Json(payload): Json<CreateInterviewReviewerRequest>,
) -> Result<(StatusCode, Json<InterviewReviewerResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    // Guard against the same reviewer being assigned twice to the same interview
    // while an active (non-deleted) assignment already exists.
    let existing = interview_reviewers::Entity::find()
        .filter(interview_reviewers::Column::InterviewId.eq(payload.interview_id))
        .filter(interview_reviewers::Column::ReviewerId.eq(payload.reviewer_id))
        .filter(interview_reviewers::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    if existing.is_some() {
        return Err((
            StatusCode::CONFLICT,
            "Reviewer already assigned to this interview".to_string(),
        ));
    }

    let review_score = match payload.review_score {
        Some(s) => Some(parse_score(&s)?),
        None => None,
    };

    let new_interview_reviewer = interview_reviewers::ActiveModel {
        interview_id: Set(payload.interview_id),
        reviewer_id: Set(payload.reviewer_id),
        review_score: Set(review_score),
        review_comments: Set(payload.review_comments),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let interview_reviewer = new_interview_reviewer
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok((StatusCode::CREATED, Json(to_response(interview_reviewer))))
}

#[utoipa::path(
    get,
    path = "/get/{id}",
    tag = "interview_reviewers",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview reviewer id")),
    responses(
        (status = 200, description = "Get interview reviewer", body = InterviewReviewerResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview reviewer not found")
    )
)]
#[axum::debug_handler]
pub async fn get_interview_reviewer(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<InterviewReviewerResponse>, (StatusCode, &'static str)> {

    let interview_reviewer = interview_reviewers::Entity::find_by_id(id)
        .filter(interview_reviewers::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview reviewer not found"))?;

    Ok(Json(to_response(interview_reviewer)))
}

#[utoipa::path(
    put,
    path = "/update/{id}",
    tag = "interview_reviewers",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview reviewer id")),
    request_body = UpdateInterviewReviewerRequest,
    responses(
        (status = 200, description = "Interview reviewer updated", body = InterviewReviewerResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview reviewer not found")
    )
)]
#[axum::debug_handler]
pub async fn update_interview_reviewer(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
    Json(payload): Json<UpdateInterviewReviewerRequest>,
) -> Result<Json<InterviewReviewerResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let interview_reviewer = interview_reviewers::Entity::find_by_id(id)
        .filter(interview_reviewers::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview reviewer not found".to_string()))?;

    let mut active_interview_reviewer: interview_reviewers::ActiveModel = interview_reviewer.into();

    if let Some(review_score) = payload.review_score {
        active_interview_reviewer.review_score = Set(Some(parse_score(&review_score)?));
    }

    if let Some(review_comments) = payload.review_comments {
        active_interview_reviewer.review_comments = Set(Some(review_comments));
    }

    active_interview_reviewer.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let interview_reviewer = active_interview_reviewer
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(Json(to_response(interview_reviewer)))
}

#[utoipa::path(
    delete,
    path = "/delete/{id}",
    tag = "interview_reviewers",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview reviewer id")),
    responses(
        (status = 204, description = "Interview reviewer deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview reviewer not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_interview_reviewer(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<StatusCode, (StatusCode, String)> {

    let interview_reviewer = interview_reviewers::Entity::find_by_id(id)
        .filter(interview_reviewers::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview reviewer not found".to_string()))?;

    let mut active_interview_reviewer: interview_reviewers::ActiveModel = interview_reviewer.into();

    active_interview_reviewer.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_interview_reviewer
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}
