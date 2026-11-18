namespace BlazorSuperHeroApi.Application.Heroes;

public sealed class SuperHeroServiceException : Exception
{
    public SuperHeroServiceException(
        string message,
        Exception? innerException = null,
        bool isAccessDenied = false)
        : base(message, innerException)
    {
        IsAccessDenied = isAccessDenied;
    }

    public bool IsAccessDenied { get; }
}
