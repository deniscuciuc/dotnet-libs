using CoreLibs.Identity.Base;

namespace CoreLibs.Identity.Validators.Production;

public class ServiceProdAuthenticationOptions : ServiceBaseAuthenticationOptions
{
    public new const string SchemeName = "InternalJWT";
}
