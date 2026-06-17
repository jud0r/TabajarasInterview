use jsonwebtoken::{encode, Header, EncodingKey};
use jsonwebtoken::{decode, DecodingKey, Validation};
use serde::{Serialize, Deserialize};

#[derive(Serialize, Deserialize)]
pub struct Claims {
    pub sub: String,
    pub exp: usize,
    pub id: i32
}

fn secret() -> String {
    std::env::var("SECRET").expect("SECRET not set")
}

pub fn generate_token(email: &str, id: i32) -> String {
    let claims = Claims {
        sub: email.to_string(),
        exp: 2000000000,
        id : id
    };

    encode(
        &Header::default(),
        &claims,
        &EncodingKey::from_secret(secret().as_bytes()),
    )
    .unwrap()
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
