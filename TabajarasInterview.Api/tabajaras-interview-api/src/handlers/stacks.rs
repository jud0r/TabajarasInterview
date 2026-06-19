use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::stacks;

/// Build the OpenAPI-aware router for the stack endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_stacks))
        .routes(routes!(get_stack))
        .routes(routes!(create_stack))
        .routes(routes!(update_stack))
        .routes(routes!(delete_stack))
}

#[derive(Serialize, ToSchema)]
pub struct StackResponse {
    pub id: i32,
    pub name: String,
    pub description: Option<String>,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreateStackRequest {
    #[validate(length(min = 1, message = "name must not be empty"))]
    pub name: String,
    pub description: Option<String>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdateStackRequest {
    #[validate(length(min = 1, message = "name must not be empty"))]
    pub name: Option<String>,
    pub description: Option<String>,
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "stacks",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List stacks", body = [StackResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_stacks(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<StackResponse>>, (StatusCode, &'static str)> {

    let stacks = stacks::Entity::find()
        .filter(stacks::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = stacks
        .into_iter()
        .map(|s| StackResponse {
            id: s.id,
            name: s.name,
            description: s.description,
            created_at: s.created_at,
            updated_at: s.updated_at,
        })
        .collect();

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "stacks",
    security(("bearer_auth" = [])),
    request_body = CreateStackRequest,
    responses(
        (status = 201, description = "Stack created", body = StackResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn create_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Json(payload): Json<CreateStackRequest>,
) -> Result<(StatusCode, Json<StackResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let new_stack = stacks::ActiveModel {
        name: Set(payload.name),
        description: Set(payload.description),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let stack = new_stack
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = StackResponse {
        id: stack.id,
        name: stack.name,
        description: stack.description,
        created_at: stack.created_at,
        updated_at: stack.updated_at,
    };

    Ok((StatusCode::CREATED, Json(response)))
}

#[utoipa::path(
    get,
    path = "/get/{id}",
    tag = "stacks",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Stack id")),
    responses(
        (status = 200, description = "Get stack", body = StackResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Stack not found")
    )
)]
#[axum::debug_handler]
pub async fn get_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<StackResponse>, (StatusCode, &'static str)> {

    let stack = stacks::Entity::find_by_id(id)
        .filter(stacks::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Stack not found"))?;

    let response = StackResponse {
        id: stack.id,
        name: stack.name,
        description: stack.description,
        created_at: stack.created_at,
        updated_at: stack.updated_at,
    };

    Ok(Json(response))
}

#[utoipa::path(
    put,
    path = "/update/{id}",
    tag = "stacks",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Stack id")),
    request_body = UpdateStackRequest,
    responses(
        (status = 200, description = "Stack updated", body = StackResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Stack not found")
    )
)]
#[axum::debug_handler]
pub async fn update_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
    Json(payload): Json<UpdateStackRequest>,
) -> Result<Json<StackResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let stack = stacks::Entity::find_by_id(id)
        .filter(stacks::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Stack not found".to_string()))?;

    let mut active_stack: stacks::ActiveModel = stack.into();

    if let Some(name) = payload.name {
        active_stack.name = Set(name);
    }

    if let Some(description) = payload.description {
        active_stack.description = Set(Some(description));
    }

    active_stack.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let stack = active_stack
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = StackResponse {
        id: stack.id,
        name: stack.name,
        description: stack.description,
        created_at: stack.created_at,
        updated_at: stack.updated_at,
    };

    Ok(Json(response))
}

#[utoipa::path(
    delete,
    path = "/delete/{id}",
    tag = "stacks",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Stack id")),
    responses(
        (status = 204, description = "Stack deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Stack not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<StatusCode, (StatusCode, String)> {

    let stack = stacks::Entity::find_by_id(id)
        .filter(stacks::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Stack not found".to_string()))?;

    let mut active_stack: stacks::ActiveModel = stack.into();

    active_stack.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_stack
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}
