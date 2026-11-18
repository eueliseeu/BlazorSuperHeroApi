namespace BlazorSuperHeroApi.Application.Heroes;

public interface ISuperHeroService
{
    Task<IReadOnlyList<HeroProfile>> SearchAsync(string name, CancellationToken cancellationToken);
}
