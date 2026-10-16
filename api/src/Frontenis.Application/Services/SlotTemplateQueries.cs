using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.ISlotTemplateQueries"/>.</summary>
public sealed class SlotTemplateQueries : ISlotTemplateQueries
{
    private readonly ISlotTemplateRepository _templates;
    public SlotTemplateQueries(ISlotTemplateRepository templates) { _templates = templates; }

    public async Task<IReadOnlyList<SlotTemplateDto>> ListAsync(CancellationToken ct = default) =>
        (await _templates.ListAsync(ct)).Select(t => t.ToDto()).ToList();
}
