# Swevo.AutoAudit

[![NuGet](https://img.shields.io/nuget/v/Swevo.AutoAudit.svg)](https://www.nuget.org/packages/Swevo.AutoAudit/)
[![Build](https://github.com/Swevo/Swevo.AutoAudit/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/Swevo.AutoAudit/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Compile-time audit field generation for EF Core entities using Roslyn source generators. Add `[Auditable]` to any `partial` entity class and get `CreatedAt`, `UpdatedAt`, `CreatedBy`, and `UpdatedBy` properties — plus a ready-to-use `AuditInterceptor` — all generated at build time. Zero reflection, AOT-safe, no runtime overhead.

---

## Installation

```bash
dotnet add package Swevo.AutoAudit
```

Requires EF Core 7+.

---

## Quick Start

### 1. Mark your entity

```csharp
using Swevo.AutoAudit;

[Auditable]
public partial class Order
{
    public int Id { get; set; }
    public string? Description { get; set; }
}
```

The generator adds these properties automatically — no base class required:

```csharp
// Generated:
partial class Order : IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
```

### 2. Register the interceptor

```csharp
// Program.cs / Startup.cs
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);

    // Optional: supply the current user identity
    var httpContext = sp.GetService<IHttpContextAccessor>();
    options.AddAuditInterceptor(() => httpContext?.HttpContext?.User?.Identity?.Name);
});
```

That's it. `CreatedAt`/`CreatedBy` are set on insert; `UpdatedAt`/`UpdatedBy` are refreshed on every update.

---

## How It Works

| Event | Fields Updated |
|---|---|
| `Added` | `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |
| `Modified` | `UpdatedAt`, `UpdatedBy` |

`CreatedAt` and `CreatedBy` are **never overwritten** on subsequent saves.

---

## Generated Types

The generator emits three shared sources into your project's `Swevo.AutoAudit` namespace:

| Type | Description |
|---|---|
| `[Auditable]` | The attribute itself |
| `IAuditableEntity` | Interface implemented by all auditable entities |
| `AuditInterceptor` | `SaveChangesInterceptor` that applies audit values |
| `AuditInterceptorExtensions` | `AddAuditInterceptor()` extension for `DbContextOptionsBuilder` |

---

## Diagnostics

| ID | Severity | Description |
|---|---|---|
| `AUDIT001` | Error | Class must be `partial` to use `[Auditable]` |

---

## Compatibility

| Dependency | Version |
|---|---|
| EF Core | 7.0+ |
| .NET | net7.0+ |
| C# | 10+ |

---

## License

MIT © 2025 Justin Bannister
