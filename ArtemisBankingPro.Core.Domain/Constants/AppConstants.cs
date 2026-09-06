namespace ArtemisBankingPro.Core.Domain.Constants;

public static class AppConstants
{
    /// <summary>Tamaño de página obligatorio en todos los listados (web y API).</summary>
    public const int PageSize = 20;

    /// <summary>Interés fijo aplicado a los avances de efectivo (6.25%).</summary>
    public const decimal CashAdvanceInterestRate = 0.0625m;

    /// <summary>Vigencia máxima del token de restablecimiento de contraseña.</summary>
    public const int ResetTokenExpirationMinutes = 30;

    /// <summary>Años de vigencia de una tarjeta de crédito desde su asignación.</summary>
    public const int CardExpirationYears = 3;

    public const int AccountNumberLength = 9;
    public const int CardNumberLength = 16;
    public const int CvcLength = 3;

    /// <summary>Plazos de préstamo permitidos: 6 a 60 meses en intervalos de 6.</summary>
    public static readonly int[] AllowedLoanTerms = [6, 12, 18, 24, 30, 36, 42, 48, 54, 60];

    /// <summary>Texto de origen para depósitos por cajero.</summary>
    public const string DepositOrigin = "DEPÓSITO";

    /// <summary>Texto de beneficiario para retiros por cajero.</summary>
    public const string WithdrawalBeneficiary = "RETIRO";

    /// <summary>Nombre de comercio registrado en consumos por avance de efectivo.</summary>
    public const string CashAdvanceCommerceName = "AVANCE";

    /// <summary>Texto de origen para el crédito inicial al abrir una cuenta (y montos adicionales del admin).</summary>
    public const string OpeningOrigin = "APERTURA";
}
