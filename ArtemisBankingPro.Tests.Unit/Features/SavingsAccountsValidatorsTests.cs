using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Commands.CancelSavingsAccount;
using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount;
using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Queries.GetAccountTransactions;
using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Queries.GetSavingsAccounts;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class SavingsAccountsValidatorsTests
{
    // ---------------- CreateSavingsAccountCommand ----------------

    [Fact]
    public void CreateSavingsAccountCommandValidator_DatosValidos_EsValido()
    {
        var validator = new CreateSavingsAccountCommandValidator();

        Assert.True(validator.Validate(new CreateSavingsAccountCommand
        {
            ClientId = "cli-1", InitialBalance = 0m, ActingUserId = "admin-1"
        }).IsValid);
    }

    [Fact]
    public void CreateSavingsAccountCommandValidator_SinBalanceInicial_EsInvalido()
    {
        // En la API el balance inicial es requerido (puede ser 0, pero no null).
        var validator = new CreateSavingsAccountCommandValidator();

        Assert.False(validator.Validate(new CreateSavingsAccountCommand
        {
            ClientId = "cli-1", InitialBalance = null, ActingUserId = "admin-1"
        }).IsValid);
    }

    [Fact]
    public void CreateSavingsAccountCommandValidator_BalanceNegativo_EsInvalidoConMensajeDelSpec()
    {
        var validator = new CreateSavingsAccountCommandValidator();

        var resultado = validator.Validate(new CreateSavingsAccountCommand
        {
            ClientId = "cli-1", InitialBalance = -1m, ActingUserId = "admin-1"
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.BalanceInicialNegativo);
    }

    // ---------------- CancelSavingsAccountCommand ----------------

    [Fact]
    public void CancelSavingsAccountCommandValidator_Numero9Digitos_EsValido()
    {
        var validator = new CancelSavingsAccountCommandValidator();

        Assert.True(validator.Validate(new CancelSavingsAccountCommand { AccountNumber = "123456789" }).IsValid);
    }

    [Theory]
    [InlineData("12345678")]    // 8 dígitos
    [InlineData("1234567890")]  // 10 dígitos
    [InlineData("12345678A")]   // no numérico
    [InlineData("")]
    public void CancelSavingsAccountCommandValidator_NumeroInvalido_EsInvalido(string numero)
    {
        var validator = new CancelSavingsAccountCommandValidator();

        Assert.False(validator.Validate(new CancelSavingsAccountCommand { AccountNumber = numero }).IsValid);
    }

    // ---------------- GetSavingsAccountsQuery ----------------

    [Theory]
    [InlineData("activa", "principal")]
    [InlineData("cancelada", "secundaria")]
    [InlineData("todas", "todas")]
    [InlineData(null, null)]
    public void GetSavingsAccountsQueryValidator_FiltrosPermitidos_SonValidos(string? estado, string? tipo)
    {
        var validator = new GetSavingsAccountsQueryValidator();

        Assert.True(validator.Validate(new GetSavingsAccountsQuery
        {
            Page = 1, Status = estado, Type = tipo
        }).IsValid);
    }

    [Fact]
    public void GetSavingsAccountsQueryValidator_TipoDesconocido_EsInvalido()
    {
        var validator = new GetSavingsAccountsQueryValidator();

        Assert.False(validator.Validate(new GetSavingsAccountsQuery { Page = 1, Type = "corriente" }).IsValid);
    }

    [Fact]
    public void GetSavingsAccountsQueryValidator_EstadoDesconocido_EsInvalido()
    {
        var validator = new GetSavingsAccountsQueryValidator();

        Assert.False(validator.Validate(new GetSavingsAccountsQuery { Page = 1, Status = "bloqueada" }).IsValid);
    }

    // ---------------- GetAccountTransactionsQuery ----------------

    [Fact]
    public void GetAccountTransactionsQueryValidator_NumeroInvalido_EsInvalido()
    {
        var validator = new GetAccountTransactionsQueryValidator();

        Assert.False(validator.Validate(new GetAccountTransactionsQuery
        {
            AccountNumber = "ABC", Page = 1
        }).IsValid);
    }

    [Fact]
    public void GetAccountTransactionsQueryValidator_DatosValidos_EsValido()
    {
        var validator = new GetAccountTransactionsQueryValidator();

        Assert.True(validator.Validate(new GetAccountTransactionsQuery
        {
            AccountNumber = "123456789", Page = 1
        }).IsValid);
    }
}
