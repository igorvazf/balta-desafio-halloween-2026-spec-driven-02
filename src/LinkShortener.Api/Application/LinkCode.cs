namespace LinkShortener.Api.Application;

/// <summary>
/// Formato do código de um link (RF-02, RF-03, RN-05).
/// </summary>
public static class LinkCode
{
    /// <summary>
    /// Letras maiúsculas de A a Z e dígitos de 1 a 9. O 0 fica de fora para não ser confundido com o O (D-03).
    /// </summary>
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

    public const int GeneratedLength = 7;

    public const int CustomMinLength = 4;

    public const int CustomMaxLength = 20;

    /// <summary>
    /// Valida um código informado pelo criador. Devolve a mensagem de erro, ou nulo quando o código é válido.
    /// </summary>
    public static string? ValidateCustom(string code)
    {
        if (code.Length is < CustomMinLength or > CustomMaxLength)
        {
            return $"O código deve ter entre {CustomMinLength} e {CustomMaxLength} caracteres.";
        }

        if (!code.All(Alphabet.Contains))
        {
            return "O código deve conter apenas letras maiúsculas de A a Z e dígitos de 1 a 9, sem espaços.";
        }

        return null;
    }
}
