use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::{positions};

/// Build the OpenAPI-aware router for the position endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_positions))
        .routes(routes!(get_position))
        .routes(routes!(create_position))
        .routes(routes!(update_position))
        .routes(routes!(delete_position))
}

/// Possible statuses for a position.
///
/// The serialized snake_case value is what gets stored in the
/// `positions.status` column.
#[derive(Serialize, Deserialize, Clone, Copy, Debug, PartialEq, Eq, ToSchema)]
#[serde(rename_all = "snake_case")]
pub enum PositionStatus {
    Draft,
    Open,
    OnHold,
    Closed,
    Cancelled,
}

impl PositionStatus {
    /// Returns the canonical string stored in the database.
    pub fn as_str(self) -> &'static str {
        match self {
            PositionStatus::Draft => "Draft",
            PositionStatus::Open => "Open",
            PositionStatus::OnHold => "OnHold",
            PositionStatus::Closed => "Closed",
            PositionStatus::Cancelled => "Cancelled",
        }
    }
}

impl std::str::FromStr for PositionStatus {
    type Err = String;

    fn from_str(value: &str) -> Result<Self, Self::Err> {
        match value {
            "Draft" => Ok(PositionStatus::Draft),
            "Open" => Ok(PositionStatus::Open),
            "OnHold" => Ok(PositionStatus::OnHold),
            "Closed" => Ok(PositionStatus::Closed),
            "Cancelled" => Ok(PositionStatus::Cancelled),
            other => Err(other.to_string()),
        }
    }
}

#[derive(Serialize, ToSchema)]
pub struct PositionResponse {
    pub id: i32,
    pub title: String,
    pub description: Option<String>,
    pub status: PositionStatus,
    pub created_by: i32,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
    pub created_at: sea_orm::prelude::DateTime,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct CreatePositionRequest {
    #[validate(length(min = 3, message = "title must be at least 3 characters"))]
    pub title: String,
    pub description: Option<String>,
    pub status: PositionStatus,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdatePositionRequest {
    #[validate(length(min = 3, message = "title must be at least 3 characters"))]
    pub title: Option<String>,
    pub description: Option<String>,
    pub status: Option<PositionStatus>,
}

fn to_response(model: positions::Model) -> Result<PositionResponse, (StatusCode, &'static str)> {
    Ok(PositionResponse {
        id: model.id,
        title: model.title,
        description: model.description,
        status: model.status.parse::<PositionStatus>()
            .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Unknown position status"))?,
        created_by: model.created_by,
        updated_at: model.updated_at,
        created_at: model.created_at,
    })
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "positions",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List positions", body = [PositionResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_positions(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<PositionResponse>>, (StatusCode, &'static str)> {

    let positions = positions::Entity::find()
        .filter(positions::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = positions
        .into_iter()
        .map(to_response)
        .collect::<Result<Vec<_>, _>>()?;

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/create",
    tag = "positions",
    security(("bearer_auth" = [])),
    request_body = CreatePositionRequest,
    responses(
        (status = 201, description = "Position created", body = PositionResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn create_position(
    State(db): State<DatabaseConnection>,
    user: AuthUser,
    Json(payload): Json<CreatePositionRequest>,
) -> Result<(StatusCode, Json<PositionResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let new_position = positions::ActiveModel {
        title: Set(payload.title),
        description: Set(payload.description),
        status: Set(payload.status.as_str().to_string()),
        created_by: Set(user.0.id),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let position = new_position
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = to_response(position).map_err(|(s, m)| (s, m.to_string()))?;

    Ok((StatusCode::CREATED, Json(response)))
}

#[utoipa::path(
    get,
    path = "/get/{id}",
    tag = "positions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Position id")),
    responses(
        (status = 200, description = "Get position", body = PositionResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position not found")
    )
)]
#[axum::debug_handler]
pub async fn get_position(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<PositionResponse>, (StatusCode, &'static str)> {

    let position = positions::Entity::find_by_id(id)
        .filter(positions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position not found"))?;

    let response = to_response(position)?;

    Ok(Json(response))
}

#[utoipa::path(
    put,
    path = "/update/{id}",
    tag = "positions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Position id")),
    request_body = UpdatePositionRequest,
    responses(
        (status = 200, description = "Position updated", body = PositionResponse),
        (status = 400, description = "Validation error"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position not found")
    )
)]
#[axum::debug_handler]
pub async fn update_position(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
    Json(payload): Json<UpdatePositionRequest>,
) -> Result<Json<PositionResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let position = positions::Entity::find_by_id(id)
        .filter(positions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position not found".to_string()))?;

    let mut active_position: positions::ActiveModel = position.into();

    if let Some(title) = payload.title {
        active_position.title = Set(title);
    }

    if let Some(description) = payload.description {
        active_position.description = Set(Some(description));
    }

    if let Some(status) = payload.status {
        active_position.status = Set(status.as_str().to_string());
    }

    active_position.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let position = active_position
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = to_response(position).map_err(|(s, m)| (s, m.to_string()))?;

    Ok(Json(response))
}

#[utoipa::path(
    delete,
    path = "/delete/{id}",
    tag = "positions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Position id")),
    responses(
        (status = 204, description = "Position deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_position(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<StatusCode, (StatusCode, String)> {

    let position = positions::Entity::find_by_id(id)
        .filter(positions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position not found".to_string()))?;

    let mut active_position: positions::ActiveModel = position.into();

    active_position.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_position
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}
