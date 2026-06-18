use axum::{Json, extract::State, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;

use crate::auth::jwt::{decode_token, generate_refresh_token, generate_token, hash_token, TokenType};
use crate::entities::{refresh_tokens, users};
use crate::handlers::users::UserResponse;

#[derive(Deserialize, ToSchema)]
pub struct LoginRequest {
    pub email: String,
    pub password: String,
}

#[derive(Serialize, ToSchema)]
pub struct LoginResponse {
    pub access_token: String,
    pub expires_in: i64,
    pub refresh_token: String,
    pub user: UserResponse
}

#[derive(Deserialize, ToSchema)]
pub struct RefreshRequest {
    pub refresh_token: String,
}

#[derive(Serialize, ToSchema)]
pub struct RefreshResponse {
    pub access_token: String,
    pub expires_in: i64,
    pub refresh_token: String,
}

/// Persist the SHA-256 hash of a refresh token so it can be validated/revoked later.
async fn store_refresh_token(
    db: &DatabaseConnection,
    user_id: i32,
    token: &str,
    expires_at: i64,
) -> Result<(), (StatusCode, &'static str)> {
    let expires_at = chrono::DateTime::from_timestamp(expires_at, 0)
        .ok_or((StatusCode::INTERNAL_SERVER_ERROR, "Invalid token expiration"))?
        .naive_utc();

    let record = refresh_tokens::ActiveModel {
        user_id: Set(user_id),
        token_hash: Set(hash_token(token)),
        expires_at: Set(expires_at),
        created_at: Set(chrono::Utc::now().naive_utc()),
        ..Default::default()
    };

    record.insert(db).await.map_err(|e| {
        println!("DB ERROR: {}", e);
        (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
    })?;

    Ok(())
}

#[utoipa::path(
    post,
    path = "/api/auth/login",
    tag = "auth",
    request_body = LoginRequest,
    responses(
        (status = 200, description = "Login succeeded", body = LoginResponse),
        (status = 401, description = "Invalid credentials")
    )
)]
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

    let access_token = generate_token(&user.email, user.id);
    let refresh_token = generate_refresh_token(&user.email, user.id);

    store_refresh_token(&db, user.id, &refresh_token.0, refresh_token.1).await?;

    let response = LoginResponse {
        access_token: access_token.0,
        expires_in: access_token.1,
        refresh_token: refresh_token.0,
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

#[utoipa::path(
    post,
    path = "/api/auth/refresh",
    tag = "auth",
    request_body = RefreshRequest,
    responses(
        (status = 200, description = "Token refreshed", body = RefreshResponse),
        (status = 401, description = "Invalid or expired refresh token")
    )
)]
#[axum::debug_handler]
pub async fn refresh(
    State(db): State<DatabaseConnection>,
    Json(payload): Json<RefreshRequest>,
) -> Result<Json<RefreshResponse>, (StatusCode, &'static str)> {

    let claims = decode_token(&payload.refresh_token)
        .ok_or((StatusCode::UNAUTHORIZED, "Invalid refresh token"))?;

    if claims.token_type != TokenType::Refresh {
        return Err((StatusCode::UNAUTHORIZED, "Invalid token type"));
    }

    let token_hash = hash_token(&payload.refresh_token);

    let stored = refresh_tokens::Entity::find()
        .filter(refresh_tokens::Column::TokenHash.eq(&token_hash))
        .filter(refresh_tokens::Column::RevokedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::UNAUTHORIZED, "Invalid refresh token"))?;

    if stored.expires_at < chrono::Utc::now().naive_utc() {
        return Err((StatusCode::UNAUTHORIZED, "Refresh token expired"));
    }

    // Rotate: revoke the used refresh token before issuing a new one.
    let mut active: refresh_tokens::ActiveModel = stored.into();
    active.revoked_at = Set(Some(chrono::Utc::now().naive_utc()));
    active.update(&db).await.map_err(|e| {
        println!("DB ERROR: {}", e);
        (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
    })?;

    let access_token = generate_token(&claims.sub, claims.id);
    let refresh_token = generate_refresh_token(&claims.sub, claims.id);

    store_refresh_token(&db, claims.id, &refresh_token.0, refresh_token.1).await?;

    let response = RefreshResponse {
        access_token: access_token.0,
        expires_in: access_token.1,
        refresh_token: refresh_token.0,
    };

    Ok(Json(response))
}