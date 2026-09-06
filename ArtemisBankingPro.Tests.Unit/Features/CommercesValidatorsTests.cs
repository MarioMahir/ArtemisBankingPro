using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.CreateCommerce;
using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.UpdateCommerce;
using ArtemisBankingPro.Core.Application.Features.Commerces.Commands.UpdateCommerceStatus;
using ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerceById;
using ArtemisBankingPro.Core.Application.Features.Commerces.Queries.GetCommerces;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class CommercesValidatorsTests
{
    private static CreateCommerceCommand ComercioValido() => new()
    {
        Name = "Colmado Central",
        Description = "Colmado del barrio",
        Email = "colmado@test.com",
        PhoneNumber = "8095551234",
        Rnc = "101000001"
    };

    // ---------------- CreateCommerceCommand ----------------

    [Fact]
    public void CreateCommerceCommandValidator_DatosValidos_EsValido()
    {
        var validator = new CreateCommerceCommandValidator();

        Assert.True(validator.Validate(ComercioValido()).IsValid);
    }

    [Fact]
    public void CreateCommerceCommandValidator_SinDescripcion_SigueSiendoValido()
    {
        // La descripción es opcional.
        var validator = new CreateCommerceCommandValidator();
        var command = ComercioValido();
        command.Description = null;

        Assert.True(validator.Validate(command).IsValid);
    }

    [Fact]
    public void CreateCommerceCommandValidator_SinRnc_EsInvalido()
    {
        var validator = new CreateCommerceCommandValidator();
        var command = ComercioValido();
        command.Rnc = "";

        Assert.False(validator.Validate(command).IsValid);
    }

    [Fact]
    public void CreateCommerceCommandValidator_CorreoInvalido_EsInvalido()
    {
        var validator = new CreateCommerceCommandValidator();
        var command = ComercioValido();
        command.Email = "correo-invalido";

        Assert.False(validator.Validate(command).IsValid);
    }

    // ---------------- UpdateCommerceCommand ----------------

    [Fact]
    public void UpdateCommerceCommandValidator_IdCero_EsInvalido()
    {
        var validator = new UpdateCommerceCommandValidator();

        var resultado = validator.Validate(new UpdateCommerceCommand
        {
            Id = 0, Name = "X", Email = "x@test.com", PhoneNumber = "809", Rnc = "101"
        });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void UpdateCommerceCommandValidator_DatosValidos_EsValido()
    {
        var validator = new UpdateCommerceCommandValidator();

        Assert.True(validator.Validate(new UpdateCommerceCommand
        {
            Id = 1, Name = "X", Email = "x@test.com", PhoneNumber = "809", Rnc = "101"
        }).IsValid);
    }

    // ---------------- UpdateCommerceStatusCommand ----------------

    [Fact]
    public void UpdateCommerceStatusCommandValidator_IdNegativo_EsInvalido()
    {
        var validator = new UpdateCommerceStatusCommandValidator();

        Assert.False(validator.Validate(new UpdateCommerceStatusCommand { Id = -1, IsActive = false }).IsValid);
    }

    // ---------------- GetCommercesQuery / GetCommerceByIdQuery ----------------

    [Theory]
    [InlineData("activos")]
    [InlineData("inactivos")]
    [InlineData("todos")]
    [InlineData(null)]
    public void GetCommercesQueryValidator_EstadosPermitidos_SonValidos(string? estado)
    {
        var validator = new GetCommercesQueryValidator();

        Assert.True(validator.Validate(new GetCommercesQuery { Page = 1, Status = estado }).IsValid);
    }

    [Fact]
    public void GetCommercesQueryValidator_EstadoDesconocido_EsInvalido()
    {
        var validator = new GetCommercesQueryValidator();

        Assert.False(validator.Validate(new GetCommercesQuery { Page = 1, Status = "suspendidos" }).IsValid);
    }

    [Fact]
    public void GetCommerceByIdQueryValidator_IdCero_EsInvalido()
    {
        var validator = new GetCommerceByIdQueryValidator();

        Assert.False(validator.Validate(new GetCommerceByIdQuery { Id = 0 }).IsValid);
    }
}
