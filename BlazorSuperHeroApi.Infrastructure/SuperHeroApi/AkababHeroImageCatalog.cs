using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BlazorSuperHeroApi.Application.Heroes;
using Microsoft.Extensions.Caching.Memory;

namespace BlazorSuperHeroApi.Infrastructure.SuperHeroApi;

internal sealed class AkababHeroImageCatalog(HttpClient httpClient, IMemoryCache cache)
    : IHeroImageCatalog
{
    private const string ImagePathPrefix = "/gh/akabab/superhero-api@0.3.0/api/images/md/";
    private static readonly Regex AllowedImagePath = new(
        "^/gh/akabab/superhero-api@0\\.3\\.0/api/images/md/[0-9]+-[a-z0-9-]+\\.jpg$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public async Task<string?> GetImageUrlAsync(
        string? heroId,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(heroId, out var id) || id <= 0)
        {
            return null;
        }

        var cacheKey = $"hero-image:{id}";
        if (cache.TryGetValue(cacheKey, out string? cachedUrl))
        {
            return cachedUrl;
        }

        using var response = await httpClient.GetAsync($"id/{id}.json", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var hero = await response.Content.ReadFromJsonAsync<AkababHero>(
            cancellationToken: cancellationToken);
        var imageUrl = hero?.Images?.Md;

        if (imageUrl is null)
        {
            return null;
        }

        if (!IsAllowedImageUrl(imageUrl))
        {
            throw new HttpRequestException("The image catalog returned an unsupported image URL.");
        }

        cache.Set(cacheKey, imageUrl, TimeSpan.FromDays(7));
        return imageUrl;
    }

    private static bool IsAllowedImageUrl(string imageUrl) =>
        Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host.Equals("cdn.jsdelivr.net", StringComparison.OrdinalIgnoreCase)
        && uri.Port == 443
        && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment)
        && AllowedImagePath.IsMatch(uri.AbsolutePath);

    private sealed class AkababHero
    {
        public AkababImages? Images { get; init; }
    }

    private sealed class AkababImages
    {
        public string? Md { get; init; }
    }
}
