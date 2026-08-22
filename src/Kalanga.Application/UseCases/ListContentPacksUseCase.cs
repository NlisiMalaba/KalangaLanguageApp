using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Kalanga.Domain.Exceptions;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[Authorize]
public sealed class ListContentPacksUseCase(IContentPackRepository packs) : ListContentPacksPort
{
    public async Task<ListContentPacksResult> ExecuteAsync(
        ListContentPacksCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureTenant(command.LanguageId, command.RequestedLanguageId);

        var category = string.IsNullOrWhiteSpace(command.Category) ? null : command.Category.Trim();
        var items = await packs.FindAsync(command.LanguageId, command.Level, category, cancellationToken);

        return new ListContentPacksResult(
            items
                .Select(static pack => new ContentPackListItemDto(
                    pack.Id,
                    pack.LanguageId,
                    pack.Name,
                    pack.Level,
                    pack.Category,
                    pack.Version,
                    pack.SizeBytes,
                    pack.ManifestUrl))
                .ToList());
    }

    internal static void EnsureTenant(LanguageId tenant, LanguageId? requested)
    {
        if (requested is { } requestedLanguage && requestedLanguage != tenant)
        {
            throw new TenantAccessDeniedException();
        }
    }
}
