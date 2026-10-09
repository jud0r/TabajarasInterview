use axum::{Json, extract::State, http::StatusCode};
use sea_orm::{DatabaseConnection, EntityTrait};
use serde::Serialize;
use utoipa::ToSchema;
use utoipa_axum::router::OpenApiRouter;
use utoipa_axum::routes;

use crate::auth::extractor::AuthUser;
use crate::entities::locations;

/// Build the OpenAPI-aware router for the location endpoints.
pub fn router() -> OpenApiRouter<DatabaseConnection> {
    OpenApiRouter::new().routes(routes!(get_locations))
}

#[derive(Serialize, ToSchema)]
pub struct LocationResponse {
    pub id: i32,
    pub city: String,
}

impl From<locations::Model> for LocationResponse {
    fn from(model: locations::Model) -> Self {
        LocationResponse {
            id: model.id,
            city: model.city,
        }
    }
}

#[utoipa::path(
    get,
    path = "/get_all",
    tag = "locations",
    security(("bearer_auth" = [])),
    responses(
        (status = 200, description = "List locations", body = [LocationResponse]),
        (status = 401, description = "Unauthorized")
    )
)]
#[axum::debug_handler]
pub async fn get_locations(
    State(db): State<DatabaseConnection>,
    _user: AuthUser,
) -> Result<Json<Vec<LocationResponse>>, (StatusCode, &'static str)> {

    let locations = locations::Entity::find()
        .all(&db)
        .await
        .map_err(|e| {
            println!("DB ERROR: {:?}", e);
            (StatusCode::INTERNAL_SERVER_ERROR, "DB error")
        })?;

    Ok(Json(locations.into_iter().map(LocationResponse::from).collect()))
}
