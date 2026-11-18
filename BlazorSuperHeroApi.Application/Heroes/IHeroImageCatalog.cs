namespace BlazorSuperHeroApi.Application.Heroes;

public interface IHeroImageCatalog
{
    Task<string?> GetImageUrlAsync(string? heroId, CancellationToken cancellationToken);
}
