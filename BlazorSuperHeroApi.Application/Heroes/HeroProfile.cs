using System.Text.Json.Serialization;

namespace BlazorSuperHeroApi.Application.Heroes;

public sealed class HeroProfile
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public HeroPowerStats? Powerstats { get; init; }

    public HeroBiography? Biography { get; init; }

    public HeroAppearance? Appearance { get; init; }

    public HeroWork? Work { get; init; }

    public HeroConnections? Connections { get; init; }

    public HeroImage? Image { get; init; }
}

public sealed class HeroPowerStats
{
    public string? Intelligence { get; init; }

    public string? Strength { get; init; }

    public string? Speed { get; init; }

    public string? Durability { get; init; }

    public string? Power { get; init; }

    public string? Combat { get; init; }
}

public sealed class HeroBiography
{
    [JsonPropertyName("full-name")]
    public string? FullName { get; init; }

    [JsonPropertyName("alter-egos")]
    public string? AlterEgos { get; init; }

    public string[]? Aliases { get; init; }

    [JsonPropertyName("place-of-birth")]
    public string? PlaceOfBirth { get; init; }

    [JsonPropertyName("first-appearance")]
    public string? FirstAppearance { get; init; }

    public string? Publisher { get; init; }

    public string? Alignment { get; init; }
}

public sealed class HeroAppearance
{
    public string? Gender { get; init; }

    public string? Race { get; init; }

    public string[]? Height { get; init; }

    public string[]? Weight { get; init; }

    [JsonPropertyName("eye-color")]
    public string? EyeColor { get; init; }

    [JsonPropertyName("hair-color")]
    public string? HairColor { get; init; }
}

public sealed class HeroWork
{
    public string? Occupation { get; init; }

    public string? Base { get; init; }
}

public sealed class HeroConnections
{
    [JsonPropertyName("group-affiliation")]
    public string? GroupAffiliation { get; init; }

    public string? Relatives { get; init; }
}

public sealed class HeroImage
{
    public string? Url { get; init; }
}
