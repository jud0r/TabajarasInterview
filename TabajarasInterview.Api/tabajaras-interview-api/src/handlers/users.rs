use axum::{Json, extract::State, http::{HeaderMap, StatusCode}};
use sea_orm::{ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter};
use serde::Serialize;
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

#[axum::debug_handler]
pub async fn get_users(
    State(db): State<DatabaseConnection>,
    headers: HeaderMap,
) -> Result<Json<Vec<UserResponse>>, (StatusCode, &'static str)> {

    let auth_header = headers
        .get("authorization")
        .ok_or((StatusCode::UNAUTHORIZED, "Missing authorization header"))?;

    let auth_str = auth_header
        .to_str()
        .map_err(|_| (StatusCode::BAD_REQUEST, "Invalid authorization header"))?;

    let token = auth_str
        .strip_prefix("Bearer ")
        .ok_or((StatusCode::UNAUTHORIZED, "Invalid Bearer token format"))?;

    if !validate_token(token) {
        return Err((StatusCode::UNAUTHORIZED, "Invalid token"));
    }

    let users = users::Entity::find()
        .filter(users::Column::DeletedAt.is_null())
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
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
