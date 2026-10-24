using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Models;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.ICompetitionCommands"/>.</summary>
public sealed class CompetitionCommands : ICompetitionCommands
{
    private readonly ICompetitionRepository _repository;
    private readonly IUnitOfWork _uow;

    public CompetitionCommands(ICompetitionRepository repository, IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<CompetitionSummaryDto> CreateAsync(CreateCompetitionRequest r, CancellationToken ct = default)
    {
        var competition = Competition.Create(new CompetitionSettings(
            r.Name, r.Type, r.PairCount, r.GroupCount, r.PairsPerGroup,
            r.PointsPerWin, r.PointsPerLoss, r.DoubleRound, r.QualifiersPerGroup, r.Rounds, r.SetsPerMatch, r.ManualRounds));
        await _repository.AddAsync(competition, ct);
        await _uow.SaveChangesAsync(ct);
        return competition.ToSummary();
    }

    public async Task<CompetitionSummaryDto> RenameAsync(int id, RenameCompetitionRequest request, CancellationToken ct = default)
    {
        var competition = await _repository.GetDetailedOrThrowAsync(id, ct);
        competition.Rename(request.Name);
        await _uow.SaveChangesAsync(ct);
        return competition.ToSummary();
    }

    public async Task<CompetitionSummaryDto> ChangeSetsPerMatchAsync(int id, SetsPerMatchRequest request, CancellationToken ct = default)
    {
        var competition = await _repository.GetDetailedOrThrowAsync(id, ct);
        competition.ChangeSetsPerMatch(request.SetsPerMatch);
        await _uow.SaveChangesAsync(ct);
        return competition.ToSummary();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _repository.GetDetailedOrThrowAsync(id, ct);
        await _repository.DeleteAsync(id, ct);
    }
}
