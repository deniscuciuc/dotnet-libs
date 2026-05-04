using System.Text.Encodings.Web;
using DenisCuciuc.Platform.Identity.Base;
using DenisCuciuc.Platform.Identity.Token;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DenisCuciuc.Platform.Identity.Validators.Internal;

public class ServiceInternalAuthenticationHandler(
    IOptionsMonitor<ServiceInternalAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TimeProvider timeProvider,
    IList<ITokenValidator> tokenValidators,
    IMemoryCache cache
) : ServiceBaseAuthenticationHandler<ServiceInternalAuthenticationOptions>(options, logger, encoder, timeProvider,
    tokenValidators, cache);
