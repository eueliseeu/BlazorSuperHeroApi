namespace BlazorSuperHeroApi.Infrastructure.SuperHeroApi;

public sealed class SuperHeroApiOptions
{
    public const string SectionName = "SuperHeroApi";

    public string BaseUrl { get; init; } = "https://superheroapi.com/api";

    public string AccessToken { get; init; } = string.Empty;
}
