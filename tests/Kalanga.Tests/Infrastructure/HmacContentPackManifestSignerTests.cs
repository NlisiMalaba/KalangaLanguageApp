using System.Text;
using Kalanga.Application;
using Kalanga.Application.Dtos;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Kalanga.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Kalanga.Tests.Infrastructure;

public sealed class HmacContentPackManifestSignerTests
{
    [Fact]
    public void Signature_is_stable_hmac_sha256_hex_of_canonical_json()
    {
        var signer = new HmacContentPackManifestSigner(Options.Create(new ContentPacksOptions
        {
            SigningKey = "DEV-ONLY-content-pack-hmac-key-32b!",
        }));

        var packId = ContentPackId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var languageId = LanguageId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var lessonId = LessonId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var audioId = AudioRecordingId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var generatedAt = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

        var manifest = new GetContentPackManifestResult(
            packId,
            languageId,
            "Beginner Everyday",
            Version: 1,
            Level.Beginner,
            "Everyday",
            generatedAt,
            SizeBytes: 2048,
            [
                new ContentPackManifestLessonDto(
                    lessonId,
                    [
                        new ContentPackManifestAudioDto(
                            audioId,
                            "https://cdn.example/a.mp3",
                            AudioFileFormat.Mp3,
                            2048),
                    ]),
            ],
            Signature: string.Empty,
            signer.Algorithm);

        var canonical = ContentPackManifestCanonical.Write(manifest);
        var signature = signer.Sign(canonical);

        Assert.Equal("HMAC-SHA256", signer.Algorithm);
        Assert.Equal(64, signature.Length);
        Assert.Equal(signature, signer.Sign(canonical));
        Assert.NotEqual(signature, signer.Sign(Encoding.UTF8.GetBytes("tampered")));
        Assert.DoesNotContain("signature", Encoding.UTF8.GetString(canonical), StringComparison.OrdinalIgnoreCase);
    }
}
