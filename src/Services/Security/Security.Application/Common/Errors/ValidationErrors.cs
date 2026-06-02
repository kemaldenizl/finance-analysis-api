namespace Security.Application.Common.Errors;

public static class ValidationErrors
{
    public static Error Invalid(string propertyName, string message) => new("validation.invalid", $"{propertyName}: {message}");  
}

//Kullanılmıyor iptal edilecek...