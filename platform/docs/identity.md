# CoreLibs.Identity

Multi-scheme token validation pipeline — JWT (Auth0), Firebase, Telegram, API keys, anonymous, and internal service tokens — with a unified `IIdentityDecoder` interface.

## Setup

```csharp
services.AddCoreIdentity(configuration);
services.AddCoreTokenValidators();
```

**appsettings.yml:**

```yaml
identity:
  service:
    Name: my-service
    InternalSecret: "..."
  auth0:
    Domain: "your-tenant.auth0.com"
    Audience: "https://api.yourapp.com"
  telegram:
    BotToken: "..."
  anonymous:
    Enabled: true
  apitoken:
    Tokens:
      - "sk-..."
```

---

## Token validator chain

Validators are resolved from DI, ordered by `Order`, and tried in sequence. The first one that returns a valid identity wins.

| Validator | Order | Description |
|---|---|---|
| `AnonymousJwtTokenValidator` | 1 | Unsigned JWT for guest/anonymous users |
| `TelegramJWTValidator` | 2 | Telegram `initData` wrapped as JWT |
| `OrganizationApiKeyJwtTokenValidator` | 3 | Org-scoped API key |
| `OpenCompositeTokenValidator` | 4 | Auth0 / Firebase composite token |
| `InternalSecretValidator` | 5 | Service-to-service shared secret |

---

## Custom validator

```csharp
public class MyTokenValidator : IOrderedTokenValidator
{
    public int Order => 10;

    public Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken ct)
    {
        // return null to pass to the next validator
        ...
    }
}

services.AddSingleton<IOrderedTokenValidator, MyTokenValidator>();
```

---

## Decode identity in handlers

```csharp
public class MyController(IIdentityDecoder decoder) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var identity = await decoder.DecodeAsync(HttpContext);
        return Ok(identity.UserId);
    }
}
```
