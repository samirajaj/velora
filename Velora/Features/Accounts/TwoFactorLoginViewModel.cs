using System.ComponentModel.DataAnnotations;

namespace Velora.Features.Accounts;

public sealed class TwoFactorLoginViewModel
{
    [Required, StringLength(7, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public bool RememberMachine { get; set; }
    public string? ReturnUrl { get; set; }
}
