using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlazorSuperHeroApi.Application.Heroes;

namespace BlazorSuperHeroApi.Infrastructure.SuperHeroApi;

internal sealed class SuperHeroService(HttpClient httpClient) : ISuperHeroService
{
    public async Task<IReadOnlyList<HeroProfile>> SearchAsync(
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"search/{Uri.EscapeDataString(name)}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Array.Empty<HeroProfile>();
            }

            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<SearchResponse>(
                cancellationToken: cancellationToken);

            if (searchResponse is null)
            {
                throw new SuperHeroServiceException(
                    "The superhero service returned an empty response.");
            }

            if (string.Equals(searchResponse.Response, "success", StringComparison.OrdinalIgnoreCase))
            {
                return searchResponse.Results ?? Array.Empty<HeroProfile>();
            }

            if (searchResponse.Error?.Contains(
                    "character with given name not found",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                return Array.Empty<HeroProfile>();
            }

            var isAccessDenied = searchResponse.Error?.Contains(
                "access denied",
                StringComparison.OrdinalIgnoreCase) == true;

            throw new SuperHeroServiceException(
                isAccessDenied
                    ? "The SuperHeroAPI rejected the configured access token."
                    : searchResponse.Error ?? "The superhero service could not complete the search.",
                isAccessDenied: isAccessDenied);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SuperHeroServiceException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException
            or JsonException
            or TaskCanceledException)
        {
            throw new SuperHeroServiceException(
                "The superhero service is temporarily unavailable.",
                exception);
        }
    }

    private sealed class SearchResponse
    {
        public string? Response { get; init; }

        public string? Error { get; init; }

        public IReadOnlyList<HeroProfile>? Results { get; init; }
    }
}
