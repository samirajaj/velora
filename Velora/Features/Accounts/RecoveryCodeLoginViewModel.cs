using System.ComponentModel.DataAnnotations;

namespace Velora.Features.Accounts;

public sealed class RecoveryCodeLoginViewModel
{
    [Required] public string RecoveryCode { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}
