using System.Text.Encodings.Web;
using CoreLibs.Identity.Base;
using CoreLibs.Identity.Token;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreLibs.Identity.Validators.DebugValidator;

public class ServiceDebugAuthenticationHandler(
    IOptionsMonitor<ServiceDebugAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TimeProvider timeProvider,
    IList<ITokenValidator> tokenValidators,
    IMemoryCache cache
) : ServiceBaseAuthenticationHandler<ServiceDebugAuthenticationOptions>(options, logger, encoder, timeProvider,
    tokenValidators, cache);
