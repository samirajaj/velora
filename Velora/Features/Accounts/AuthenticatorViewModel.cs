using System.ComponentModel.DataAnnotations;

namespace Velora.Features.Accounts;

public sealed class AuthenticatorViewModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    [Required, StringLength(7, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
}
