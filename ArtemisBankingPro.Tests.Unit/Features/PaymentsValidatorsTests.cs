using ArtemisBankingPro.Core.Application.Features.Payments.Commands.ProcessPayment;
using ArtemisBankingPro.Core.Application.Features.Payments.Queries.GetCommerceTransactions;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class PaymentsValidatorsTests
{
    private static ProcessPaymentCommand PagoValido() => new()
    {
        CommerceId = 1,
        CardNumber = "5425123456781234",
        MonthExpirationCard = "07",
        YearExpirationCard = "2028",
        Cvc = "123",
        TransactionAmount = 250m
    };

    [Fact]
    public void ProcessPaymentCommandValidator_DatosValidos_EsValido()
    {
        var validator = new ProcessPaymentCommandValidator();

        Assert.True(validator.Validate(PagoValido()).IsValid);
    }

    [Theory]
    [InlineData("542512345678123")]    // 15 dígitos
    [InlineData("54251234567812345")]  // 17 dígitos
    [InlineData("5425-1234-5678-1234")]
    [InlineData("")]
    public void ProcessPaymentCommandValidator_TarjetaQueNoTiene16Digitos_EsInvalida(string numero)
    {
        var validator = new ProcessPaymentCommandValidator();
        var command = PagoValido();
        command.CardNumber = numero;

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ProcessPaymentCommand.CardNumber));
    }

    [Theory]
    [InlineData("13")]  // mes 13 no existe
    [InlineData("00")]
    [InlineData("7")]   // sin cero inicial
    [InlineData("ab")]
    public void ProcessPaymentCommandValidator_MesInvalido_EsInvalido(string mes)
    {
        var validator = new ProcessPaymentCommandValidator();
        var command = PagoValido();
        command.MonthExpirationCard = mes;

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ProcessPaymentCommand.MonthExpirationCard));
    }

    [Theory]
    [InlineData("01")]
    [InlineData("09")]
    [InlineData("10")]
    [InlineData("12")]
    public void ProcessPaymentCommandValidator_MesesValidos_SonAceptados(string mes)
    {
        var validator = new ProcessPaymentCommandValidator();
        var command = PagoValido();
        command.MonthExpirationCard = mes;

        Assert.True(validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("28")]    // 2 dígitos: debe ser YYYY
    [InlineData("202X")]
    [InlineData("")]
    public void ProcessPaymentCommandValidator_AnioInvalido_EsInvalido(string anio)
    {
        var validator = new ProcessPaymentCommandValidator();
        var command = PagoValido();
        command.YearExpirationCard = anio;

        Assert.False(validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("12")]    // 2 dígitos
    [InlineData("1234")]  // 4 dígitos
    [InlineData("ab1")]
    [InlineData("")]
    public void ProcessPaymentCommandValidator_CvcQueNoTiene3Digitos_EsInvalido(string cvc)
    {
        var validator = new ProcessPaymentCommandValidator();
        var command = PagoValido();
        command.Cvc = cvc;

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ProcessPaymentCommand.Cvc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ProcessPaymentCommandValidator_MontoMenorOIgualACero_EsInvalido(decimal monto)
    {
        var validator = new ProcessPaymentCommandValidator();
        var command = PagoValido();
        command.TransactionAmount = monto;

        Assert.False(validator.Validate(command).IsValid);
    }

    [Fact]
    public void ProcessPaymentCommandValidator_CommerceIdCero_EsInvalido()
    {
        var validator = new ProcessPaymentCommandValidator();
        var command = PagoValido();
        command.CommerceId = 0;

        Assert.False(validator.Validate(command).IsValid);
    }

    // ---------------- GetCommerceTransactionsQuery ----------------

    [Fact]
    public void GetCommerceTransactionsQueryValidator_DatosValidos_EsValido()
    {
        var validator = new GetCommerceTransactionsQueryValidator();

        Assert.True(validator.Validate(new GetCommerceTransactionsQuery { CommerceId = 1, Page = 1 }).IsValid);
    }

    [Fact]
    public void GetCommerceTransactionsQueryValidator_PaginaCero_EsInvalido()
    {
        var validator = new GetCommerceTransactionsQueryValidator();

        Assert.False(validator.Validate(new GetCommerceTransactionsQuery { CommerceId = 1, Page = 0 }).IsValid);
    }
}
