using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Loans;
using ArtemisBankingPro.Core.Application.Features.Loans.Commands.CreateLoan;
using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Mappings;
using ArtemisBankingPro.Core.Domain.Constants;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class CreateLoanCommandHandlerTests
{
    private readonly Mock<ILoanService> _loanService = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<WebApiMappingProfile>()).CreateMapper();

    private CreateLoanCommandHandler CreateHandler() => new(_loanService.Object, _mapper);

    private static CreateLoanCommand Comando(bool confirmar = false) => new()
    {
        ClientId = "cli-1",
        CapitalAmount = 100_000m,
        TermInMonths = 12,
        AnnualInterestRate = 12m,
        ConfirmHighRisk = confirmar,
        ActingUserId = "admin-1"
    };

    private void RiesgoDevuelve(RiskLevel nivel) =>
        _loanService.Setup(s => s.EvaluateRiskAsync(It.IsAny<CreateLoanRequest>()))
            .ReturnsAsync(ServiceResult<RiskEvaluationDto>.Ok(new RiskEvaluationDto
            {
                RiskLevel = nivel,
                WarningMessage = nivel switch
                {
                    RiskLevel.CurrentHighRisk => Mensajes.ClienteAltoRiesgoActual,
                    RiskLevel.ProjectedHighRisk => Mensajes.ClienteAltoRiesgoProyectado,
                    _ => null
                },
                CurrentDebt = 60_000m,
                ProjectedDebt = 170_000m,
                AverageDebt = 50_000m
            }));

    [Fact]
    public async Task Handle_AltoRiesgoSinConfirmar_DevuelveConflictoSinCrearElPrestamo()
    {
        // Arrange
        RiesgoDevuelve(RiskLevel.CurrentHighRisk);
        var handler = CreateHandler();

        // Act
        var resultado = await handler.Handle(Comando(confirmar: false), CancellationToken.None);

        // Assert: conflicto 409 con riskType y las deudas; NO se llama CreateLoanAsync.
        Assert.NotNull(resultado.Conflict);
        Assert.Null(resultado.Result);
        Assert.Equal("CurrentHighRisk", resultado.Conflict!.RiskType);
        Assert.Equal(Mensajes.ClienteAltoRiesgoActual, resultado.Conflict.Message);
        Assert.Equal(60_000m, resultado.Conflict.CurrentDebt);
        Assert.Equal(170_000m, resultado.Conflict.ProjectedDebt);
        Assert.Equal(50_000m, resultado.Conflict.AverageDebt);
        _loanService.Verify(s => s.CreateLoanAsync(It.IsAny<CreateLoanRequest>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AltoRiesgoConConfirmHighRisk_CreaElPrestamo()
    {
        // Arrange
        RiesgoDevuelve(RiskLevel.ProjectedHighRisk);
        _loanService.Setup(s => s.CreateLoanAsync(It.IsAny<CreateLoanRequest>(), "admin-1"))
            .ReturnsAsync(ServiceResult<LoanDto>.Ok(new LoanDto { Id = 1, LoanNumber = "900000001", UserId = "cli-1" }));
        var handler = CreateHandler();

        // Act
        var resultado = await handler.Handle(Comando(confirmar: true), CancellationToken.None);

        // Assert
        Assert.Null(resultado.Conflict);
        Assert.NotNull(resultado.Result);
        Assert.True(resultado.Result!.Succeeded);
        Assert.Equal("900000001", resultado.Result.Data!.LoanNumber);
        _loanService.Verify(s => s.CreateLoanAsync(
            It.Is<CreateLoanRequest>(r => r.ClientId == "cli-1" && r.CapitalAmount == 100_000m
                && r.TermInMonths == 12 && r.AnnualInterestRate == 12m),
            "admin-1"), Times.Once);
    }

    [Fact]
    public async Task Handle_SinRiesgo_CreaDirectamenteSinNecesidadDeConfirmacion()
    {
        // Arrange
        RiesgoDevuelve(RiskLevel.None);
        _loanService.Setup(s => s.CreateLoanAsync(It.IsAny<CreateLoanRequest>(), "admin-1"))
            .ReturnsAsync(ServiceResult<LoanDto>.Ok(new LoanDto { Id = 1, LoanNumber = "900000001", UserId = "cli-1" }));
        var handler = CreateHandler();

        // Act
        var resultado = await handler.Handle(Comando(confirmar: false), CancellationToken.None);

        // Assert
        Assert.Null(resultado.Conflict);
        Assert.True(resultado.Result!.Succeeded);
    }

    [Fact]
    public async Task Handle_ReglaDeNegocioFallida_PropagaElErrorSinConflicto()
    {
        // Arrange: p. ej. el cliente ya tiene un préstamo activo.
        _loanService.Setup(s => s.EvaluateRiskAsync(It.IsAny<CreateLoanRequest>()))
            .ReturnsAsync(ServiceResult<RiskEvaluationDto>.Fail(Mensajes.ClienteConPrestamoActivo));
        var handler = CreateHandler();

        // Act
        var resultado = await handler.Handle(Comando(), CancellationToken.None);

        // Assert
        Assert.Null(resultado.Conflict);
        Assert.False(resultado.Result!.Succeeded);
        Assert.Equal(Mensajes.ClienteConPrestamoActivo, resultado.Result.Message);
        _loanService.Verify(s => s.CreateLoanAsync(It.IsAny<CreateLoanRequest>(), It.IsAny<string>()), Times.Never);
    }
}
