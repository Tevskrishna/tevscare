# Security

- Passwords are hashed by ASP.NET Identity. They are not stored or logged.
- Access tokens are JWT. Refresh tokens are stored as SHA-256 hashes and rotated on use.
- The phone keeps tokens in Expo Secure Store, in separate keys.
- Account deletion removes profile data and the Identity user.
- Request validation is FluentValidation. Database access is EF Core parameters, not concatenated SQL.
- Auth endpoints are rate limited.
- Security headers are added by middleware.
- Logs must not include passwords, tokens, or reset-email bodies.
- `.env` is gitignored. `.env.example` has the local Docker password that is also in `docker-compose.yml`. It is not a production secret.
- Production forces demo account seeding off.
- Staging and production do not return exception messages to clients. The Testing environment still includes the exception type so API tests can diagnose failures.
- `TrustForwardedHeaders` is for a TLS proxy. Leave it false when the API is reached directly.
- Browser CORS uses the configured origin list in every environment. An empty list allows no browser origin. Native apps do not use CORS.
- Analytics events use an allow-list and reject property payloads that contain password or token fields.

This document does not claim HIPAA, GDPR, or any other certification. Privacy and terms screens are placeholders.

The product disclaimer, shown in the app and returned by the API:

TEVSCARE provides meal-planning and habit-tracking information. It is not a substitute for medical diagnosis or treatment. Dietary restrictions should be confirmed with a qualified healthcare professional.
