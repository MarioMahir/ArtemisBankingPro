using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateCommerceUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.UpdateUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.UpdateUserStatus;
using ArtemisBankingPro.Core.Application.Features.Users.Queries.GetUsers;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class UsersValidatorsTests
{
    private static CreateUserCommand CrearClienteValido() => new()
    {
        FirstName = "Ana",
        LastName = "Pérez",
        Identification = "00112345678",
        Email = "ana@test.com",
        UserName = "ana",
        Password = "P@ssw0rd!",
        ConfirmPassword = "P@ssw0rd!",
        Role = Roles.Cliente,
        InitialAmount = 1_000m
    };

    // ---------------- CreateUserCommand ----------------

    [Fact]
    public void CreateUserCommandValidator_ClienteValido_EsValido()
    {
        var validator = new CreateUserCommandValidator();

        Assert.True(validator.Validate(CrearClienteValido()).IsValid);
    }

    [Fact]
    public void CreateUserCommandValidator_CorreoConFormatoInvalido_EsInvalido()
    {
        var validator = new CreateUserCommandValidator();
        var command = CrearClienteValido();
        command.Email = "no-es-un-correo";

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Fact]
    public void CreateUserCommandValidator_ContrasenasNoCoinciden_EsInvalido()
    {
        var validator = new CreateUserCommandValidator();
        var command = CrearClienteValido();
        command.ConfirmPassword = "Otra";

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.ContrasenasNoCoinciden);
    }

    [Fact]
    public void CreateUserCommandValidator_RolComercio_NoEstaPermitido()
    {
        var validator = new CreateUserCommandValidator();
        var command = CrearClienteValido();
        command.Role = Roles.Comercio;
        command.InitialAmount = null;

        Assert.False(validator.Validate(command).IsValid);
    }

    [Fact]
    public void CreateUserCommandValidator_MontoInicialNegativo_EsInvalido()
    {
        var validator = new CreateUserCommandValidator();
        var command = CrearClienteValido();
        command.InitialAmount = -100m;

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.MontoInicialNegativo);
    }

    [Fact]
    public void CreateUserCommandValidator_MontoInicialEnRolNoCliente_EsInvalido()
    {
        var validator = new CreateUserCommandValidator();
        var command = CrearClienteValido();
        command.Role = Roles.Cajero;
        command.InitialAmount = 500m; // solo aplica a Cliente

        Assert.False(validator.Validate(command).IsValid);
    }

    [Fact]
    public void CreateUserCommandValidator_CajeroSinMontoInicial_EsValido()
    {
        var validator = new CreateUserCommandValidator();
        var command = CrearClienteValido();
        command.Role = Roles.Cajero;
        command.InitialAmount = null;

        Assert.True(validator.Validate(command).IsValid);
    }

    // ---------------- CreateCommerceUserCommand ----------------

    private static CreateCommerceUserCommand ComercioValido() => new()
    {
        CommerceId = 1,
        FirstName = "Comercio",
        LastName = "Central",
        Identification = "00100000001",
        Email = "comercio@test.com",
        UserName = "comercio1",
        Password = "P@ssw0rd!",
        ConfirmPassword = "P@ssw0rd!",
        InitialAmount = 0m
    };

    [Fact]
    public void CreateCommerceUserCommandValidator_Valido_EsValido()
    {
        var validator = new CreateCommerceUserCommandValidator();

        Assert.True(validator.Validate(ComercioValido()).IsValid);
    }

    [Fact]
    public void CreateCommerceUserCommandValidator_SinMontoInicial_EsInvalido()
    {
        // El monto inicial es REQUERIDO para usuarios de comercio.
        var validator = new CreateCommerceUserCommandValidator();
        var command = ComercioValido();
        command.InitialAmount = null;

        Assert.False(validator.Validate(command).IsValid);
    }

    [Fact]
    public void CreateCommerceUserCommandValidator_CommerceIdCero_EsInvalido()
    {
        var validator = new CreateCommerceUserCommandValidator();
        var command = ComercioValido();
        command.CommerceId = 0;

        Assert.False(validator.Validate(command).IsValid);
    }

    // ---------------- UpdateUserCommand ----------------

    private static UpdateUserCommand ActualizarValido() => new()
    {
        Id = "user-1",
        FirstName = "Ana",
        LastName = "Pérez",
        Identification = "00112345678",
        Email = "ana@test.com",
        UserName = "ana",
        ActingUserId = "admin-1"
    };

    [Fact]
    public void UpdateUserCommandValidator_SinContrasena_EsValido()
    {
        // La contraseña es opcional al editar.
        var validator = new UpdateUserCommandValidator();

        Assert.True(validator.Validate(ActualizarValido()).IsValid);
    }

    [Fact]
    public void UpdateUserCommandValidator_ContrasenaSinConfirmacion_EsInvalido()
    {
        var validator = new UpdateUserCommandValidator();
        var command = ActualizarValido();
        command.Password = "Nueva123!";
        command.ConfirmPassword = "Distinta";

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.ContrasenasNoCoinciden);
    }

    [Fact]
    public void UpdateUserCommandValidator_MontoAdicionalNegativo_EsInvalido()
    {
        var validator = new UpdateUserCommandValidator();
        var command = ActualizarValido();
        command.AdditionalAmount = -5m;

        Assert.False(validator.Validate(command).IsValid);
    }

    // ---------------- UpdateUserStatusCommand ----------------

    [Fact]
    public void UpdateUserStatusCommandValidator_SinUserId_EsInvalido()
    {
        var validator = new UpdateUserStatusCommandValidator();

        Assert.False(validator.Validate(new UpdateUserStatusCommand
        {
            UserId = "", IsActive = true, ActingUserId = "admin-1"
        }).IsValid);
    }

    // ---------------- GetUsersQuery ----------------

    [Fact]
    public void GetUsersQueryValidator_RolComercioComoFiltro_EsInvalido()
    {
        // El listado de usuarios web EXCLUYE el rol Comercio.
        var validator = new GetUsersQueryValidator();

        Assert.False(validator.Validate(new GetUsersQuery { Page = 1, Role = Roles.Comercio }).IsValid);
    }

    [Fact]
    public void GetUsersQueryValidator_PaginaCero_EsInvalido()
    {
        var validator = new GetUsersQueryValidator();

        Assert.False(validator.Validate(new GetUsersQuery { Page = 0 }).IsValid);
    }

    [Fact]
    public void GetUsersQueryValidator_FiltroClienteYPagina1_EsValido()
    {
        var validator = new GetUsersQueryValidator();

        Assert.True(validator.Validate(new GetUsersQuery { Page = 1, Role = Roles.Cliente }).IsValid);
    }
}
