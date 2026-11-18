using BlazorSuperHeroApi.Application.Heroes;
using BlazorSuperHeroApi.Server.Endpoints;
using BlazorSuperHeroApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Services.AddSuperHeroApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapHeroImageEndpoints();

app.MapGet("/api/heroes/search", async (
    string? name,
    ISuperHeroService superHeroService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
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
});

app.MapFallbackToFile("index.html");

app.Run();
