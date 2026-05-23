namespace SWP391_AutoWashPro_BE.Service.Models;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
