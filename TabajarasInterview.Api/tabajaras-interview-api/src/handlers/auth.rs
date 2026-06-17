use axum::{Json, extract::State, http::StatusCode};
use sea_orm::{ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter};
use serde::{Deserialize, Serialize};

use crate::auth::jwt::generate_token;
use crate::entities::users;
use crate::handlers::users::UserResponse;

#[derive(Deserialize)]
pub struct LoginRequest {
    pub email: String,
    pub password: String,
}

#[derive(Serialize)]
pub struct LoginResponse {
    pub token: String,
    pub user: UserResponse
}

#[axum::debug_handler]
pub async fn login(
    State(db): State<DatabaseConnection>,
    Json(payload): Json<LoginRequest>,
) -> Result<Json<LoginResponse>, (StatusCode, &'static str)> {

    let user = users::Entity::find()
        .filter(users::Column::Email.eq(&payload.email))
        .filter(users::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::UNAUTHORIZED, "Invalid credentials"))?;

    let password_ok = bcrypt::verify(&payload.password, &user.password_hash)
        .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Invalid credentials"))?;

    if !password_ok {
        return Err((StatusCode::UNAUTHORIZED, "Invalid credentials"));
    }

    let token = generate_token(&user.email, user.id);

    let response = LoginResponse {
        token,
        user: UserResponse {
            id: user.id,
            first_name: user.first_name,
            last_name: user.last_name,
            email: user.email,
            created_at: user.created_at,
            updated_at: user.updated_at,
        },
    };

    Ok(Json(response))
}