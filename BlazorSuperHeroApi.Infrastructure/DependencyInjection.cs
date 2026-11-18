using BlazorSuperHeroApi.Application.Heroes;
using BlazorSuperHeroApi.Infrastructure.SuperHeroApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazorSuperHeroApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSuperHeroApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SuperHeroApiOptions>()
            .Bind(configuration.GetSection(SuperHeroApiOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps,
                "SuperHeroApi:BaseUrl must be an absolute HTTPS URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.AccessToken),
                "Configure the SuperHeroAPI access token using SuperHeroApi:AccessToken.")
            .ValidateOnStart();

        services.AddHttpClient<ISuperHeroService, SuperHeroService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SuperHeroApiOptions>>().Value;
            client.BaseAddress = new Uri(
                $"{options.BaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(options.AccessToken)}/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddHttpClient<IHeroImageProxy, SuperHeroImageProxy>(client =>
        {
            client.BaseAddress = new Uri("https://www.superherodb.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        return services;
    }
}
