use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::position_stacks;

/// Build the OpenAPI-aware router for the position stack (join table) endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_position_stacks))
        .routes(routes!(get_position_stack))
        .routes(routes!(create_position_stack))
        .routes(routes!(delete_position_stack))
}

#[derive(Serialize, ToSchema)]
pub struct PositionStackResponse {
    pub position_id: i32,
    pub stack_id: i32,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreatePositionStackRequest {
    pub position_id: i32,
    pub stack_id: i32,
}

pub fn to_response(model: position_stacks::Model) -> PositionStackResponse {
    PositionStackResponse {
        position_id: model.position_id,
        stack_id: model.stack_id,
        created_at: model.created_at,
        updated_at: model.updated_at,
    }
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "position_stacks",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List position stacks", body = [PositionStackResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_position_stacks(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<PositionStackResponse>>, (StatusCode, &'static str)> {

    let position_stacks = position_stacks::Entity::find()
        .filter(position_stacks::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = position_stacks.into_iter().map(to_response).collect();

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "position_stacks",
    security(("bearer_auth" = [])),
    request_body = CreatePositionStackRequest,
    responses(
        (status = 201, description = "Position stack created", body = PositionStackResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 409, description = "Stack already linked to this position")
    )
)]
#[axum::debug_handler]
pub async fn create_position_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Json(payload): Json<CreatePositionStackRequest>,
) -> Result<(StatusCode, Json<PositionStackResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    // Guard against linking the same stack to the same position more than once
    // while an active (non-deleted) link already exists.
    let existing = position_stacks::Entity::find_by_id((payload.position_id, payload.stack_id))
        .filter(position_stacks::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    if existing.is_some() {
        return Err((
            StatusCode::CONFLICT,
            "Stack already linked to this position".to_string(),
        ));
    }

    let new_position_stack = position_stacks::ActiveModel {
        position_id: Set(payload.position_id),
        stack_id: Set(payload.stack_id),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let position_stack = new_position_stack
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok((StatusCode::CREATED, Json(to_response(position_stack))))
}

#[utoipa::path(
    get,
    path = "/get/{position_id}/{stack_id}",
    tag = "position_stacks",
    security(("bearer_auth" = [])),
    params(
        ("position_id" = i32, Path, description = "Position id"),
        ("stack_id" = i32, Path, description = "Stack id")
    ),
    responses(
        (status = 200, description = "Get position stack", body = PositionStackResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position stack not found")
    )
)]
#[axum::debug_handler]
pub async fn get_position_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path((position_id, stack_id)): Path<(i32, i32)>,
) -> Result<Json<PositionStackResponse>, (StatusCode, &'static str)> {

    let position_stack = position_stacks::Entity::find_by_id((position_id, stack_id))
        .filter(position_stacks::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position stack not found"))?;

    Ok(Json(to_response(position_stack)))
}

#[utoipa::path(
    delete,
    path = "/delete/{position_id}/{stack_id}",
    tag = "position_stacks",
    security(("bearer_auth" = [])),
    params(
        ("position_id" = i32, Path, description = "Position id"),
        ("stack_id" = i32, Path, description = "Stack id")
    ),
    responses(
        (status = 204, description = "Position stack deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position stack not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_position_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path((position_id, stack_id)): Path<(i32, i32)>,
) -> Result<StatusCode, (StatusCode, String)> {

    let position_stack = position_stacks::Entity::find_by_id((position_id, stack_id))
        .filter(position_stacks::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position stack not found".to_string()))?;

    let mut active_position_stack: position_stacks::ActiveModel = position_stack.into();

    active_position_stack.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_position_stack
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}
