using Kalanga.Application.Ports.Out;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Tests.Application;

public sealed class RepositoryPortTenantIsolationTests
{
    [Fact]
    public void Every_output_port_method_requires_language_id()
    {
        var ports = typeof(ILessonRepository).Assembly
            .GetTypes()
            .Where(type => type.IsInterface && type.Namespace == typeof(ILessonRepository).Namespace)
            .ToArray();

        Assert.Equal(11, ports.Length);

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
