using System.Text.Json;
using BlazorSuperHeroApi.Application.Heroes;

namespace BlazorSuperHeroApi.Server.Endpoints;

public static class HeroImageEndpoints
{
    public static IEndpointRouteBuilder MapHeroImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/heroes/{id}/image", GetImageUrlAsync)
            .WithName("GetHeroImageUrl")
            .WithSummary("Redirects to a character image in the Akabab image catalog.")
            .WithDescription("Returns a redirect to the pinned jsDelivr image for the character ID.")
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return endpoints;
    }

    private static async Task<IResult> GetImageUrlAsync(
        string id,
        IHeroImageCatalog imageCatalog,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(HeroImageEndpoints));

        try
        {
            var imageUrl = await imageCatalog.GetImageUrlAsync(id, cancellationToken);
            if (imageUrl is null)
            {
                return Results.NotFound();
            }

            httpContext.Response.Headers["Cache-Control"] = "public, max-age=86400";
            return Results.Redirect(imageUrl);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException exception)
        {
            logger.LogWarning(
                "Hero image catalog request timed out for hero {HeroId} with {ExceptionType}.",
                id,
                exception.GetType().Name);

            return Results.Problem(
                title: "Imagem indisponível.",
                detail: "Não foi possível carregar a imagem do personagem.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException)
        {
            logger.LogWarning(
                "Hero image catalog request failed for hero {HeroId} with status {StatusCode} and {ExceptionType}.",
                id,
                (exception as HttpRequestException)?.StatusCode,
                exception.GetType().Name);

            return Results.Problem(
                title: "Imagem indisponível.",
                detail: "Não foi possível carregar a imagem do personagem.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
