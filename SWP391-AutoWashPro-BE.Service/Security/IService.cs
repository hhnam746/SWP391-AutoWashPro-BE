namespace SWP391_AutoWashPro_BE.Service.Security;

public interface IService
{
    public string Hash(string password);
    public bool Verify(string password, string storedHash);
}
