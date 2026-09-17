using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/usuarios")]
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpPost("administradores")]
        public ActionResult<UsuarioResponse> CrearAdministrador([FromBody] CrearAdministradorRequest request)
        {
            return StatusCode(StatusCodes.Status201Created, _usuarioService.CrearAdministrador(request));
        }

        [HttpGet("administradores")]
        public ActionResult<IEnumerable<UsuarioResponse>> ObtenerAdministradores()
        {
            return Ok(_usuarioService.ObtenerAdministradores());
        }

        [HttpPut("administradores/{id:int}")]
        public ActionResult<UsuarioResponse> EditarAdministrador(int id, [FromBody] EditarAdministradorRequest request)
        {
            return Ok(_usuarioService.EditarAdministrador(id, request));
        }

        [HttpPatch("administradores/{id:int}/contrasena")]
        public ActionResult<UsuarioResponse> CambiarContrasena(int id, [FromBody] CambiarContrasenaRequest request)
        {
            return Ok(_usuarioService.CambiarContrasenaAdministrador(id, request));
        }

        [HttpPatch("administradores/{id:int}/desactivar")]
        public ActionResult<UsuarioResponse> DesactivarAdministrador(int id)
        {
            return Ok(_usuarioService.DesactivarAdministrador(id));
        }
    }
}
