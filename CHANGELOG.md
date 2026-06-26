# Changelog

## [1.0.0] - 2025-01-01

### Added
- `[Auditable]` attribute — marks a partial entity class for audit field generation
- `IAuditableEntity` interface — generated into consuming project; implemented by all `[Auditable]` entities
- Generated properties: `CreatedAt` (`DateTimeOffset`), `UpdatedAt` (`DateTimeOffset`), `CreatedBy` (`string?`), `UpdatedBy` (`string?`)
- `AuditInterceptor` — `SaveChangesInterceptor` that sets audit fields on insert/update; accepts optional `Func<string?>` for current user
- `AuditInterceptorExtensions.AddAuditInterceptor()` — extension method for `DbContextOptionsBuilder`
- `AUDIT001` diagnostic — compile-time error when `[Auditable]` is applied to a non-partial class
