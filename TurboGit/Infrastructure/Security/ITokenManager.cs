namespace TurboGit.Infrastructure.Security
{
    public interface ITokenManager
    {
        void SaveToken(string token);
        string? GetToken();
        void DeleteToken();
    }
}
