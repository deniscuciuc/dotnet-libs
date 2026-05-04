using DenisCuciuc.Platform.Identity.Base;

namespace DenisCuciuc.Platform.Identity.Validators.Production;

public class ServiceProdAuthenticationOptions : ServiceBaseAuthenticationOptions
{
    public new const string SchemeName = "InternalJWT";
}
