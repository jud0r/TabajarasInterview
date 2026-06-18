mod auth;
mod handlers;
mod entities;

use axum::{
    Router, routing::{get, post, put, delete}
};
use sea_orm::{Database, DatabaseConnection};
use std::net::SocketAddr;
use tokio::time::{sleep, Duration};
use utoipa::OpenApi;
use utoipa::openapi::security::{HttpAuthScheme, HttpBuilder, SecurityScheme};
use utoipa_swagger_ui::SwaggerUi;
use handlers::auth::{login, refresh};
use handlers::users::{get_users, create_user, update_user, delete_user};

use crate::handlers::users::get_user;

#[derive(OpenApi)]
#[openapi(
    paths(
        handlers::auth::login,
        handlers::auth::refresh,
        handlers::users::get_users,
        handlers::users::get_user,
        handlers::users::create_user,
        handlers::users::update_user,
        handlers::users::delete_user,
    ),
    components(schemas(
        handlers::auth::LoginRequest,
        handlers::auth::LoginResponse,
        handlers::auth::RefreshRequest,
        handlers::auth::RefreshResponse,
        handlers::users::UserResponse,
        handlers::users::CreateUserRequest,
        handlers::users::UpdateUserRequest,
    )),
    modifiers(&SecurityAddon),
    tags(
        (name = "auth", description = "Authentication endpoints"),
        (name = "users", description = "User management endpoints"),
    )
)]
struct ApiDoc;

struct SecurityAddon;

impl utoipa::Modify for SecurityAddon {
    fn modify(&self, openapi: &mut utoipa::openapi::OpenApi) {
        let components = openapi.components.as_mut().unwrap();
        components.add_security_scheme(
            "bearer_auth",
            SecurityScheme::Http(
                HttpBuilder::new()
                    .scheme(HttpAuthScheme::Bearer)
                    .bearer_format("JWT")
                    .build(),
            ),
        );
    }
}



#[tokio::main]
async fn main() {
    dotenvy::dotenv().ok();
    
    let database_url = std::env::var("DATABASE_URL")
    .expect("DATABASE_URL not set");


    let db = connect_with_retry(&database_url).await;

    let app = Router::new()        
        .route("/api/auth/login", post(login))
        .route("/api/auth/refresh", post(refresh))
        .route("/api/users/get_all", get(get_users))
        .route("/api/users/get", get(get_user))
        .route("/api/users/create", post(create_user))
        .route("/api/users/update", put(update_user))
        .route("/api/users/delete", delete(delete_user))
        .merge(SwaggerUi::new("/swagger").url("/api-docs/openapi.json", ApiDoc::openapi()))
        .with_state(db);


    let addr = SocketAddr::from(([0, 0, 0, 0], 8080));

    println!("🚀 Running on {}", addr);

    axum::serve(
        tokio::net::TcpListener::bind(addr).await.unwrap(),
        app,
    )
    .await
    .unwrap();
}


async fn connect_with_retry(database_url: &str) -> DatabaseConnection {
    loop {
        match Database::connect(database_url).await {
            Ok(db) => {
                println!("✅ Connected to DB");
                return db;
            }
            Err(e) => {
                println!("⏳ Waiting for DB: {}", e);
                sleep(Duration::from_secs(2)).await;
            }
        }
    }
}


