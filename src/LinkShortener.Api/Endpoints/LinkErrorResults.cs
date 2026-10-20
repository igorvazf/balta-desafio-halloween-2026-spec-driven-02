using LinkShortener.Api.Application;

namespace LinkShortener.Api.Endpoints;

/// <summary>
/// Traduz o erro do serviço em resposta HTTP com ProblemDetails.
/// </summary>
public static class LinkErrorResults
{
    public static IResult ToResult(LinkError error) => error.Type switch
    {
        LinkErrorType.Validation => ValidationProblem(error.Field ?? string.Empty, error.Message),
        LinkErrorType.Conflict => Results.Problem(error.Message, statusCode: StatusCodes.Status409Conflict, title: "Código em uso"),
        LinkErrorType.NotFound => Results.Problem(error.Message, statusCode: StatusCodes.Status404NotFound, title: "Link não encontrado"),
        LinkErrorType.Expired => Results.Problem(error.Message, statusCode: StatusCodes.Status410Gone, title: "Link expirado"),
        LinkErrorType.CodeGenerationFailed => Results.Problem(error.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "Código indisponível"),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, null),
    };

    public static IResult ValidationProblem(string field, string message) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { [field] = [message] },
            detail: message,
            title: "Requisição inválida");
}
