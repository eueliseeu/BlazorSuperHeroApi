namespace BlazorSuperHeroApi.Application.Heroes;

public interface IHeroImageProxy
{
    bool IsAllowedSource(string? sourceUrl);

    Task<HeroImageContent?> FetchAsync(string sourceUrl, CancellationToken cancellationToken);
}
