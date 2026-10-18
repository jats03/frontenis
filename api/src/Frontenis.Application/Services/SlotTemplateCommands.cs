using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.ISlotTemplateCommands"/>.</summary>
public sealed class SlotTemplateCommands : ISlotTemplateCommands
{
    private readonly ISlotTemplateRepository _templates;
    private readonly IUnitOfWork _uow;

    public SlotTemplateCommands(ISlotTemplateRepository templates, IUnitOfWork uow)
    {
        _templates = templates;
        _uow = uow;
    }

    public async Task<SlotTemplateDto> CreateAsync(SaveSlotTemplateRequest r, CancellationToken ct = default)
    {
        var template = SlotTemplate.Create(r.Day, r.Start, r.End);
        await _templates.AddAsync(template, ct);
        await _uow.SaveChangesAsync(ct);
        return template.ToDto();
    }

    public async Task<SlotTemplateDto> UpdateAsync(int id, SaveSlotTemplateRequest r, CancellationToken ct = default)
    {
        var template = await _templates.GetOrThrowAsync(id, ct);
        template.Update(r.Day, r.Start, r.End, r.Active);
        await _uow.SaveChangesAsync(ct);
        return template.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _templates.Remove(await _templates.GetOrThrowAsync(id, ct));
        await _uow.SaveChangesAsync(ct);
    }
}
