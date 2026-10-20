use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use sea_orm::prelude::Decimal;
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::interviews;

/// Build the OpenAPI-aware router for the interview endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_interviews))
        .routes(routes!(get_interview))
        .routes(routes!(create_interview))
        .routes(routes!(update_interview))
        .routes(routes!(delete_interview))
}

/// Possible types for an interview.
///
/// The serialized snake_case value is what gets stored in the
/// `interviews.interview_type` column.
#[derive(Serialize, Deserialize, Clone, Copy, Debug, PartialEq, Eq, ToSchema)]
#[serde(rename_all = "snake_case")]
pub enum InterviewType {
    Screening,
    Technical,
    Behavioral,
    Final,
}

impl InterviewType {
    /// Returns the canonical string stored in the database.
    pub fn as_str(self) -> &'static str {
        match self {
            InterviewType::Screening => "Screening",
            InterviewType::Technical => "Technical",
            InterviewType::Behavioral => "Behavioral",
            InterviewType::Final => "Final",
        }
    }
}

impl std::str::FromStr for InterviewType {
    type Err = String;

    fn from_str(value: &str) -> Result<Self, Self::Err> {
        match value {
            "Screening" => Ok(InterviewType::Screening),
            "Technical" => Ok(InterviewType::Technical),
            "Behavioral" => Ok(InterviewType::Behavioral),
            "Final" => Ok(InterviewType::Final),
            other => Err(other.to_string()),
        }
    }
}

/// Possible statuses for an interview.
///
/// The serialized snake_case value is what gets stored in the
/// `interviews.status` column.
#[derive(Serialize, Deserialize, Clone, Copy, Debug, PartialEq, Eq, ToSchema)]
#[serde(rename_all = "snake_case")]
pub enum InterviewStatus {
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
    Rescheduled,
}

impl InterviewStatus {
    /// Returns the canonical string stored in the database.
    pub fn as_str(self) -> &'static str {
        match self {
            InterviewStatus::Scheduled => "Scheduled",
            InterviewStatus::InProgress => "InProgress",
            InterviewStatus::Completed => "Completed",
            InterviewStatus::Cancelled => "Cancelled",
            InterviewStatus::NoShow => "NoShow",
            InterviewStatus::Rescheduled => "Rescheduled",
        }
    }
}

impl std::str::FromStr for InterviewStatus {
    type Err = String;

    fn from_str(value: &str) -> Result<Self, Self::Err> {
        match value {
            "Scheduled" => Ok(InterviewStatus::Scheduled),
            "InProgress" => Ok(InterviewStatus::InProgress),
            "Completed" => Ok(InterviewStatus::Completed),
            "Cancelled" => Ok(InterviewStatus::Cancelled),
            "NoShow" => Ok(InterviewStatus::NoShow),
            "Rescheduled" => Ok(InterviewStatus::Rescheduled),
            other => Err(other.to_string()),
        }
    }
}

