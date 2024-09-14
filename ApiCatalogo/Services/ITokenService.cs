using ApiCatalogo.Models;

namespace ApiCatalogo.Service
{
    public interface ITokenService
    {
        string GerarToken(string key, string issuer, string audience, UserModel user);
    }
}
