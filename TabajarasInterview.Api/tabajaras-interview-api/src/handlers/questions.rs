use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::questions;

/// Build the OpenAPI-aware router for the question endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_questions))
        .routes(routes!(get_questions_by_stack))
        .routes(routes!(get_question))
        .routes(routes!(create_question))
        .routes(routes!(update_question))
        .routes(routes!(delete_question))
}

#[derive(Serialize, ToSchema)]
pub struct QuestionResponse {
    pub id: i32,
    pub stack_id: i32,
    pub question: String,
    pub acceptable_answer: Option<String>,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreateQuestionRequest {
    pub stack_id: i32,
    #[validate(length(min = 1, message = "question must not be empty"))]
    pub question: String,
    pub acceptable_answer: Option<String>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdateQuestionRequest {
    pub stack_id: Option<i32>,
    #[validate(length(min = 1, message = "question must not be empty"))]
    pub question: Option<String>,
    pub acceptable_answer: Option<String>,
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "questions",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List questions", body = [QuestionResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_questions(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<QuestionResponse>>, (StatusCode, &'static str)> {

    let questions = questions::Entity::find()
        .filter(questions::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = questions
        .into_iter()
        .map(|q| QuestionResponse {
            id: q.id,
            stack_id: q.stack_id,
            question: q.question,
            acceptable_answer: q.acceptable_answer,
            created_at: q.created_at,
            updated_at: q.updated_at,
        })
        .collect();

    Ok(Json(response))
}

#[utoipa::path(
    get,
    path = "/by_stack/{stack_id}",
    tag = "questions",
    security(("bearer_auth" = [])),
    params(("stack_id" = i32, Path, description = "Stack id")),
    responses(
        (status = 200, description = "List questions for a stack", body = [QuestionResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_questions_by_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(stack_id): Path<i32>,
) -> Result<Json<Vec<QuestionResponse>>, (StatusCode, &'static str)> {

    let questions = questions::Entity::find()
        .filter(questions::Column::StackId.eq(stack_id))
        .filter(questions::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = questions
        .into_iter()
        .map(|q| QuestionResponse {
            id: q.id,
            stack_id: q.stack_id,
            question: q.question,
            acceptable_answer: q.acceptable_answer,
            created_at: q.created_at,
            updated_at: q.updated_at,
        })
        .collect();

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "questions",
    security(("bearer_auth" = [])),
    request_body = CreateQuestionRequest,
    responses(
        (status = 201, description = "Question created", body = QuestionResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn create_question(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Json(payload): Json<CreateQuestionRequest>,
) -> Result<(StatusCode, Json<QuestionResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let new_question = questions::ActiveModel {
        stack_id: Set(payload.stack_id),
        question: Set(payload.question),
        acceptable_answer: Set(payload.acceptable_answer),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let question = new_question
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = QuestionResponse {
        id: question.id,
        stack_id: question.stack_id,
        question: question.question,
        acceptable_answer: question.acceptable_answer,
        created_at: question.created_at,
        updated_at: question.updated_at,
    };

    Ok((StatusCode::CREATED, Json(response)))
}

#[utoipa::path(
    get,
    path = "/get/{id}",
    tag = "questions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Question id")),
    responses(
        (status = 200, description = "Get question", body = QuestionResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Question not found")
    )
)]
#[axum::debug_handler]
pub async fn get_question(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<QuestionResponse>, (StatusCode, &'static str)> {

    let question = questions::Entity::find_by_id(id)
        .filter(questions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Question not found"))?;

    let response = QuestionResponse {
        id: question.id,
        stack_id: question.stack_id,
        question: question.question,
        acceptable_answer: question.acceptable_answer,
        created_at: question.created_at,
        updated_at: question.updated_at,
    };

    Ok(Json(response))
}

#[utoipa::path(
    put,
    path = "/update/{id}",
    tag = "questions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Question id")),
    request_body = UpdateQuestionRequest,
    responses(
        (status = 200, description = "Question updated", body = QuestionResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Question not found")
    )
)]
#[axum::debug_handler]
pub async fn update_question(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
    Json(payload): Json<UpdateQuestionRequest>,
) -> Result<Json<QuestionResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let question = questions::Entity::find_by_id(id)
        .filter(questions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Question not found".to_string()))?;

    let mut active_question: questions::ActiveModel = question.into();

    if let Some(stack_id) = payload.stack_id {
        active_question.stack_id = Set(stack_id);
    }

    if let Some(question) = payload.question {
        active_question.question = Set(question);
    }

    if let Some(acceptable_answer) = payload.acceptable_answer {
        active_question.acceptable_answer = Set(Some(acceptable_answer));
    }

    active_question.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let question = active_question
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = QuestionResponse {
        id: question.id,
        stack_id: question.stack_id,
        question: question.question,
        acceptable_answer: question.acceptable_answer,
        created_at: question.created_at,
        updated_at: question.updated_at,
    };

    Ok(Json(response))
}

#[utoipa::path(
    delete,
    path = "/delete/{id}",
    tag = "questions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Question id")),
    responses(
        (status = 204, description = "Question deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Question not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_question(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<StatusCode, (StatusCode, String)> {

    let question = questions::Entity::find_by_id(id)
        .filter(questions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Question not found".to_string()))?;

    let mut active_question: questions::ActiveModel = question.into();

    active_question.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_question
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}
