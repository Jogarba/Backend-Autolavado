using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/operarios")]
    [Authorize]
    public class OperariosController : ControllerBase
    {
        private readonly IOperarioService _operarioService;

        public OperariosController(IOperarioService operarioService)
        {
            _operarioService = operarioService;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerTodos()
        {   
            return Ok(_operarioService.ObtenerTodos());
        }

        [HttpGet("activos")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerActivos()
        {
            return Ok(_operarioService.ObtenerActivos());
        }

        [HttpGet("inactivos")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerInactivos()
        {
            return Ok(_operarioService.ObtenerInactivos());
        }

        [HttpGet("ocupados")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerOcupados()
        {
            return Ok(_operarioService.ObtenerOcupados());
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public ActionResult<OperarioResponse> Crear([FromBody] CrearOperarioRequest request)
        {
            var operario = _operarioService.Crear(request);
            return StatusCode(StatusCodes.Status201Created, operario);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<OperarioResponse> Editar(int id, [FromBody] EditarOperarioRequest request)
        {
            return Ok(_operarioService.Editar(id, request));
        }

        [HttpPatch("{id:int}/desactivar")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<OperarioResponse> Desactivar(int id)
        {
            return Ok(_operarioService.Desactivar(id));
        }

        [HttpPatch("{id:int}/estado")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<OperarioResponse> CambiarEstado(int id, [FromBody] CambiarEstadoOperarioRequest request)
        {
            return Ok(_operarioService.CambiarEstado(id, request.Estado));
        }
    }
}
