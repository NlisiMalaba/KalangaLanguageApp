using System.Security.Cryptography;
using System.Text;
using Kalanga.Application.Ports.Out;
using Microsoft.Extensions.Options;

namespace Kalanga.Infrastructure.Security;

internal sealed class HmacContentPackManifestSigner(IOptions<ContentPacksOptions> options) : IContentPackManifestSigner
{
    public const string HmacSha256 = "HMAC-SHA256";

    private readonly byte[] _key = Encoding.UTF8.GetBytes(RequireKey(options.Value.SigningKey));

    public string Algorithm => HmacSha256;

    public string Sign(ReadOnlySpan<byte> canonicalUtf8)
    {
        var hash = HMACSHA256.HashData(_key, canonicalUtf8);
        return Convert.ToHexStringLower(hash);
    }

    private static string RequireKey(string? signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("ContentPacks:SigningKey must be configured and at least 32 bytes.");
        }

        return signingKey;
    }
}
