mod auth;
mod handlers;
mod entities;

use axum::{
    Router, routing::{get, post, put}
};
use sea_orm::{Database, DatabaseConnection};
use std::net::SocketAddr;
use tokio::time::{sleep, Duration};
use handlers::auth::{login, refresh};
use handlers::users::{get_users, create_user, update_user, delete_user};



#[tokio::main]
async fn main() {
    dotenvy::dotenv().ok();
    
    let database_url = std::env::var("DATABASE_URL")
    .expect("DATABASE_URL not set");


    let db = connect_with_retry(&database_url).await;

    let app = Router::new()        
        .route("/login", post(login))
        .route("/refresh", post(refresh))
        .route("/users", get(get_users).post(create_user))
        .route("/users/me", put(update_user).delete(delete_user))
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


