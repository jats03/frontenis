using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;

namespace Frontenis.Application.Mapping;

/// <summary>Conversión de plantillas semanales a DTO (las franjas se componen en <c>SlotDtoBuilder</c>, que necesita la hora y los nombres).</summary>
internal static class SlotMapper
{
    public static SlotTemplateDto ToDto(this SlotTemplate t) => new(t.Id, t.Day, t.Start, t.End, t.Active);
}
