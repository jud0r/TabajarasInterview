use axum::{Json, extract::State, http::{HeaderMap, StatusCode}};
use sea_orm::{DatabaseConnection, EntityTrait};
use crate::entities::users;
use crate::auth::jwt::validate_token;

#[axum::debug_handler]
pub async fn get_users(
    State(db): State<DatabaseConnection>,
    headers: HeaderMap,
) -> Result<Json<Vec<users::Model>>, (StatusCode, &'static str)> {

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
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    Ok(Json(users))
}
