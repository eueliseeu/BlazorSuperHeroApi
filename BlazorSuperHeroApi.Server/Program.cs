using BlazorSuperHeroApi.Infrastructure;
using BlazorSuperHeroApi.Server.Endpoints;

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
app.MapHeroSearchEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
