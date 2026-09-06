using ArtemisBankingPro.Core.Application.Helpers;

namespace ArtemisBankingPro.Tests.Unit.Helpers;

public class CardFormatterTests
{
    [Fact]
    public void Mask_Tarjeta16Digitos_DevuelveElFormatoDelSpec()
    {
        // Formato LITERAL del spec para los listados de la API: 11 asteriscos + 4 dígitos.
        Assert.Equal("***********1234", CardFormatter.Mask("5425123456781234"));
    }

    [Fact]
    public void Mask_ConservaSoloLosUltimos4Digitos()
    {
        var enmascarada = CardFormatter.Mask("5425123456781234");

        Assert.EndsWith("1234", enmascarada);
        Assert.DoesNotContain("5425", enmascarada);
        // Formato literal del spec: 11 asteriscos + 4 dígitos = 15 caracteres.
        Assert.Equal(15, enmascarada.Length);
    }

    [Fact]
    public void LastFour_Tarjeta16Digitos_DevuelveLosUltimos4()
    {
        Assert.Equal("9876", CardFormatter.LastFour("1111222233339876"));
    }

    [Fact]
    public void LastFour_NumeroDe4OMenosDigitos_DevuelveElNumeroCompleto()
    {
        Assert.Equal("1234", CardFormatter.LastFour("1234"));
        Assert.Equal("12", CardFormatter.LastFour("12"));
    }

    [Fact]
    public void ExpirationDisplay_DevuelveFormatoMMBarraAA()
    {
        Assert.Equal("07/28", CardFormatter.ExpirationDisplay(new DateTime(2028, 7, 15)));
    }

    [Fact]
    public void ExpirationDisplay_MesDeUnDigito_LlevaCeroInicial()
    {
        Assert.Equal("01/29", CardFormatter.ExpirationDisplay(new DateTime(2029, 1, 31)));
    }
}
