using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.UI.Dtos;
using ApiAutoLavado.LogicaNegocio.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/operarios")]
    public class OperariosController : ControllerBase
    {
        private readonly IOperarioService _operarioService;

        public OperariosController(IOperarioService operarioService)
        {
            _operarioService = operarioService;
        }

        [HttpGet("activos")]
        public ActionResult<IEnumerable<Operario>> ObtenerActivos()
        {
            return Ok(_operarioService.ObtenerActivos());
        }

        [HttpPost]
        public ActionResult<Operario> Crear([FromBody] CrearOperarioRequest request)
        {
            try
            {
                var operario = _operarioService.Crear(request);
                return StatusCode(StatusCodes.Status201Created, operario);
            }
            catch (ReglaNegocioException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }
    }
}
