using System.ComponentModel.DataAnnotations;

namespace Velora.Features.Accounts;

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}
