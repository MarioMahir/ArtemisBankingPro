using ArtemisBankingPro.Core.Application.Dtos.Commerces;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Application.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

public class CommerceServiceTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly List<Commerce> _commerces = [];
    private Mock<Core.Application.Interfaces.Repositories.IGenericRepository<Commerce>> _repo = null!;

    private CommerceService CreateService()
    {
        _repo = MockHelpers.Repo(_commerces);
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] keys) => _commerces.FirstOrDefault(c => c.Id == (int)keys[0]));
        return new CommerceService(_repo.Object, _accountService.Object, MockHelpers.UnitOfWork().Object, MockHelpers.Mapper());
    }

    private Commerce Comercio(int id = 1, string rnc = "101000001", string email = "c1@test.com", bool activo = true)
    {
        var commerce = new Commerce
        {
            Id = id, Name = $"Comercio {id}", Email = email, PhoneNumber = "8090000000",
            Rnc = rnc, IsActive = activo, CreatedAt = DateTime.Now
        };
        _commerces.Add(commerce);
        return commerce;
    }

    private static SaveCommerceRequest Solicitud(string rnc = "101000009", string email = "nuevo@test.com") => new()
    {
        Name = "Nuevo Comercio", Email = email, PhoneNumber = "8090000001", Rnc = rnc
    };

    [Fact]
    public async Task CreateAsync_RncDuplicado_Falla()
    {
        // Arrange
        Comercio(rnc: "101000001");
        var service = CreateService();

        // Act
        var resultado = await service.CreateAsync(Solicitud(rnc: "101000001"));

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ComercioRncDuplicado, resultado.Message);
        Assert.Single(_commerces);
    }

    [Fact]
    public async Task CreateAsync_CorreoDuplicado_Falla()
    {
        // Arrange
        Comercio(email: "c1@test.com");
        var service = CreateService();

        // Act
        var resultado = await service.CreateAsync(Solicitud(email: "c1@test.com"));

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ComercioCorreoDuplicado, resultado.Message);
    }

    [Fact]
    public async Task CreateAsync_DatosValidos_NaceActivo()
    {
        // Arrange
        var service = CreateService();

        // Act
        var resultado = await service.CreateAsync(Solicitud());

        // Assert
        Assert.True(resultado.Succeeded);
        var comercio = Assert.Single(_commerces);
        Assert.True(comercio.IsActive); // nace Activo y SIN usuario
    }

    [Fact]
    public async Task UpdateAsync_RncDeOtroComercio_Falla()
    {
        // Arrange
        Comercio(id: 1, rnc: "101000001", email: "c1@test.com");
        Comercio(id: 2, rnc: "101000002", email: "c2@test.com");
        var service = CreateService();

        // Act: intenta ponerle al comercio 2 el RNC del comercio 1.
        var resultado = await service.UpdateAsync(2, Solicitud(rnc: "101000001", email: "c2@test.com"));

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ComercioRncDuplicado, resultado.Message);
    }

    [Fact]
    public async Task UpdateAsync_MismoRncDelPropioComercio_SeAcepta()
    {
        // Arrange
        var comercio = Comercio(id: 1, rnc: "101000001");
        var service = CreateService();

        // Act
        var resultado = await service.UpdateAsync(1, Solicitud(rnc: "101000001", email: "c1@test.com"));

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.Equal("Nuevo Comercio", comercio.Name);
    }

    [Fact]
    public async Task SetStatusAsync_Desactivar_InactivaLosUsuariosDelComercio()
    {
        // Arrange
        var comercio = Comercio(id: 1, activo: true);
        var service = CreateService();

        // Act
        var resultado = await service.SetStatusAsync(1, isActive: false);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.False(comercio.IsActive);
        _accountService.Verify(a => a.DeactivateUsersByCommerceAsync(1), Times.Once);
    }

    [Fact]
    public async Task SetStatusAsync_Reactivar_NoReactivaLosUsuarios()
    {
        // Arrange
        var comercio = Comercio(id: 1, activo: false);
        var service = CreateService();

        // Act
        var resultado = await service.SetStatusAsync(1, isActive: true);

        // Assert
        Assert.True(resultado.Succeeded);
        Assert.True(comercio.IsActive);
        _accountService.Verify(a => a.DeactivateUsersByCommerceAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetPagedAsync_Todos_DevuelveActivosEInactivos()
    {
        // Arrange
        Comercio(id: 1, rnc: "101000001", email: "c1@test.com", activo: true);
        Comercio(id: 2, rnc: "101000002", email: "c2@test.com", activo: false);
        var service = CreateService();

        // Act: isActive null = "todos"
        var resultado = await service.GetPagedAsync(1, isActive: null);

        // Assert
        Assert.Equal(2, resultado.TotalCount);
        Assert.Equal(2, resultado.Items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_SoloActivos_ExcluyeInactivos()
    {
        // Arrange
        Comercio(id: 1, rnc: "101000001", email: "c1@test.com", activo: true);
        Comercio(id: 2, rnc: "101000002", email: "c2@test.com", activo: false);
        var service = CreateService();

        // Act
        var resultado = await service.GetPagedAsync(1, isActive: true);

        // Assert
        var unico = Assert.Single(resultado.Items);
        Assert.True(unico.IsActive);
        Assert.Equal(1, resultado.TotalCount);
    }

    [Fact]
    public async Task SetStatusAsync_ComercioInexistente_Falla()
    {
        // Arrange
        var service = CreateService();

        // Act
        var resultado = await service.SetStatusAsync(99, false);

        // Assert
        Assert.False(resultado.Succeeded);
        Assert.Equal(Mensajes.ComercioNoExiste, resultado.Message);
    }
}
