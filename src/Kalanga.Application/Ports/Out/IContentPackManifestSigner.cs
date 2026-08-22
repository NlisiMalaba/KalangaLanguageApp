namespace Kalanga.Application.Ports.Out;

public interface IContentPackManifestSigner
{
    string Algorithm { get; }

    string Sign(ReadOnlySpan<byte> canonicalUtf8);
}
