using System.Text.Encodings.Web;
using DenisCuciuc.Platform.Identity.Base;
using DenisCuciuc.Platform.Identity.Token;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.Identity.Validators.Telegram;

public class TelegramAuthenticationHandler(
    IOptionsMonitor<TelegramAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TimeProvider timeProvider,
    ITelegramJWTValidator tokenValidator,
    IMemoryCache cache
) : ServiceBaseAuthenticationHandler<TelegramAuthenticationOptions>(options, logger, encoder, timeProvider,
    [tokenValidator],
    cache)
{
    protected override bool IsTokenValid(ServiceToken serviceToken)
    {
        return true;
    }
}
