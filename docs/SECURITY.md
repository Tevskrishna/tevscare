# Security

- Passwords are hashed by ASP.NET Identity. They are not stored or logged.
- Access tokens are JWT. Refresh tokens are stored as SHA-256 hashes and rotated on use.
- The phone keeps tokens in Expo Secure Store, in separate keys.
- Account deletion removes profile data and the Identity user.
- Request validation is FluentValidation. Database access is EF Core parameters, not concatenated SQL.
- Auth endpoints are rate limited.
- Security headers are added by middleware.
- Logs must not include passwords, tokens, or reset-email bodies.
- `.env` is gitignored. `.env.example` has placeholders. The Docker database password is a local development password, not a production secret.
- Analytics events use an allow-list and reject property payloads that contain password or token fields.

This document does not claim HIPAA, GDPR, or any other certification. Privacy and terms screens are placeholders.

The product disclaimer, shown in the app and returned by the API:

TEVSCARE provides meal-planning and habit-tracking information. It is not a substitute for medical diagnosis or treatment. Dietary restrictions should be confirmed with a qualified healthcare professional.
