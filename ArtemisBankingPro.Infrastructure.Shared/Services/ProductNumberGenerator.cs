using System.Security.Cryptography;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.Infrastructure.Shared.Services;

public class ProductNumberGenerator(IProductNumberRepository repository) : IProductNumberGenerator
{
    public async Task<string> GenerateAccountOrLoanNumberAsync()
    {
        // El espacio de 9 dígitos es COMPARTIDO entre cuentas de ahorro y préstamos.
        string number;
        do
        {
            number = RandomDigits(AppConstants.AccountNumberLength);
        } while (await repository.AccountOrLoanNumberExistsAsync(number));

        return number;
    }

    public async Task<string> GenerateCardNumberAsync()
    {
        string number;
        do
        {
            number = RandomDigits(AppConstants.CardNumberLength);
        } while (await repository.CardNumberExistsAsync(number));

        return number;
    }

    public string GenerateCvc() => RandomDigits(AppConstants.CvcLength);

    /// <summary>Dígitos criptográficamente aleatorios; puede incluir ceros iniciales (por eso se almacena como texto).</summary>
    private static string RandomDigits(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        return new string(chars);
    }
}
