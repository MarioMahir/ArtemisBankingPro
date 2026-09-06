using ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.CancelCreditCard;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.CreateCreditCard;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.UpdateCardLimit;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Queries.GetCreditCardById;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Queries.GetCreditCards;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class CreditCardsValidatorsTests
{
    // ---------------- CreateCreditCardCommand ----------------

    [Fact]
    public void CreateCreditCardCommandValidator_DatosValidos_EsValido()
    {
        var validator = new CreateCreditCardCommandValidator();

        Assert.True(validator.Validate(new CreateCreditCardCommand
        {
            ClientId = "cli-1", CreditLimit = 10_000m, ActingUserId = "admin-1"
        }).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void CreateCreditCardCommandValidator_LimiteMenorOIgualACero_EsInvalido(decimal limite)
    {
        var validator = new CreateCreditCardCommandValidator();

        var resultado = validator.Validate(new CreateCreditCardCommand
        {
            ClientId = "cli-1", CreditLimit = limite, ActingUserId = "admin-1"
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.LimiteInvalido);
    }

    [Fact]
    public void CreateCreditCardCommandValidator_SinCliente_EsInvalido()
    {
        var validator = new CreateCreditCardCommandValidator();

        Assert.False(validator.Validate(new CreateCreditCardCommand
        {
            ClientId = "", CreditLimit = 1_000m, ActingUserId = "admin-1"
        }).IsValid);
    }

    // ---------------- UpdateCardLimitCommand ----------------

    [Fact]
    public void UpdateCardLimitCommandValidator_LimiteCero_EsInvalido()
    {
        var validator = new UpdateCardLimitCommandValidator();

        var resultado = validator.Validate(new UpdateCardLimitCommand { CardId = 1, NewLimit = 0m });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.LimiteTarjetaInvalido);
    }

    [Fact]
    public void UpdateCardLimitCommandValidator_DatosValidos_EsValido()
    {
        var validator = new UpdateCardLimitCommandValidator();

        Assert.True(validator.Validate(new UpdateCardLimitCommand { CardId = 1, NewLimit = 5_000m }).IsValid);
    }

    // ---------------- CancelCreditCardCommand ----------------

    [Fact]
    public void CancelCreditCardCommandValidator_IdCero_EsInvalido()
    {
        var validator = new CancelCreditCardCommandValidator();

        Assert.False(validator.Validate(new CancelCreditCardCommand { CardId = 0 }).IsValid);
    }

    // ---------------- GetCreditCardsQuery / GetCreditCardByIdQuery ----------------

    [Theory]
    [InlineData("activas")]
    [InlineData("canceladas")]
    [InlineData("todas")]
    [InlineData(null)]
    public void GetCreditCardsQueryValidator_EstadosPermitidos_SonValidos(string? estado)
    {
        var validator = new GetCreditCardsQueryValidator();

        Assert.True(validator.Validate(new GetCreditCardsQuery { Page = 1, Status = estado }).IsValid);
    }

    [Fact]
    public void GetCreditCardsQueryValidator_EstadoDesconocido_EsInvalido()
    {
        var validator = new GetCreditCardsQueryValidator();

        Assert.False(validator.Validate(new GetCreditCardsQuery { Page = 1, Status = "vencidas" }).IsValid);
    }

    [Fact]
    public void GetCreditCardByIdQueryValidator_IdNegativo_EsInvalido()
    {
        var validator = new GetCreditCardByIdQueryValidator();

        Assert.False(validator.Validate(new GetCreditCardByIdQuery { Id = -1 }).IsValid);
    }
}
