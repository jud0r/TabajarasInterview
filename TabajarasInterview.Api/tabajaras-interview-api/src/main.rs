mod auth;
mod handlers;
mod entities;

use sea_orm::{Database, DatabaseConnection};
use std::net::SocketAddr;
use tokio::time::{sleep, Duration};
use utoipa::OpenApi;
use utoipa::openapi::security::{HttpAuthScheme, HttpBuilder, SecurityScheme};
use utoipa_axum::router::OpenApiRouter;
use utoipa_swagger_ui::SwaggerUi;

#[derive(OpenApi)]
#[openapi(
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

    let (router, api) = OpenApiRouter::with_openapi(ApiDoc::openapi())
        .nest("/api/auth", handlers::auth::router())
        .nest("/api/users", handlers::users::router())
        .split_for_parts();

    let app = router
        .merge(SwaggerUi::new("/swagger").url("/api-docs/openapi.json", api))
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


