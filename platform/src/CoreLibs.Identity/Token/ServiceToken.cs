using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreLibs.Identity.Identity;

namespace CoreLibs.Identity.Token;

public sealed class ServiceToken
{
    [JsonPropertyName("uid")] public string Uid { get; set; } = null!;

    [JsonPropertyName("name")] public string Name { get; set; } = null!;

    [JsonPropertyName("m")] public string Metadata { get; set; } = null!;

    [JsonPropertyName("lp")] public string? LoginProvider { get; set; }

    [JsonPropertyName("e")] public string Email { get; set; } = null!;

    [JsonPropertyName("reg")] public bool Registered { get; set; }

    [JsonPropertyName("s")] public string Secret { get; set; } = null!;

    [JsonPropertyName("exp")] public DateTime ExpireAt { get; set; }

    public static bool TryParse(string value, out ServiceToken? token)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);

            var data = Encoding.UTF8.GetString(bytes, 8, bytes.Length - 40);

            token = JsonSerializer.Deserialize<ServiceToken>(data);
        }
        catch
        {
            token = null;
        }

        return token != null;
    }

    public IdentityClaims GetIdentity()
    {
        return new IdentityClaims(new Dictionary<string, string>
        {
            { IdentityKeys.UserId, Uid },
            { IdentityKeys.UserName, Name },
            { IdentityKeys.LoginProvider, LoginProvider! },
            { IdentityKeys.Email, Email },
            { IdentityKeys.Metadata, Metadata }
        });
    }
}
