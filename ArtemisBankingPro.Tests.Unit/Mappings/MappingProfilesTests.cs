using ArtemisBankingPro.Core.Application.Mappings;
using ArtemisBankingPro.WebApp.Mappings;
using AutoMapper;

namespace ArtemisBankingPro.Tests.Unit.Mappings;

/// <summary>
/// Valida que TODOS los perfiles de AutoMapper estén completos: cada miembro de
/// destino mapeado o ignorado explícitamente. Falla en build si un DTO/ViewModel
/// gana propiedades sin actualizar su mapeo.
/// </summary>
public class MappingProfilesTests
{
    [Fact]
    public void EntityMappingProfile_ConfiguracionValida()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EntityMappingProfile>());
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void WebApiMappingProfile_ConfiguracionValida()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<WebApiMappingProfile>());
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void WebAppMappingProfile_ConfiguracionValida()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<WebAppMappingProfile>());
        config.AssertConfigurationIsValid();
    }
}
