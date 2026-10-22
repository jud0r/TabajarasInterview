use axum::{Json, extract::State, http::StatusCode};
use sea_orm::{ActiveModelTrait, ColumnTrait, DatabaseConnection, EntityTrait, QueryFilter, Set};
use serde::{Deserialize, Serialize};
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;
use validator::Validate;
use crate::auth::extractor::AuthUser;
use crate::entities::users;

/// Build the OpenAPI-aware router for the user endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new()
        .routes(routes!(get_users))
        .routes(routes!(get_user))
        .routes(routes!(create_user))
        .routes(routes!(update_user))
        .routes(routes!(change_password))
        .routes(routes!(delete_user))
}

#[derive(Serialize, ToSchema)]
pub struct UserResponse {
    pub id: i32,
    pub first_name: String,
    pub last_name: String,
    pub email: String,
    pub created_at: sea_orm::prelude::DateTime,
    pub updated_at: Option<sea_orm::prelude::DateTime>,
}

#[derive(Deserialize, Validate, ToSchema)]
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

#[derive(Deserialize, Validate, ToSchema)]
pub struct UpdateUserRequest {
    #[validate(length(min = 3, message = "first_name must be at least 3 characters"))]
    pub first_name: Option<String>,
    #[validate(length(min = 3, message = "last_name must be at least 3 characters"))]
    pub last_name: Option<String>,
}

#[derive(Deserialize, Validate, ToSchema)]
pub struct ChangePasswordRequest {
    #[validate(length(min = 8, message = "current_password must be at least 8 characters"))]
    pub current_password: String,
    #[validate(length(min = 8, message = "new_password must be at least 8 characters"))]
    pub new_password: String,
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "users",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List users", body = [UserResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_users(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<UserResponse>>, (StatusCode, &'static str)> {

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

#[utoipa::path(
    post,
    path = "/create",
    tag = "users",
    request_body = CreateUserRequest,
    responses(
        (status = 201, description = "User created", body = UserResponse),
        (status = 400, description = "Validation error")
    )
)]
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

#[utoipa::path(
    get,
    path = "/get",
    tag = "users",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "Get current user", body = UserResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "User not found")
    )
)]
#[axum::debug_handler]
pub async fn get_user(
    State(db): State<DatabaseConnection>,
    AuthUser(claims): AuthUser
) -> Result<Json<UserResponse>, (StatusCode, &'static str)> {
    let user = users::Entity::find_by_id(claims.id)
        .filter(users::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?
        .ok_or((StatusCode::NOT_FOUND, "User not found"))?;

    let response = UserResponse {
        id: user.id,
        first_name: user.first_name,
        last_name: user.last_name,
        email: user.email,
        created_at: user.created_at,
        updated_at: user.updated_at,
    };

    Ok(Json(response))
}

#[utoipa::path(
    put,
    path = "/update",
    tag = "users",
    security(("bearer_auth" = [])),
    request_body = UpdateUserRequest,
    responses(
        (status = 200, description = "User updated", body = UserResponse),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "User not found")
    )
)]
#[axum::debug_handler]
pub async fn update_user(
    State(db): State<DatabaseConnection>,
    AuthUser(claims): AuthUser,
    Json(payload): Json<UpdateUserRequest>,
) -> Result<Json<UserResponse>, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let user = users::Entity::find_by_id(claims.id)
        .filter(users::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "User not found".to_string()))?;

    let mut active_user: users::ActiveModel = user.into();

    if let Some(first_name) = payload.first_name {
        active_user.first_name = Set(first_name);
    }

    if let Some(last_name) = payload.last_name {
        active_user.last_name = Set(last_name);
    }

    active_user.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    let user = active_user
        .update(&db)
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

    Ok(Json(response))
}

#[utoipa::path(
    put,
    path = "/password",
    tag = "users",
    security(("bearer_auth" = [])),
    request_body = ChangePasswordRequest,
    responses(
        (status = 204, description = "Password changed"),
        (status = 400, description = "Current password is incorrect"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "User not found")
    )
)]
#[axum::debug_handler]
pub async fn change_password(
    State(db): State<DatabaseConnection>,
    AuthUser(claims): AuthUser,
    Json(payload): Json<ChangePasswordRequest>,
) -> Result<StatusCode, (StatusCode, String)> {

    payload
        .validate()
        .map_err(|e| (StatusCode::BAD_REQUEST, e.to_string()))?;

    let user = users::Entity::find_by_id(claims.id)
        .filter(users::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "User not found".to_string()))?;

    let password_ok = bcrypt::verify(&payload.current_password, &user.password_hash)
        .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Failed to verify password".to_string()))?;

    if !password_ok {
        return Err((StatusCode::BAD_REQUEST, "Current password is incorrect".to_string()));
    }

    let new_password_hash = bcrypt::hash(&payload.new_password, bcrypt::DEFAULT_COST)
        .map_err(|_| (StatusCode::INTERNAL_SERVER_ERROR, "Failed to hash password".to_string()))?;

    let mut active_user: users::ActiveModel = user.into();
    active_user.password_hash = Set(new_password_hash);
    active_user.updated_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_user
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;

    Ok(StatusCode::NO_CONTENT)
}

#[utoipa::path(
    delete,
    path = "/delete",
    tag = "users",
    security(("bearer_auth" = [])),
    responses(
        (status = 204, description = "User deleted"),
        (status = 401, description = "Unauthorized"),
        (status = 404, description = "User not found")
    )
)]
#[axum::debug_handler]
pub async fn delete_user(
    State(db): State<DatabaseConnection>,
    AuthUser(claims): AuthUser,
) -> Result<StatusCode, (StatusCode, String)> {
    
    let user = users::Entity::find_by_id(claims.id)
        .filter(users::Column::DeletedAt.is_null())
        .one(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?
        .ok_or((StatusCode::NOT_FOUND, "User not found".to_string()))?;

    let mut active_user: users::ActiveModel = user.into();
    
    active_user.deleted_at = Set(Some(chrono::Utc::now().naive_utc()));

    active_user
        .update(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error".to_string())
        })?;
    
    Ok(StatusCode::NO_CONTENT)
}