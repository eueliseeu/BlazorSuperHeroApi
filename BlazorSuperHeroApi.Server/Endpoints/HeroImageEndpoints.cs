using BlazorSuperHeroApi.Application.Heroes;

namespace BlazorSuperHeroApi.Server.Endpoints;

public static class HeroImageEndpoints
{
    public static IEndpointRouteBuilder MapHeroImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/heroes/image", GetImageAsync)
            .WithName("GetHeroImage")
            .WithSummary("Retrieves a SuperHeroDB character image through the application server.")
            .WithDescription("Only portrait images hosted on www.superherodb.com are supported.")
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .Produces(StatusCodes.Status200OK, contentType: "image/gif")
            .Produces(StatusCodes.Status200OK, contentType: "image/webp")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return endpoints;
    }

    private static async Task<IResult> GetImageAsync(
        string? url,
        IHeroImageProxy imageProxy,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(HeroImageEndpoints));

        if (string.IsNullOrWhiteSpace(url) || !imageProxy.IsAllowedSource(url))
        {
            return Results.Problem(
                title: "URL de imagem inválida.",
                detail: "Informe uma imagem de personagem hospedada na SuperHeroDB.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var image = await imageProxy.FetchAsync(url, cancellationToken);
            if (image is null)
            {
                return Results.NotFound();
            }

            httpContext.Response.Headers["Cache-Control"] = "public, max-age=86400";
            httpContext.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
            httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";

            return Results.File(image.Content, image.ContentType);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException exception)
        {
            logger.LogWarning(
                "Hero image request timed out with {ExceptionType}.",
                exception.GetType().Name);

            return Results.Problem(
                title: "Imagem indisponível.",
                detail: "Não foi possível carregar a imagem do personagem.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            logger.LogWarning(
                "Hero image request failed with status {StatusCode} and {ExceptionType}.",
                (exception as HttpRequestException)?.StatusCode,
                exception.GetType().Name);

            return Results.Problem(
                title: "Imagem indisponível.",
                detail: "Não foi possível carregar a imagem do personagem.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
