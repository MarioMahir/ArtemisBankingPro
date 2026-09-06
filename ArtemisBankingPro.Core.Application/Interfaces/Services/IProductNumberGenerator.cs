namespace ArtemisBankingPro.Core.Application.Interfaces.Services;

public interface IProductNumberGenerator
{
    /// <summary>9 dígitos únicos en el espacio COMPARTIDO de cuentas de ahorro y préstamos.</summary>
    Task<string> GenerateAccountOrLoanNumberAsync();

    /// <summary>16 dígitos únicos entre tarjetas de crédito.</summary>
    Task<string> GenerateCardNumberAsync();

    /// <summary>CVC aleatorio de 3 dígitos. Solo debe persistirse su hash.</summary>
    string GenerateCvc();
}
