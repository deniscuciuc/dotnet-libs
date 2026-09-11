# Telegram Integration
## Setup
```csharp
services.AddCoreLocalizationTelegram<MyUserContext>();
```
Implement `ITelegramUserContext`:
```csharp
public class MyUserContext : ITelegramUserContext
{
    public string? GetUserCulture(long userId)
        => _userService.GetLanguageCode(userId); // e.g. "ru"
}
```
## Usage
```csharp
public class MyHandler(TelegramLocalizer localizer)
{
    public string GetGreeting(long userId, string name)
        => localizer.ForUser(userId).Get("telegram.greeting", new { name });
}
```
`ForUser(userId)` auto-resolves the culture and returns a `UserLocalizer` scoped to that user.
