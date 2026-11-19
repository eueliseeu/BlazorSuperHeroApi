using BlazorSuperHeroApi.Application.Heroes;

namespace BlazorSuperHeroApi.Server.Endpoints;

public static class HeroSearchEndpoints
{
    public static IEndpointRouteBuilder MapHeroSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/heroes/search", SearchAsync)
            .WithName("SearchHeroes")
            .WithSummary("Searches for heroes by name.")
            .WithDescription("Returns the biography, appearance, powers, work, and connections for matching heroes.")
            .Produces<IReadOnlyList<HeroProfile>>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return endpoints;
    }

    private static async Task<IResult> SearchAsync(
        string? name,
        ISuperHeroService superHeroService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(HeroSearchEndpoints));

        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Informe o nome do herói para iniciar a busca."]
            });
        }

        try
        {
            var heroes = await superHeroService.SearchAsync(name.Trim(), cancellationToken);
            return Results.Ok(heroes);
        }
        catch (SuperHeroServiceException exception)
        {
            if (exception.IsAccessDenied)
            {
                logger.LogWarning("SuperHeroAPI rejected the configured access token.");

                return Results.Problem(
                    title: "A SuperHeroAPI recusou o token configurado.",
                    detail: "Verifique se o token está ativo e configurado no projeto BlazorSuperHeroApi.Server.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            logger.LogError(
                "SuperHeroAPI search failed with {ExceptionType}.",
                exception.InnerException?.GetType().Name ?? exception.GetType().Name);

            return Results.Problem(
                title: "Não foi possível consultar a SuperHeroAPI.",
                detail: "Tente novamente em instantes.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
