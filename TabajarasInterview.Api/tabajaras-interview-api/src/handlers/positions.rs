use axum::{Json, extract::{Path, State}, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;

use crate::auth::extractor::AuthUser;
use crate::entities::{candidate_applications, candidates, position_stacks, positions, stacks};
use crate::handlers::candidates::{CandidateResponse, to_response as candidate_to_response};
use crate::handlers::stacks::StackResponse;

/// Build the OpenAPI-aware router for the position endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_positions))
        .routes(routes!(get_position))
        .routes(routes!(create_position))
        .routes(routes!(update_position))
        .routes(routes!(delete_position))
        .routes(routes!(get_position_candidates))
        .routes(routes!(get_position_stacks))
        .routes(routes!(assign_position_stack, remove_position_stack))
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
    pub stacks: Vec<StackResponse>,
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

pub fn to_response(model: positions::Model) -> Result<PositionResponse, (StatusCode, &'static str)> {
    Ok(PositionResponse {
        id: model.id,
        title: model.title,
        description: model.description,
        status: model.status.parse::<PositionStatus>()
            .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Unknown position status"))?,
        created_by: model.created_by,
        updated_at: model.updated_at,
        created_at: model.created_at,
        stacks: Vec::new(),
    })
}

/// Loads the active stacks linked to a position through the `position_stacks` join table.
async fn load_position_stacks(
    db: &DatabaseConnection,
    position_id: i32,
) -> Result<Vec<StackResponse>, (StatusCode, &'static str)> {
    let links = position_stacks::Entity::find()
        .filter(position_stacks::Column::PositionId.eq(position_id))
        .filter(position_stacks::Column::DeletedAt.is_null())
        .all(db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let stack_ids: Vec<i32> = links.iter().map(|l| l.stack_id).collect();

    let stacks = stacks::Entity::find()
        .filter(stacks::Column::Id.is_in(stack_ids))
        .filter(stacks::Column::DeletedAt.is_null())
        .all(db)
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

    Ok(response)
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

    let mut response = Vec::with_capacity(positions.len());
    for position in positions {
        let mut item = to_response(position)?;
        item.stacks = load_position_stacks(&db, item.id).await?;
        response.push(item);
    }

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

    let mut response = to_response(position)?;
    response.stacks = load_position_stacks(&db, id).await?;

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

    let mut response = to_response(position).map_err(|(s, m)| (s, m.to_string()))?;
    response.stacks = load_position_stacks(&db, id)
        .await
        .map_err(|(s, m)| (s, m.to_string()))?;

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

#[utoipa::path(
    get,
    path = "/get/{id}/candidates",
    tag = "positions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Position id")),
    responses(
        (status = 200, description = "List candidates who applied to a position", body = [CandidateResponse]),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position not found")
    )
)]
#[axum::debug_handler]
pub async fn get_position_candidates(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<Vec<CandidateResponse>>, (StatusCode, &'static str)> {

    positions::Entity::find_by_id(id)
        .filter(positions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position not found"))?;

    let applications = candidate_applications::Entity::find()
        .filter(candidate_applications::Column::PositionId.eq(id))
        .filter(candidate_applications::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let candidate_ids: Vec<i32> = applications.iter().map(|a| a.candidate_id).collect();

    let candidates = candidates::Entity::find()
        .filter(candidates::Column::Id.is_in(candidate_ids))
        .filter(candidates::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = candidates.into_iter().map(candidate_to_response).collect();

    Ok(Json(response))
}

#[utoipa::path(
    get,
    path = "/get/{id}/stacks",
    tag = "positions",
    security(("bearer_auth" = [])),
    params(("id" = i32, Path, description = "Position id")),
    responses(
        (status = 200, description = "List stacks required for a position", body = [StackResponse]),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position not found")
    )
)]
#[axum::debug_handler]
pub async fn get_position_stacks(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path(id): Path<i32>,
) -> Result<Json<Vec<StackResponse>>, (StatusCode, &'static str)> {

    positions::Entity::find_by_id(id)
        .filter(positions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position not found"))?;

    let response = load_position_stacks(&db, id).await?;

    Ok(Json(response))
}

#[utoipa::path(
    post,
    path = "/get/{id}/stacks/{stack_id}",
    tag = "positions",
    security(("bearer_auth" = [])),
    params(
        ("id" = i32, Path, description = "Position id"),
        ("stack_id" = i32, Path, description = "Stack id")
    ),
    responses(
        (status = 201, description = "Stack assigned to the position", body = [StackResponse]),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position or stack not found"),
        (status = 409, description = "Stack already assigned to this position")
    )
)]
#[axum::debug_handler]
pub async fn assign_position_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path((id, stack_id)): Path<(i32, i32)>,
) -> Result<(StatusCode, Json<Vec<StackResponse>>), (StatusCode, &'static str)> {

    // Ensure the target position exists and is active.
    positions::Entity::find_by_id(id)
        .filter(positions::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position not found"))?;

    // Ensure the target stack exists and is active.
    stacks::Entity::find_by_id(stack_id)
        .filter(stacks::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Stack not found"))?;

    // The join table uses a composite primary key and soft-deletes, so a
    // previously removed link still physically exists. Look the row up by its
    // full key (ignoring `deleted_at`) to either reject a duplicate or revive a
    // removed association instead of triggering a primary-key collision.
    let existing = position_stacks::Entity::find_by_id((id, stack_id))
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    match existing {
        Some(link) if link.deleted_at.is_none() => {
            return Err((StatusCode::CONFLICT, "Stack already assigned to this position"));
        }
        Some(link) => {
            let mut active_link: position_stacks::ActiveModel = link.into();
            active_link.deleted_at = Set(None);
            active_link.updated_at = Set(Some(chrono::Utc::now().naive_utc()));
            active_link
                .update(&db)
                .await
                .map_err(|e| {
                    println!("DB ERROR: {:?}", e);
                    (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
                })?;
        }
        None => {
            let new_link = position_stacks::ActiveModel {
                position_id: Set(id),
                stack_id: Set(stack_id),
                created_at: Set(chrono::Utc::now().naive_utc()),
                ..Default::default()
            };
            new_link
                .insert(&db)
                .await
                .map_err(|e| {
                    println!("DB ERROR: {:?}", e);
                    (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
                })?;
        }
    }

    let response = load_position_stacks(&db, id).await?;

    Ok((StatusCode::CREATED, Json(response)))
}

#[utoipa::path(
    delete,
    path = "/get/{id}/stacks/{stack_id}",
    tag = "positions",
    security(("bearer_auth" = [])),
    params(
        ("id" = i32, Path, description = "Position id"),
        ("stack_id" = i32, Path, description = "Stack id")
    ),
    responses(
        (status = 200, description = "Stack removed from the position", body = [StackResponse]),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "Position stack link not found")
    )
)]
#[axum::debug_handler]
pub async fn remove_position_stack(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
    Path((id, stack_id)): Path<(i32, i32)>,
) -> Result<Json<Vec<StackResponse>>, (StatusCode, &'static str)> {

    let link = position_stacks::Entity::find_by_id((id, stack_id))
        .filter(position_stacks::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "Position stack link not found"))?;

    let mut active_link: position_stacks::ActiveModel = link.into();
    active_link.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));
    active_link
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    let response = load_position_stacks(&db, id).await?;

    Ok(Json(response))
}
