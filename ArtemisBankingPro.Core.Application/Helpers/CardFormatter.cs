namespace ArtemisBankingPro.Core.Application.Helpers;

public static class CardFormatter
{
    public static string LastFour(string cardNumber) =>
        cardNumber.Length <= 4 ? cardNumber : cardNumber[^4..];

    /// <summary>
    /// Formato LITERAL del spec para listados: ***********1234 (11 asteriscos + últimos 4).
    /// Se usa el literal del documento, no la longitud real del número.
    /// </summary>
    public static string Mask(string cardNumber) =>
        "***********" + LastFour(cardNumber);

    /// <summary>Fecha de expiración en formato MM/AA.</summary>
    public static string ExpirationDisplay(DateTime expirationDate) =>
        expirationDate.ToString("MM/yy");
}
