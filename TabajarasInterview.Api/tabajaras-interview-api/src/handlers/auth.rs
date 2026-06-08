use axum::{Json, response::IntoResponse};
use serde::{Deserialize, Serialize};

use crate::auth::jwt::generate_token;

#[derive(Deserialize)]
pub struct LoginRequest {
    pub username: String,
    pub password: String,
}

#[derive(Serialize)]
pub struct LoginResponse {
    pub token: String,
}

pub async fn login(Json(payload): Json<LoginRequest>) -> impl IntoResponse {
    if payload.username == "admin" && payload.password == "123" {
        let token = generate_token(&payload.username);

        return Json(LoginResponse { token });
    }

    Json(LoginResponse {
        token: "invalid".to_string(),
    })
}