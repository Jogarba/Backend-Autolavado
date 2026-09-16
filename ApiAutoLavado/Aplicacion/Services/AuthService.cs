using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiAutoLavado.Aplicacion.Configuracion;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;
using Microsoft.IdentityModel.Tokens;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarios;
        private readonly JwtOpciones _opciones;

        public AuthService(IUsuarioRepository usuarios, JwtOpciones opciones)
        {
            _usuarios = usuarios;
            _opciones = opciones;
        }

        public LoginResponse Login(LoginRequest request)
        {
            var usuario = _usuarios.ObtenerPorNombreUsuario(request.NombreUsuario.Trim());

            if (usuario is null
                || !usuario.Activo
                || !BCrypt.Net.BCrypt.Verify(request.Contrasena, usuario.ContrasenaHash))
            {
                throw new CredencialesInvalidasException("Usuario o contraseña incorrectos.");
            }

            var (token, expiraEn) = GenerarToken(usuario);

            return new LoginResponse
            {
                Token = token,
                Rol = usuario.Rol,
                NombreUsuario = usuario.NombreUsuario,
                ExpiraEn = expiraEn
            };
        }

        private (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario)
        {
            var expiraEn = DateTime.UtcNow.AddMinutes(_opciones.ExpiracionMinutos);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),
                new Claim("role", usuario.Rol.ToString())
            };

            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _opciones.Issuer,
                audience: _opciones.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiraEn,
                signingCredentials: credenciales);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
        }
    }
}
