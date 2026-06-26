use jsonwebtoken::{encode, Header, EncodingKey};
use jsonwebtoken::{decode, DecodingKey, Validation};
use serde::{Serialize, Deserialize};
use sha2::{Digest, Sha256};

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum TokenType {
    Access,
    Refresh,
}

#[derive(Serialize, Deserialize)]
pub struct Claims {
    pub sub: String,
    pub exp: usize,
    pub id: i32,
    #[serde(default = "default_token_type")]
    pub token_type: TokenType,
}

fn default_token_type() -> TokenType {
    TokenType::Access
}

fn secret() -> String {
    std::env::var("SECRET").expect("SECRET not set")
}

fn token_expiration() -> i32 {
    std::env::var("TOKEN_EXPIRATION_IN_MINUTES")
        .unwrap_or_else(|_| "60".to_string())
        .parse::<i32>()
        .unwrap_or(60)
}

fn refresh_token_expiration() -> i32 {
    std::env::var("REFRESH_TOKEN_EXPIRATION_IN_DAYS")
        .unwrap_or_else(|_| "30".to_string())
        .parse::<i32>()
        .unwrap_or(30)
}

pub fn generate_token(email: &str, id: i32) -> (String, i32) {
    let ttl_seconds = token_expiration() * 60;
    generate(email, id, TokenType::Access, ttl_seconds)
}

pub fn generate_refresh_token(email: &str, id: i32) -> (String, i32) {
    let ttl_seconds = refresh_token_expiration() * 24 * 60 * 60;
    generate(email, id, TokenType::Refresh, ttl_seconds)
}

fn generate(email: &str, id: i32, token_type: TokenType, ttl_seconds: i32) -> (String, i32) {
    let exp = chrono::Utc::now().timestamp() + ttl_seconds as i64;

    let claims = Claims {
        sub: email.to_string(),
        exp: exp as usize,
        id,
        token_type,
    };

    let token = encode(
        &Header::default(),
        &claims,
        &EncodingKey::from_secret(secret().as_bytes()),
    )
    .unwrap();

    (token, ttl_seconds)
}

pub fn decode_token(token: &str) -> Option<Claims> {
    decode::<Claims>(
        token,
        &DecodingKey::from_secret(secret().as_bytes()),
        &Validation::default(),
    )
    .map(|data| data.claims)
    .ok()
}

/// Hash a token (e.g. a refresh token) for storage/lookup in the database.
/// Refresh tokens are high-entropy JWTs, so a fast SHA-256 digest is suitable
/// and avoids bcrypt's 72-byte input limitation.
pub fn hash_token(token: &str) -> String {
    let digest = Sha256::digest(token.as_bytes());
    hex::encode(digest)
}
