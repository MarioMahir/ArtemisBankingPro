using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Usuarios;

public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [Display(Name = "Nombre")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [Display(Name = "Apellido")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [Display(Name = "Cédula")]
    public string Identification { get; set; } = null!;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [Display(Name = "Usuario")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = Mensajes.ContrasenasNoCoinciden)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Debe seleccionar el tipo de usuario.")]
    [Display(Name = "Tipo de usuario")]
    public string Role { get; set; } = null!;

    /// <summary>Visible solo cuando el tipo seleccionado es Cliente.</summary>
    [Range(0, 999999999999.99, ErrorMessage = Mensajes.MontoInicialNegativo)]
    [Display(Name = "Monto inicial")]
    public decimal? InitialAmount { get; set; }
}
