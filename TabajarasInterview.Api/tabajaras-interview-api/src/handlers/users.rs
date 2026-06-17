use axum::{Json, extract::State, http::{HeaderMap, StatusCode}};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use validator::Validate;
use crate::{auth::jwt::validate_token};
use crate::entities::users;

#[derive(Serialize)]
pub struct UserResponse {
    pub id: i32,
    pub first_name: String,
    pub last_name: String,
    pub email: String,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate)]
pub struct CreateUserRequest {
    #[validate(length(min = 3, message = "first_name must be at least 3 characters"))]
    pub first_name: String,
    #[validate(length(min = 3, message = "last_name must be at least 3 characters"))]
    pub last_name: String,
    #[validate(email(message = "email must be a valid email address"))]
    pub email: String,
    #[validate(length(min = 8, message = "password must be at least 8 characters"))]
    pub password: String,
}

#[axum::debug_handler]
pub async fn get_users(
    State(db): State<DatabaseConnection>,
    headers: HeaderMap,
) -> Result<Json<Vec<UserResponse>>, (StatusCode, String)> {

    let auth_header = headers
        .get("authorization")
        .ok_or((StatusCode::UNAUTHORIZED, "Missing authorization header".to_string()))?;

    let auth_str = auth_header
        .to_str()
        .map_err(|_| (StatusCode::BAD_REQUEST, "Invalid authorization header".to_string()))?;

    let token = auth_str
        .strip_prefix("Bearer ")
        .ok_or((StatusCode::UNAUTHORIZED, "Invalid Bearer token format".to_string()))?;

    if !validate_token(token) {
        return Err((StatusCode::UNAUTHORIZED, "Invalid token".to_string()));
    }

    let users = users::Entity::find()
        .filter(users::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = users
        .into_iter()
        .map(|u| UserResponse {
            id: u.id,
            first_name: u.first_name,
            last_name: u.last_name,
            email: u.email,
            created_at: u.created_at,
            updated_at: u.updated_at,
        })
        .collect();

    Ok(Json(response))
}

#[axum::debug_handler]
pub async fn create_user(
    State(db): State<DatabaseConnection>,
    Json(payload): Json<CreateUserRequest>,
) -> Result<(StatusCode, Json<UserResponse>), (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let password_hash = bcrypt::hash(&payload.password, bcrypt::DEFAULT_COST)
        .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Failed to hash password".to_string()))?;

    let new_user = users::ActiveModel {
        first_name: Set(payload.first_name),
        last_name: Set(payload.last_name),
        email: Set(payload.email),
        password_hash: Set(password_hash),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    let user = new_user
        .insert(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    let response = UserResponse {
        id: user.id,
        first_name: user.first_name,
        last_name: user.last_name,
        email: user.email,
        created_at: user.created_at,
        updated_at: user.updated_at,
    };

    Ok((StatusCode::CREATED, Json(response)))
}
