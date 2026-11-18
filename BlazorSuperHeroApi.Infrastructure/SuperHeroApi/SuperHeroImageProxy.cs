using System.Net;
using System.Text.RegularExpressions;
using BlazorSuperHeroApi.Application.Heroes;

namespace BlazorSuperHeroApi.Infrastructure.SuperHeroApi;

internal sealed class SuperHeroImageProxy(HttpClient httpClient) : IHeroImageProxy
{
    private const int MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly Regex AllowedImagePath = new(
        "^/pictures2/portraits/[0-9]+/[0-9]+/[0-9]+\\.(jpg|jpeg|png|gif|webp)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    public bool IsAllowedSource(string? sourceUrl) =>
        TryGetImagePath(sourceUrl, out _);

    public async Task<HeroImageContent?> FetchAsync(
        string sourceUrl,
        CancellationToken cancellationToken)
    {
        if (!TryGetImagePath(sourceUrl, out var imagePath))
        {
            throw new ArgumentException("The image source is not allowed.", nameof(sourceUrl));
        }

        using var response = await httpClient.GetAsync(
            imagePath,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "The image provider returned an unsuccessful response.",
                null,
                response.StatusCode);
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is null || !AllowedContentTypes.Contains(contentType))
        {
            throw new HttpRequestException("The image provider returned an unsupported content type.");
        }

        if (response.Content.Headers.ContentLength > MaxImageSizeBytes)
        {
            throw new HttpRequestException("The image provider returned an image that is too large.");
        }

        var content = await ReadLimitedAsync(response.Content, cancellationToken);
        return new HeroImageContent(content, contentType);
    }

    private static bool TryGetImagePath(string? sourceUrl, out string imagePath)
    {
        imagePath = string.Empty;

        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return false;
        }

        var normalizedUrl = sourceUrl.Replace("\\/", "/", StringComparison.Ordinal).Trim();
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !uri.Host.Equals("www.superherodb.com", StringComparison.OrdinalIgnoreCase)
            || uri.Port != 443
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || !AllowedImagePath.IsMatch(uri.AbsolutePath))
        {
            return false;
        }

        imagePath = uri.AbsolutePath;
        return true;
    }

    private static async Task<byte[]> ReadLimitedAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        using var destination = new MemoryStream();
        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (destination.Length + bytesRead > MaxImageSizeBytes)
            {
                throw new HttpRequestException("The image provider returned an image that is too large.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        return destination.ToArray();
    }
}
