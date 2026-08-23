using Kalanga.Infrastructure;

namespace Kalanga.Api.Hosting;

internal sealed class DevelopmentLanguageSeedHostedService(
    IServiceScopeFactory scopes,
    ILogger<DevelopmentLanguageSeedHostedService> logger) : BackgroundService
{
    private static readonly int[] RetryDelaysMs = [1_000, 2_000, 4_000, 8_000, 16_000];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= RetryDelaysMs.Length; attempt++)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.SeedDevelopmentDataAsync(stoppingToken);
                logger.LogInformation("Development Kalanga language tenant is present.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (attempt < RetryDelaysMs.Length)
            {
                var cap = RetryDelaysMs[attempt - 1];
                var delay = TimeSpan.FromMilliseconds(Random.Shared.Next(cap + 1));
                logger.LogWarning(
                    ex,
                    "Development language seed failed (attempt {Attempt}/{MaxAttempts}). Postgres may still be starting or in recovery. Retrying.",
                    attempt,
                    RetryDelaysMs.Length);
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Development language seed failed after {Attempts} attempts. GET /languages will be empty until Postgres is up and the API is restarted.",
                    RetryDelaysMs.Length);
            }
        }
    }
}
