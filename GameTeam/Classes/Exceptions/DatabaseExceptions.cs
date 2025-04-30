namespace GameTeam.Classes.Exceptions;

public class RegistrationException : Exception
{
    public RegistrationException(string message) : base(message)
    {
    }
}

public class EmailAlreadyExists : RegistrationException
{
    public EmailAlreadyExists() : base("Email уже занят")
    {
    }
}

public class UsernameAlreadyExists : RegistrationException
{
    public UsernameAlreadyExists() : base($"Имя пользователя уже занято")
    {
    }
}