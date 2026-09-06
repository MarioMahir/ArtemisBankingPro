using ArtemisBankingPro.Core.Application.Behaviors;
using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Features.Account.Commands.Login;
using ArtemisBankingPro.Core.Application.Features.Loans.Commands.CreateLoan;
using ArtemisBankingPro.Core.Domain.Constants;
using FluentValidation;
using ValidationException = ArtemisBankingPro.Core.Application.Exceptions.ValidationException;

namespace ArtemisBankingPro.Tests.Unit.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_RequestInvalido_LanzaLaValidationExceptionPropiaConErroresPorCampo()
    {
        // Arrange: login sin usuario ni contraseña.
        var behavior = new ValidationBehavior<LoginCommand, ServiceResult<LoginResponse>>(
            [new LoginCommandValidator()]);
        var request = new LoginCommand { UserName = "", Password = "" };
        var siguienteEjecutado = false;

        // Act + Assert
        var excepcion = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(request, _ =>
            {
                siguienteEjecutado = true;
                return Task.FromResult(ServiceResult<LoginResponse>.Ok(new LoginResponse { Jwt = "x" }));
            }, CancellationToken.None));

        // El handler NUNCA se ejecuta y los errores vienen agrupados por propiedad.
        Assert.False(siguienteEjecutado);
        Assert.Equal(2, excepcion.Errors.Count);
        Assert.True(excepcion.Errors.ContainsKey(nameof(LoginCommand.UserName)));
        Assert.True(excepcion.Errors.ContainsKey(nameof(LoginCommand.Password)));
        Assert.Contains("El nombre de usuario es requerido.", excepcion.Errors[nameof(LoginCommand.UserName)]);
    }

    [Fact]
    public async Task Handle_RequestValido_PasaAlSiguienteDelPipeline()
    {
        // Arrange
        var behavior = new ValidationBehavior<LoginCommand, ServiceResult<LoginResponse>>(
            [new LoginCommandValidator()]);
        var request = new LoginCommand { UserName = "admin", Password = "P@ss1" };

        // Act
        var resultado = await behavior.Handle(request,
            _ => Task.FromResult(ServiceResult<LoginResponse>.Ok(new LoginResponse { Jwt = "jwt-ok" })),
            CancellationToken.None);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal("jwt-ok", resultado.Data!.Jwt);
    }

    [Fact]
    public async Task Handle_SinValidadoresRegistrados_PasaDirecto()
    {
        // Arrange: colección vacía de validadores.
        var behavior = new ValidationBehavior<LoginCommand, ServiceResult>([]);

        // Act
        var resultado = await behavior.Handle(
            new LoginCommand { UserName = "", Password = "" },
            _ => Task.FromResult(ServiceResult.Ok()),
            CancellationToken.None);

        // Assert
        Assert.True(resultado.Succeeded);
    }

    [Fact]
    public async Task Handle_VariosErroresEnLaMismaPropiedad_SeAgrupanSinDuplicados()
    {
        // Arrange: préstamo con varios campos inválidos.
        var behavior = new ValidationBehavior<CreateLoanCommand, CreateLoanResult>(
            [new CreateLoanCommandValidator()]);
        var request = new CreateLoanCommand
        {
            ClientId = "", CapitalAmount = 0m, TermInMonths = 7,
            AnnualInterestRate = -1m, ActingUserId = "admin-1"
        };

        // Act
        var excepcion = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(request, _ => Task.FromResult(new CreateLoanResult()), CancellationToken.None));

        // Assert: un error por cada campo inválido, con los mensajes del spec.
        Assert.Equal(4, excepcion.Errors.Count);
        Assert.Contains(Mensajes.PlazoInvalido, excepcion.Errors[nameof(CreateLoanCommand.TermInMonths)]);
        Assert.Contains(Mensajes.MontoPrestamoInvalido, excepcion.Errors[nameof(CreateLoanCommand.CapitalAmount)]);
        Assert.Contains(Mensajes.TasaNegativa, excepcion.Errors[nameof(CreateLoanCommand.AnnualInterestRate)]);
    }

    [Fact]
    public async Task Handle_ExcepcionLanzada_TieneElMensajeGeneralEnEspanol()
    {
        // Arrange
        var behavior = new ValidationBehavior<LoginCommand, ServiceResult>(
            [new LoginCommandValidator()]);

        // Act
        var excepcion = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new LoginCommand { UserName = "", Password = "x" },
                _ => Task.FromResult(ServiceResult.Ok()), CancellationToken.None));

        // Assert
        Assert.Equal("Se produjeron uno o más errores de validación.", excepcion.Message);
    }
}
