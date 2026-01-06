namespace MuhasibPro.Domain.Models
{
    public enum RegistrationResult
    {
        Success,
        PasswordsDoNotMatch,
        EmailAlreadyExists,
        UsernameAlreadyExists,
    }
}
