use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use sea_orm::prelude::Decimal;
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::interview_questions;

/// Build the OpenAPI-aware router for the interview question endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_interview_questions))
        .routes(routes!(get_interview_question))
        .routes(routes!(create_interview_question))
        .routes(routes!(update_interview_question))
        .routes(routes!(delete_interview_question))
}

#[derive(Serialize, ToSchema)]
pub struct InterviewQuestionResponse {
    pub id: i32,
    pub interview_id: i32,
    pub question_id: i32,
    pub question_order: i32,
    pub score: Option<String>,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreateInterviewQuestionRequest {
    pub interview_id: i32,
    pub question_id: i32,
    pub question_order: i32,
    pub score: Option<String>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdateInterviewQuestionRequest {
    pub question_order: Option<i32>,
    pub score: Option<String>,
}

fn parse_score(value: &str) -> Result<Decimal, (StatusCode, String)> {
    value
        .parse::<Decimal>()
        .map_err(|_| (StatusCode::BAD_REQUEST, "score must be a valid decimal".to_string()))
}

pub fn to_response(model: interview_questions::Model) -> InterviewQuestionResponse {
    InterviewQuestionResponse {
        id: model.id,
        interview_id: model.interview_id,
        question_id: model.question_id,
        question_order: model.question_order,
        score: model.score.map(|d| d.to_string()),
        created_at: model.created_at,
        updated_at: model.updated_at,
    }
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "interview_questions",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List interview questions", body = [InterviewQuestionResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_interview_questions(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<InterviewQuestionResponse>>, (StatusCode, &'static str)> {

    let interview_questions = interview_questions::Entity::find()
        .filter(interview_questions::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = interview_questions.into_iter().map(to_response).collect();

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "interview_questions",
    security(("bearer_auth" = [])),
    request_body = CreateInterviewQuestionRequest,
    responses(
        (status = 201, description = "Interview question created", body = InterviewQuestionResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 409, description = "Question already added to this interview")
    )
)]
#[axum::debug_handler]
pub async fn create_interview_question(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Json(payload): Json<CreateInterviewQuestionRequest>,
) -> Result<(StatusCode, Json<InterviewQuestionResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    // Guard against the same question being added twice to the same interview
    // while an active (non-deleted) link already exists.
    let existing = interview_questions::Entity::find()
        .filter(interview_questions::Column::InterviewId.eq(payload.interview_id))
        .filter(interview_questions::Column::QuestionId.eq(payload.question_id))
        .filter(interview_questions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    if existing.is_some() {
        return Err((
            StatusCode::CONFLICT,
            "Question already added to this interview".to_string(),
        ));
    }

    let score = match payload.score {
        Some(s) => Some(parse_score(&s)?),
        None => None,
    };

    let new_interview_question = interview_questions::ActiveModel {
        interview_id: Set(payload.interview_id),
        question_id: Set(payload.question_id),
        question_order: Set(payload.question_order),
        score: Set(score),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let interview_question = new_interview_question
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok((StatusCode::CREATED, Json(to_response(interview_question))))
}

#[utoipa::path(
    get,
    path = "/get/{id}",
    tag = "interview_questions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview question id")),
    responses(
        (status = 200, description = "Get interview question", body = InterviewQuestionResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview question not found")
    )
)]
#[axum::debug_handler]
pub async fn get_interview_question(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<InterviewQuestionResponse>, (StatusCode, &'static str)> {

    let interview_question = interview_questions::Entity::find_by_id(id)
        .filter(interview_questions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview question not found"))?;

    Ok(Json(to_response(interview_question)))
}

#[utoipa::path(
    put,
    path = "/update/{id}",
    tag = "interview_questions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview question id")),
    request_body = UpdateInterviewQuestionRequest,
    responses(
        (status = 200, description = "Interview question updated", body = InterviewQuestionResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview question not found")
    )
)]
#[axum::debug_handler]
pub async fn update_interview_question(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
    Json(payload): Json<UpdateInterviewQuestionRequest>,
) -> Result<Json<InterviewQuestionResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let interview_question = interview_questions::Entity::find_by_id(id)
        .filter(interview_questions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview question not found".to_string()))?;

    let mut active_interview_question: interview_questions::ActiveModel = interview_question.into();

    if let Some(question_order) = payload.question_order {
        active_interview_question.question_order = Set(question_order);
    }

    if let Some(score) = payload.score {
        active_interview_question.score = Set(Some(parse_score(&score)?));
    }

    active_interview_question.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let interview_question = active_interview_question
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(Json(to_response(interview_question)))
}

#[utoipa::path(
    delete,
    path = "/delete/{id}",
    tag = "interview_questions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Interview question id")),
    responses(
        (status = 204, description = "Interview question deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Interview question not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_interview_question(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<StatusCode, (StatusCode, String)> {

    let interview_question = interview_questions::Entity::find_by_id(id)
        .filter(interview_questions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Interview question not found".to_string()))?;

    let mut active_interview_question: interview_questions::ActiveModel = interview_question.into();

    active_interview_question.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_interview_question
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}
