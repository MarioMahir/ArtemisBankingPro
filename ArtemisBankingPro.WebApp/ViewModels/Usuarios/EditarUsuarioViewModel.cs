using System.ComponentModel.DataAnnotations;
using ArtemisBankingPro.Core.Domain.Constants;

namespace ArtemisBankingPro.WebApp.ViewModels.Usuarios;

public class EditarUsuarioViewModel
{
    [Required]
    public string Id { get; set; } = null!;

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

    /// <summary>Opcional: vacía = la contraseña actual no se modifica.</summary>
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña (opcional)")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = Mensajes.ContrasenasNoCoinciden)]
    [Display(Name = "Confirmar contraseña")]
    public string? ConfirmPassword { get; set; }

    /// <summary>El rol NO es editable: solo se muestra.</summary>
    [Display(Name = "Tipo de usuario")]
    public string Role { get; set; } = null!;

    /// <summary>Solo clientes: se SUMA al balance de la cuenta principal.</summary>
    [Range(0, 999999999999.99, ErrorMessage = Mensajes.MontoAdicionalNegativo)]
    [Display(Name = "Monto adicional")]
    public decimal? AdditionalAmount { get; set; }
}
