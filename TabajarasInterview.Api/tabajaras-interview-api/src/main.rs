mod auth;
mod handlers;

use axum::{
    Json, Router, extract::State, routing::{get, post}
};
use serde::Serialize;
use sea_orm::{ConnectionTrait, Database, DatabaseConnection, DbBackend, Statement};
use std::net::SocketAddr;
use tokio::time::{sleep, Duration};
use handlers::auth::login;
use axum::http::HeaderMap;
use crate::auth::jwt::validate_token;


#[derive(Serialize)]
struct User {
    id: i32,
    first_name: String,
    last_name: String,
    email: String
}


async fn get_users(
    State(db): State<DatabaseConnection>,
    headers: HeaderMap,
) -> Result<Json<Vec<User>>, &'static str> {

    if let Some(auth_header) = headers.get("authorization") {
        if let Ok(auth_str) = auth_header.to_str() {
            if let Some(token) = auth_str.strip_prefix("Bearer ") {
                if validate_token(token) {

                    let stmt = Statement::from_string(
                        DbBackend::MySql,
                        "SELECT id, firstName, lastName, email FROM Users".to_string(),
                    );

                    let rows = ConnectionTrait::query_all(&db, stmt)
                        .await
                        .map_err(|_| "DB error")?;

                    let users = rows
                        .into_iter()
                        .map(|row| User {
                            id: row.try_get("", "id").unwrap(),
                            first_name: row.try_get("", "firstName").unwrap(),
                            last_name: row.try_get("", "lastName").unwrap(),
                            email: row.try_get("", "email").unwrap(),
                        })
                        .collect();

                    return Ok(Json(users));
                }
            }
        }
    }

    Err("Unauthorized")
}


#[tokio::main]
async fn main() {
    dotenvy::dotenv().ok();
    
    let database_url = std::env::var("DATABASE_URL")
    .expect("DATABASE_URL not set");


    let db = connect_with_retry(&database_url).await;

    let app = Router::new()        
        .route("/login", post(login))
        .route("/users", get(get_users))
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