#[derive(Serialize, ToSchema)]
pub struct InterviewResponse {
    pub id: i32,
    pub candidate_application_id: i32,
    pub interviewer_id: i32,
    pub name: String,
    pub interview_type: InterviewType,
    pub status: InterviewStatus,
    pub score: Option<String>,
    pub comments: Option<String>,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreateInterviewRequest {
    pub candidate_application_id: i32,
    pub interviewer_id: i32,
    #[validate(length(min = 1, message = "name must not be empty"))]
    pub name: String,
    pub interview_type: InterviewType,
    pub status: InterviewStatus,
    pub score: Option<String>,
    pub comments: Option<String>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdateInterviewRequest {
    #[validate(length(min = 1, message = "name must not be empty"))]
    pub name: Option<String>,
    pub interview_type: Option<InterviewType>,
    pub status: Option<InterviewStatus>,
    pub score: Option<String>,
    pub comments: Option<String>,
}

fn parse_score(value: &str) -> Result<Decimal, (StatusCode, String)> {
    value
        .parse::<Decimal>()
        .map_err(|_| (StatusCode::BAD_REQUEST, "score must be a valid decimal".to_string()))
}

pub fn to_response(model: interviews::Model) -> Result<InterviewResponse, (StatusCode, &'static str)> {
    Ok(InterviewResponse {
        id: model.id,
        candidate_application_id: model.candidate_application_id,
        interviewer_id: model.interviewer_id,
        name: model.name,
        interview_type: model.interview_type.parse::<InterviewType>()
            .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Unknown interview type"))?,
        status: model.status.parse::<InterviewStatus>()
            .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Unknown interview status"))?,
        score: model.score.map(|d| d.to_string()),
        comments: model.comments,
        created_at: model.created_at,
        updated_at: model.updated_at,
    })
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "interviews",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List interviews", body = [InterviewResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_interviews(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<InterviewResponse>>, (StatusCode, &'static str)> {

    let interviews = interviews::Entity::find()
        .filter(interviews::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = interviews
        .into_iter()
        .map(to_response)
        .collect::<Result<Vec<_>, _>>()?;

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "interviews",
    security(("bearer_auth" = [])),
    request_body = CreateInterviewRequest,
    responses(
        (status = 201, description = "Interview created", body = InterviewResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn create_interview(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Json(payload): Json<CreateInterviewRequest>,
) -> Result<(StatusCode, Json<InterviewResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let score = match payload.score {
        Some(s) => Some(parse_score(&s)?),
        None => None,
    };

    let new_interview = interviews::ActiveModel {
        candidate_application_id: Set(payload.candidate_application_id),
        interviewer_id: Set(payload.interviewer_id),
        name: Set(payload.name),
        interview_type: Set(payload.interview_type.as_str().to_string()),
        status: Set(payload.status.as_str().to_string()),
        score: Set(score),
        comments: Set(payload.comments),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let interview = new_interview
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = to_response(interview).map_err(|(s, m)| (s, m.to_string()))?;

    Ok((StatusCode::CREATED, Json(response)))
}

#[utoipa::path(
    get,
    path = "/get/{id}",
    tag = "interviews",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview id")),
    responses(
        (status = 200, description = "Get interview", body = InterviewResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview not found")
    )
)]
#[axum::debug_handler]
pub async fn get_interview(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<InterviewResponse>, (StatusCode, &'static str)> {

    let interview = interviews::Entity::find_by_id(id)
        .filter(interviews::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview not found"))?;

    Ok(Json(to_response(interview)?))
}

#[utoipa::path(
    put,
    path = "/update/{id}",
    tag = "interviews",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview id")),
    request_body = UpdateInterviewRequest,
    responses(
        (status = 200, description = "Interview updated", body = InterviewResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview not found")
    )
)]
#[axum::debug_handler]
pub async fn update_interview(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
    Json(payload): Json<UpdateInterviewRequest>,
) -> Result<Json<InterviewResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let interview = interviews::Entity::find_by_id(id)
        .filter(interviews::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview not found".to_string()))?;

    let mut active_interview: interviews::ActiveModel = interview.into();

    if let Some(name) = payload.name {
        active_interview.name = Set(name);
    }

    if let Some(interview_type) = payload.interview_type {
        active_interview.interview_type = Set(interview_type.as_str().to_string());
    }

    if let Some(status) = payload.status {
        active_interview.status = Set(status.as_str().to_string());
    }

    if let Some(score) = payload.score {
        active_interview.score = Set(Some(parse_score(&score)?));
    }

    if let Some(comments) = payload.comments {
        active_interview.comments = Set(Some(comments));
    }

    active_interview.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let interview = active_interview
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = to_response(interview).map_err(|(s, m)| (s, m.to_string()))?;

    Ok(Json(response))
}

#[utoipa::path(
    delete,
    path = "/delete/{id}",
    tag = "interviews",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview id")),
    responses(
        (status = 204, description = "Interview deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_interview(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<StatusCode, (StatusCode, String)> {

    let interview = interviews::Entity::find_by_id(id)
        .filter(interviews::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview not found".to_string()))?;

    let mut active_interview: interviews::ActiveModel = interview.into();

    active_interview.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_interview
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}
