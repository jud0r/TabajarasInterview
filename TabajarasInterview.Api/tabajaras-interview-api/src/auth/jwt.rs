use jsonwebtoken::{encode, Header, EncodingKey};
use jsonwebtoken::{decode, DecodingKey, Validation};
use serde::{Serialize, Deserialize};

#[derive(Serialize, Deserialize)]
pub struct Claims {
    pub sub: String,
    pub exp: usize,
}

const SECRET: &[u8] = b"mysecretkey";

pub fn generate_token(username: &str) -> String {
    let claims = Claims {
        sub: username.to_string(),
        exp: 2000000000,
    };

    encode(&Header::default(), &claims, &EncodingKey::from_secret(SECRET))
        .unwrap()
}

pub fn validate_token(token: &str) -> bool {
    decode::<Claims>(
        token,
        &DecodingKey::from_secret(SECRET),
        &Validation::default(),
    )
    .is_ok()
}
