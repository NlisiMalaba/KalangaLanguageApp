using Kalanga.Application.Ports.Out;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Tests.Application;

public sealed class RepositoryPortTenantIsolationTests
{
    private static readonly HashSet<string> NonTenantPorts =
    [
        nameof(IPasswordHasher),
        nameof(ITokenService),
        nameof(IContentPackManifestSigner),
        nameof(IUnitOfWork),
        nameof(ILanguageRepository),
    ];

    [Fact]
    public void Every_tenant_scoped_output_port_method_requires_language_id()
    {
        var ports = typeof(ILessonRepository).Assembly
            .GetTypes()
            .Where(type => type.IsInterface
                && type.Namespace == typeof(ILessonRepository).Namespace
                && !NonTenantPorts.Contains(type.Name))
            .ToArray();

        Assert.True(ports.Length >= 11, "Expected the original repository ports to remain registered.");

        foreach (var port in ports)
        {
            foreach (var method in port.GetMethods())
            {
                var hasLanguageId = method.GetParameters()
                    .Any(parameter => parameter.ParameterType == typeof(LanguageId));

                Assert.True(
                    hasLanguageId,
                    $"{port.Name}.{method.Name} must accept {nameof(LanguageId)}.");
            }
        }
    }
}
