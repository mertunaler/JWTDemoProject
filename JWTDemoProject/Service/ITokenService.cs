using TaskManagementSystem.Model;

namespace TaskManagementSystem.Service
{
    public interface ITokenService
    {
        string GetToken(User user);
    }
}
