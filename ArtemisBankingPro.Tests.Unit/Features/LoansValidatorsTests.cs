using ArtemisBankingPro.Core.Application.Features.Loans.Commands.CreateLoan;
using ArtemisBankingPro.Core.Application.Features.Loans.Commands.UpdateLoanRate;
using ArtemisBankingPro.Core.Application.Features.Loans.Queries.GetLoanById;
using ArtemisBankingPro.Core.Application.Features.Loans.Queries.GetLoans;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class LoansValidatorsTests
{
    private static CreateLoanCommand PrestamoValido(int plazo = 12) => new()
    {
        ClientId = "cli-1",
        CapitalAmount = 100_000m,
        TermInMonths = plazo,
        AnnualInterestRate = 12m,
        ActingUserId = "admin-1"
    };

    // ---------------- CreateLoanCommand ----------------

    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(36)]
    [InlineData(60)]
    public void CreateLoanCommandValidator_PlazosMultiplosDe6_SonValidos(int plazo)
    {
        var validator = new CreateLoanCommandValidator();

        Assert.True(validator.Validate(PrestamoValido(plazo)).IsValid);
    }

    [Theory]
    [InlineData(7)]    // no múltiplo de 6
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(66)]   // fuera del rango 6-60
    [InlineData(-6)]
    public void CreateLoanCommandValidator_PlazosInvalidos_SonRechazadosConMensajeDelSpec(int plazo)
    {
        var validator = new CreateLoanCommandValidator();

        var resultado = validator.Validate(PrestamoValido(plazo));

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.PlazoInvalido);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void CreateLoanCommandValidator_MontoMenorOIgualACero_EsInvalido(decimal monto)
    {
        var validator = new CreateLoanCommandValidator();
        var command = PrestamoValido();
        command.CapitalAmount = monto;

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.MontoPrestamoInvalido);
    }

    [Fact]
    public void CreateLoanCommandValidator_TasaNegativa_EsInvalida()
    {
        var validator = new CreateLoanCommandValidator();
        var command = PrestamoValido();
        command.AnnualInterestRate = -1m;

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.TasaNegativa);
    }

    [Fact]
    public void CreateLoanCommandValidator_TasaCero_EsValida()
    {
        var validator = new CreateLoanCommandValidator();
        var command = PrestamoValido();
        command.AnnualInterestRate = 0m;

        Assert.True(validator.Validate(command).IsValid);
    }

    [Fact]
    public void CreateLoanCommandValidator_SinCliente_EsInvalido()
    {
        var validator = new CreateLoanCommandValidator();
        var command = PrestamoValido();
        command.ClientId = "";

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.DebeSeleccionarCliente);
    }

    // ---------------- UpdateLoanRateCommand ----------------

    [Fact]
    public void UpdateLoanRateCommandValidator_TasaNegativa_EsInvalida()
    {
        var validator = new UpdateLoanRateCommandValidator();

        var resultado = validator.Validate(new UpdateLoanRateCommand { LoanId = 1, AnnualInterestRate = -0.5m });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.TasaNegativa);
    }

    [Fact]
    public void UpdateLoanRateCommandValidator_IdCero_EsInvalido()
    {
        var validator = new UpdateLoanRateCommandValidator();

        Assert.False(validator.Validate(new UpdateLoanRateCommand { LoanId = 0, AnnualInterestRate = 10m }).IsValid);
    }

    [Fact]
    public void UpdateLoanRateCommandValidator_DatosValidos_EsValido()
    {
        var validator = new UpdateLoanRateCommandValidator();

        Assert.True(validator.Validate(new UpdateLoanRateCommand { LoanId = 1, AnnualInterestRate = 15m }).IsValid);
    }

    // ---------------- GetLoansQuery / GetLoanByIdQuery ----------------

    [Theory]
    [InlineData("activos")]
    [InlineData("completados")]
    [InlineData("todos")]
    [InlineData(null)]
    public void GetLoansQueryValidator_EstadosPermitidos_SonValidos(string? estado)
    {
        var validator = new GetLoansQueryValidator();

        Assert.True(validator.Validate(new GetLoansQuery { Page = 1, Status = estado }).IsValid);
    }

    [Fact]
    public void GetLoansQueryValidator_EstadoDesconocido_EsInvalido()
    {
        var validator = new GetLoansQueryValidator();

        Assert.False(validator.Validate(new GetLoansQuery { Page = 1, Status = "vencidos" }).IsValid);
    }

    [Fact]
    public void GetLoanByIdQueryValidator_IdCero_EsInvalido()
    {
        var validator = new GetLoanByIdQueryValidator();

        Assert.False(validator.Validate(new GetLoanByIdQuery { Id = 0 }).IsValid);
    }
}
