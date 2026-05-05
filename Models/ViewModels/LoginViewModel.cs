using System.ComponentModel.DataAnnotations;

namespace ContratosIA.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-mail obrigatório")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha obrigatória")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    public bool LembrarMe { get; set; }
}
