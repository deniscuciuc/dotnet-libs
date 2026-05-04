# DenisCuciuc.Platform Messaging

Two complementary messaging libraries:

| Library | Purpose |
|---|---|
| `DenisCuciuc.Platform.MQ` | Async message bus - RabbitMQ via MassTransit |
| `DenisCuciuc.Platform.CQRS` | In-process commands/queries - MediatR + FluentValidation + ErrorOr |

---

## DenisCuciuc.Platform.MQ - Message Queue

### Setup

```csharp
services.AddPlatformRabbitMq(
        builder.Configuration,
        consumers => consumers
                .AddConsumer<OrderCreatedConsumer>()
                .AddConsumer<PaymentProcessedConsumer>());
```

Default section path: `MQ:Rabbit`

```yaml
MQ:
    Rabbit:
        host: localhost
        virtualHost: /
        username: guest
        password: guest
        endpointDefaults:
            prefetchCount: 1
            retryCount: 3
            retryIntervalInSeconds: 3
            queueType: Classic
```

### Consumer

```csharp
[RabbitMqConsumer("orders.created", RetryCount = 5, RetryIntervalInSeconds = 2)]
public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var order = context.Message;
        // process...
    }
}
```

### Publish

```csharp
public class OrderService(IPublishEndpoint bus)
{
    public async Task PlaceOrderAsync(Order order)
    {
        await bus.Publish(new OrderCreated { OrderId = order.Id });
    }
}
```

---

## DenisCuciuc.Platform.CQRS - Commands & Queries

Built on MediatR with `ErrorOr<T>` return types, options-driven pipeline behaviors, and platform cache integration.

### Setup

```csharp
services.AddPlatformCqrs(
        builder.Configuration,
        typeof(Program).Assembly);
// Scans assembly for IRequestHandler and IValidator implementations
```

Default options section: `Cqrs`

```yaml
Cqrs:
    enableLoggingBehavior: true
    enableValidationBehavior: true
    enableCachingBehavior: true
    enableDetailedErrorLogging: false
    defaultBatchMaxSize: 100
    caching:
        enabled: true
        keyPrefix: cqrs
        maxJitterSeconds: 5
        defaultAbsoluteExpiration: 00:05:00
        cacheErrors: false
```

### Command

```csharp
public sealed record CreateUserCommand(string Email, string Name) : ICommand<User>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateUserHandler(IUserRepository repo)
    : ICommandHandler<CreateUserCommand, User>
{
    public async Task<ErrorOr<User>> Handle(
        CreateUserCommand cmd, CancellationToken ct)
    {
        var user = new User { Email = cmd.Email, Name = cmd.Name };
        await repo.InsertOneAsync(user);
        return user;
    }
}
```

### Query

```csharp
public sealed record GetUserQuery(string Id) : IQuery<User>;

public class GetUserHandler(IUserRepository repo)
    : IQueryHandler<GetUserQuery, User>
{
    public async Task<ErrorOr<User>> Handle(GetUserQuery q, CancellationToken ct)
    {
        var user = await repo.FindByIdAsync(ObjectId.Parse(q.Id));
        return user is null ? Error.NotFound() : user;
    }
}
```

### Cacheable query

```csharp
public sealed record GetUserProfileQuery(Guid UserId) : ICacheableQuery<UserProfile>
{
    public QueryCachePolicy CachePolicy => QueryCachePolicy.Absolute(
        key: $"user-profile:{UserId}",
        expiration: TimeSpan.FromMinutes(5));
}
```

### Dispatch

```csharp
public class UsersController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserCommand cmd)
        => (await mediator.Send(cmd)).Match(Ok, err => Problem(err));
}
```

Validation runs automatically in the MediatR pipeline - returns `Error.Validation(...)` before the handler is called if any `IValidator<T>` fails.
