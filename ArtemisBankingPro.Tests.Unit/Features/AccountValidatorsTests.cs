using ArtemisBankingPro.Core.Application.Features.Account.Commands.ConfirmAccount;
using ArtemisBankingPro.Core.Application.Features.Account.Commands.GetResetToken;
using ArtemisBankingPro.Core.Application.Features.Account.Commands.Login;
using ArtemisBankingPro.Core.Application.Features.Account.Commands.ResetPassword;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.Tests.Unit.Features;

public class AccountValidatorsTests
{
    // ---------------- LoginCommand ----------------

    [Fact]
    public void LoginCommandValidator_CredencialesCompletas_EsValido()
    {
        var validator = new LoginCommandValidator();

        var resultado = validator.Validate(new LoginCommand { UserName = "admin", Password = "P@ss1" });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void LoginCommandValidator_SinUsuario_EsInvalido()
    {
        var validator = new LoginCommandValidator();

        var resultado = validator.Validate(new LoginCommand { UserName = "", Password = "P@ss1" });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(LoginCommand.UserName));
    }

    [Fact]
    public void LoginCommandValidator_SinContrasena_EsInvalido()
    {
        var validator = new LoginCommandValidator();

        var resultado = validator.Validate(new LoginCommand { UserName = "admin", Password = "" });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(LoginCommand.Password));
    }

    // ---------------- ConfirmAccountCommand ----------------

    [Fact]
    public void ConfirmAccountCommandValidator_SinToken_EsInvalido()
    {
        var validator = new ConfirmAccountCommandValidator();

        var resultado = validator.Validate(new ConfirmAccountCommand { Token = "" });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void ConfirmAccountCommandValidator_ConToken_EsValido()
    {
        var validator = new ConfirmAccountCommandValidator();

        var resultado = validator.Validate(new ConfirmAccountCommand { Token = "abc123" });

        Assert.True(resultado.IsValid);
    }

    // ---------------- GetResetTokenCommand ----------------

    [Fact]
    public void GetResetTokenCommandValidator_SinUsuario_EsInvalido()
    {
        var validator = new GetResetTokenCommandValidator();

        var resultado = validator.Validate(new GetResetTokenCommand { UserName = "" });

        Assert.False(resultado.IsValid);
    }

    // ---------------- ResetPasswordCommand ----------------

    private static ResetPasswordCommand ResetValido() => new()
    {
        UserId = "user-1",
        Token = "token-1",
        Password = "Nueva123!",
        ConfirmPassword = "Nueva123!"
    };

    [Fact]
    public void ResetPasswordCommandValidator_DatosCompletos_EsValido()
    {
        var validator = new ResetPasswordCommandValidator();

        Assert.True(validator.Validate(ResetValido()).IsValid);
    }

    [Fact]
    public void ResetPasswordCommandValidator_ContrasenasNoCoinciden_EsInvalidoConMensajeDelSpec()
    {
        var validator = new ResetPasswordCommandValidator();
        var command = ResetValido();
        command.ConfirmPassword = "Otra123!";

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage == Mensajes.ContrasenasNoCoinciden);
    }

    [Fact]
    public void ResetPasswordCommandValidator_SinUserId_EsInvalido()
    {
        var validator = new ResetPasswordCommandValidator();
        var command = ResetValido();
        command.UserId = "";

        Assert.False(validator.Validate(command).IsValid);
    }
}
