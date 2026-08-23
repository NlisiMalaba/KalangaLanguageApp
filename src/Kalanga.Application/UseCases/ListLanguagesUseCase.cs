using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Application.Ports.Out;
using Microsoft.AspNetCore.Authorization;

namespace Kalanga.Application.UseCases;

[AllowAnonymous]
public sealed class ListLanguagesUseCase(ILanguageRepository languages) : ListLanguagesPort
{
    public async Task<ListLanguagesResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var items = await languages.ListActiveAsync(cancellationToken);
        return new ListLanguagesResult(items.Select(static language => language.ToDto()).ToArray());
    }
}
