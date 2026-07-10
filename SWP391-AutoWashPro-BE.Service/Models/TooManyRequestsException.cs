namespace SWP391_AutoWashPro_BE.Service.Models;

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message)
    {
    }
}
