using System.ComponentModel.DataAnnotations;

namespace ContratosIA.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Nome obrigatório")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail obrigatório")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha obrigatória")]
    [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Compare("Senha", ErrorMessage = "Senhas não conferem")]
    [DataType(DataType.Password)]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
