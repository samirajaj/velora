using System.ComponentModel.DataAnnotations;

namespace Velora.Features.Accounts;

public sealed class RegisterViewModel
{
    [Required, StringLength(80)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(80)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), MinLength(10)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class TwoFactorLoginViewModel
{
    [Required, StringLength(7, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public bool RememberMachine { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class RecoveryCodeLoginViewModel
{
    [Required] public string RecoveryCode { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public sealed class AuthenticatorViewModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    [Required, StringLength(7, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
}

public sealed class ForgotPasswordViewModel { [Required, EmailAddress] public string Email { get; set; } = string.Empty; }
public sealed class ResetPasswordViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), MinLength(10)] public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), MinLength(10)] public string NewPassword { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = string.Empty;
}
