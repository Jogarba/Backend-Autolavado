using ApiAutoLavado.Aplicacion.Dtos;

namespace ApiAutoLavado.Aplicacion.Services
{
    public interface IAuthService
    {
        LoginResponse Login(LoginRequest request);
    }
}
