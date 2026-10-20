namespace LinkShortener.Api.OpenApi;

/// <summary>
/// Descrição, em português, de uma resposta do endpoint na documentação OpenAPI.
/// </summary>
public sealed record ResponseDescriptionMetadata(int StatusCode, string Description);

public static class ResponseDescriptionExtensions
{
    public static RouteHandlerBuilder WithResponseDescription(this RouteHandlerBuilder builder, int statusCode, string description) =>
        builder.WithMetadata(new ResponseDescriptionMetadata(statusCode, description));
}
