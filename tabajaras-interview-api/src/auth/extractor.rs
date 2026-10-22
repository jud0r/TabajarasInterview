use axum::extract::FromRequestParts;
use axum::http::StatusCode;
use axum::http::request::Parts;

use crate::auth::jwt::{decode_token, Claims, TokenType};

pub struct AuthUser(pub Claims);

impl<S> FromRequestParts<S> for AuthUser
where
    S: Send + Sync,
{
    type Rejection = (StatusCode, &'static str);

    async fn from_request_parts(parts: &mut Parts, _state: &S) -> Result<Self, Self::Rejection> {
        let auth_header = parts
            .headers
            .get("authorization")
            .ok_or((StatusCode::UNAUTHORIZED, "Missing authorization header"))?;

        let auth_str = auth_header
            .to_str()
            .map_err(|_| (StatusCode::BAD_REQUEST, "Invalid authorization header"))?;

        let token = auth_str
            .strip_prefix("Bearer ")
            .ok_or((StatusCode::UNAUTHORIZED, "Invalid Bearer token format"))?;

        let claims = decode_token(token)
            .ok_or((StatusCode::UNAUTHORIZED, "Invalid token"))?;

        if claims.token_type != TokenType::Access {
            return Err((StatusCode::UNAUTHORIZED, "Invalid token type"));
        }

        Ok(AuthUser(claims))
    }
}
