using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Commerces;
using ArtemisBankingPro.Core.Application.Dtos.CreditCards;
using ArtemisBankingPro.Core.Application.Dtos.SavingsAccounts;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Features.Account.Commands.Login;
using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.CreateCommerce;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.CancelCreditCard;
using ArtemisBankingPro.Core.Application.Features.CreditCards.Commands.CreateCreditCard;
using ArtemisBankingPro.Core.Application.Features.Payments.Commands.ProcessPayment;
using ArtemisBankingPro.Core.Application.Features.SavingsAccounts.Commands.CancelSavingsAccount;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateCommerceUser;
using ArtemisBankingPro.Core.Application.Features.Users.Commands.CreateUser;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Mappings;
using ArtemisBankingPro.Core.Domain.Constants;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class HandlersTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<WebApiMappingProfile>()).CreateMapper();

    // ---------------- LoginCommand → JWT ----------------

    [Fact]
    public async Task LoginCommandHandler_CredencialesValidas_DevuelveElJwt()
    {
        // Arrange
        var accountService = new Mock<IAccountService>();
        var jwtService = new Mock<IJwtService>();
        var usuario = new UserDto
        {
            Id = "adm-1", FirstName = "Admin", LastName = "Root", Identification = "001",
            Email = "admin@test.com", UserName = "admin", Role = Roles.Administrador, IsActive = true
        };
        accountService.Setup(a => a.AuthenticateAsync("admin", "P@ss1", Roles.ApiRoles))
            .ReturnsAsync(ServiceResult<UserDto>.Ok(usuario));
        jwtService.Setup(j => j.GenerateToken(usuario)).Returns("jwt-token-firmado");
        var handler = new LoginCommandHandler(accountService.Object, jwtService.Object);

        // Act
        var resultado = await handler.Handle(
            new LoginCommand { UserName = "admin", Password = "P@ss1" }, CancellationToken.None);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal("jwt-token-firmado", resultado.Data!.Jwt);
    }

    [Fact]
    public async Task LoginCommandHandler_CredencialesInvalidas_FallaSinJwt()
    {
        // Arrange
        var accountService = new Mock<IAccountService>();
        var jwtService = new Mock<IJwtService>();
        accountService.Setup(a => a.AuthenticateAsync("admin", "mala", Roles.ApiRoles))
            .ReturnsAsync(ServiceResult<UserDto>.Fail(Mensajes.CredencialesInvalidas));
        var handler = new LoginCommandHandler(accountService.Object, jwtService.Object);

        // Act
        var resultado = await handler.Handle(
            new LoginCommand { UserName = "admin", Password = "mala" }, CancellationToken.None);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.CredencialesInvalidas, resultado.Message);
        jwtService.Verify(j => j.GenerateToken(It.IsAny<UserDto>()), Times.Never);
    }

    [Fact]
    public async Task LoginCommandHandler_CuentaInactiva_UsaElMensajeDeLaApi()
    {
        // Arrange
        var accountService = new Mock<IAccountService>();
        accountService.Setup(a => a.AuthenticateAsync("admin", "P@ss1", Roles.ApiRoles))
            .ReturnsAsync(ServiceResult<UserDto>.Fail(Mensajes.CuentaInactiva));
        var handler = new LoginCommandHandler(accountService.Object, new Mock<IJwtService>().Object);

        // Act
        var resultado = await handler.Handle(
            new LoginCommand { UserName = "admin", Password = "P@ss1" }, CancellationToken.None);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ApiCuentaInactiva, resultado.Message);
    }

    // ---------------- ProcessPaymentCommand → servicio Hermes Pay ----------------

    [Fact]
    public async Task ProcessPaymentCommandHandler_MapeaTodosLosCamposAlServicio()
    {
        // Arrange
        var hermesPay = new Mock<IHermesPayService>();
        hermesPay.Setup(h => h.ProcessPaymentAsync(5, "5425123456781234", "07", "2028", "123", 250m))
            .ReturnsAsync(ServiceResult.Ok());
        var handler = new ProcessPaymentCommandHandler(hermesPay.Object);

        // Act
        var resultado = await handler.Handle(new ProcessPaymentCommand
        {
            CommerceId = 5,
            CardNumber = "5425123456781234",
            MonthExpirationCard = "07",
            YearExpirationCard = "2028",
            Cvc = "123",
            TransactionAmount = 250m
        }, CancellationToken.None);

        // Assert
        Assert.True(resultado.Succeeded);
        hermesPay.Verify(h => h.ProcessPaymentAsync(5, "5425123456781234", "07", "2028", "123", 250m), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentCommandHandler_RechazoDelServicio_PropagaElFallo()
    {
        // Arrange
        var hermesPay = new Mock<IHermesPayService>();
        hermesPay.Setup(h => h.ProcessPaymentAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(ServiceResult.Fail(Mensajes.ApiExcedeCreditoDisponible));
        var handler = new ProcessPaymentCommandHandler(hermesPay.Object);

        // Act
        var resultado = await handler.Handle(new ProcessPaymentCommand
        {
            CommerceId = 5, CardNumber = "5425123456781234", MonthExpirationCard = "07",
            YearExpirationCard = "2028", Cvc = "123", TransactionAmount = 999_999m
        }, CancellationToken.None);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ApiExcedeCreditoDisponible, resultado.Message);
    }

    // ---------------- CreateUserCommand → UserService (TokenInBody = true) ----------------

    [Fact]
    public async Task CreateUserCommandHandler_MapeaElComandoConTokenEnElCuerpo()
    {
        // Arrange
        var userService = new Mock<IUserService>();
        CreateUserRequest? capturado = null;
        userService.Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .Callback<CreateUserRequest>(r => capturado = r)
            .ReturnsAsync(ServiceResult<UserDto>.Ok(new UserDto
            {
                Id = "u1", FirstName = "Ana", LastName = "P", Identification = "1",
                Email = "a@a.com", UserName = "ana", Role = Roles.Cliente
            }));
        var handler = new CreateUserCommandHandler(userService.Object, Mapper);

        // Act
        await handler.Handle(new CreateUserCommand
        {
            FirstName = "Ana", LastName = "P", Identification = "1", Email = "a@a.com",
            UserName = "ana", Password = "P@ss1", ConfirmPassword = "P@ss1",
            Role = Roles.Cliente, InitialAmount = 500m
        }, CancellationToken.None);

        // Assert: la API siempre manda el token EN EL CUERPO del correo.
        Assert.NotNull(capturado);
        Assert.True(capturado!.TokenInBody);
        Assert.Equal(Roles.Cliente, capturado.Role);
        Assert.Equal(500m, capturado.InitialAmount);
    }

    // ---------------- CreateCommerceUserCommand ----------------

    [Fact]
    public async Task CreateCommerceUserCommandHandler_ComercioConUsuario_DevuelveConflicto()
    {
        // Arrange: el comercio existe pero YA tiene un usuario (máximo 1).
        var userService = new Mock<IUserService>();
        var accountService = new Mock<IAccountService>();
        var commerceService = new Mock<ICommerceService>();
        commerceService.Setup(c => c.GetByIdAsync(5))
            .ReturnsAsync(ServiceResult<CommerceDto>.Ok(new CommerceDto { Id = 5, Name = "X", Email = "x@x.com", PhoneNumber = "809", Rnc = "1" }));
        accountService.Setup(a => a.GetByCommerceIdAsync(5)).ReturnsAsync(new UserDto
        {
            Id = "existente", FirstName = "Y", LastName = "Z", Identification = "2",
            Email = "y@y.com", UserName = "y", Role = Roles.Comercio, CommerceId = 5
        });
        var handler = new CreateCommerceUserCommandHandler(
            userService.Object, accountService.Object, commerceService.Object, Mapper);

        // Act
        var resultado = await handler.Handle(new CreateCommerceUserCommand
        {
            CommerceId = 5, FirstName = "A", LastName = "B", Identification = "3",
            Email = "n@n.com", UserName = "n", Password = "P@ss1", ConfirmPassword = "P@ss1",
            InitialAmount = 0m
        }, CancellationToken.None);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(CreateCommerceUserCommand.ComercioYaTieneUsuario, resultado.Message);
        userService.Verify(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateCommerceUserCommandHandler_ComercioSinUsuario_CreaConRolComercio()
    {
        // Arrange
        var userService = new Mock<IUserService>();
        var accountService = new Mock<IAccountService>();
        var commerceService = new Mock<ICommerceService>();
        commerceService.Setup(c => c.GetByIdAsync(5))
            .ReturnsAsync(ServiceResult<CommerceDto>.Ok(new CommerceDto { Id = 5, Name = "X", Email = "x@x.com", PhoneNumber = "809", Rnc = "1" }));
        accountService.Setup(a => a.GetByCommerceIdAsync(5)).ReturnsAsync((UserDto?)null);
        CreateUserRequest? capturado = null;
        userService.Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .Callback<CreateUserRequest>(r => capturado = r)
            .ReturnsAsync(ServiceResult<UserDto>.Ok(new UserDto
            {
                Id = "nuevo", FirstName = "A", LastName = "B", Identification = "3",
                Email = "n@n.com", UserName = "n", Role = Roles.Comercio
            }));
        var handler = new CreateCommerceUserCommandHandler(
            userService.Object, accountService.Object, commerceService.Object, Mapper);

        // Act
        var resultado = await handler.Handle(new CreateCommerceUserCommand
        {
            CommerceId = 5, FirstName = "A", LastName = "B", Identification = "3",
            Email = "n@n.com", UserName = "n", Password = "P@ss1", ConfirmPassword = "P@ss1",
            InitialAmount = 1_000m
        }, CancellationToken.None);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.NotNull(capturado);
        Assert.Equal(Roles.Comercio, capturado!.Role); // el rol lo fija el mapeo, no el cliente
        Assert.True(capturado.TokenInBody);
        Assert.Equal(1_000m, capturado.InitialAmount);
    }

    // ---------------- Tarjetas y cuentas: delegación directa ----------------

    [Fact]
    public async Task CancelCreditCardCommandHandler_DelegaEnElServicio()
    {
        // Arrange
        var cardService = new Mock<ICreditCardService>();
        cardService.Setup(s => s.CancelAsync(7))
            .ReturnsAsync(ServiceResult.Fail(Mensajes.TarjetaConDeudaNoCancelable));
        var handler = new CancelCreditCardCommandHandler(cardService.Object);

        // Act
        var resultado = await handler.Handle(new CancelCreditCardCommand { CardId = 7 }, CancellationToken.None);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.TarjetaConDeudaNoCancelable, resultado.Message);
    }

    [Fact]
    public async Task CreateCreditCardCommandHandler_PasaElAdminAutenticadoAlServicio()
    {
        // Arrange
        var cardService = new Mock<ICreditCardService>();
        cardService.Setup(s => s.AssignAsync("cli-1", 5_000m, "admin-1"))
            .ReturnsAsync(ServiceResult<CreditCardDto>.Ok(new CreditCardDto
            {
                Id = 1, MaskedCardNumber = "************1234", LastFourDigits = "1234",
                UserId = "cli-1", ExpirationDisplay = "08/29"
            }));
        var handler = new CreateCreditCardCommandHandler(cardService.Object);

        // Act
        var resultado = await handler.Handle(new CreateCreditCardCommand
        {
            ClientId = "cli-1", CreditLimit = 5_000m, ActingUserId = "admin-1"
        }, CancellationToken.None);

        // Assert
        Assert.True(resultado.Succeeded);
        cardService.Verify(s => s.AssignAsync("cli-1", 5_000m, "admin-1"), Times.Once);
    }

    [Fact]
    public async Task CancelSavingsAccountCommandHandler_DelegaEnElServicio()
    {
        // Arrange
        var accountService = new Mock<ISavingsAccountService>();
        accountService.Setup(s => s.CancelAsync("123456789"))
            .ReturnsAsync(ServiceResult.Fail(Mensajes.PrincipalNoCancelable));
        var handler = new CancelSavingsAccountCommandHandler(accountService.Object);

        // Act
        var resultado = await handler.Handle(
            new CancelSavingsAccountCommand { AccountNumber = "123456789" }, CancellationToken.None);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.PrincipalNoCancelable, resultado.Message);
    }

    // ---------------- CreateCommerceCommand ----------------

    [Fact]
    public async Task CreateCommerceCommandHandler_MapeaElComandoAlServicio()
    {
        // Arrange
        var commerceService = new Mock<ICommerceService>();
        SaveCommerceRequest? capturado = null;
        commerceService.Setup(s => s.CreateAsync(It.IsAny<SaveCommerceRequest>()))
            .Callback<SaveCommerceRequest>(r => capturado = r)
            .ReturnsAsync(ServiceResult<CommerceDto>.Ok(new CommerceDto
            {
                Id = 1, Name = "Colmado", Email = "c@c.com", PhoneNumber = "809", Rnc = "101", IsActive = true
            }));
        var handler = new CreateCommerceCommandHandler(commerceService.Object, Mapper);

        // Act
        var resultado = await handler.Handle(new CreateCommerceCommand
        {
            Name = "Colmado", Email = "c@c.com", PhoneNumber = "809", Rnc = "101"
        }, CancellationToken.None);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.NotNull(capturado);
        Assert.Equal("101", capturado!.Rnc);
        Assert.Equal("Colmado", capturado.Name);
    }
}
