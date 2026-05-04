using System.Text.Encodings.Web;
using DenisCuciuc.Platform.Identity.Base;
using DenisCuciuc.Platform.Identity.Token;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.Identity.Validators.Production;

public class ServiceProdAuthenticationHandler(
    IOptionsMonitor<ServiceProdAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TimeProvider timeProvider,
    IList<ITokenValidator> tokenValidators,
    IMemoryCache cache
) : ServiceBaseAuthenticationHandler<ServiceProdAuthenticationOptions>(options, logger, encoder, timeProvider,
    tokenValidators, cache)
{
    protected override bool IsTokenValid(ServiceToken serviceToken)
    {
        return true;
    }
}
