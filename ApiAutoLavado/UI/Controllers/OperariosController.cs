using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Services;

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
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerActivos()
        {
            return Ok(_operarioService.ObtenerActivos());
        }

        [HttpPost]
        public ActionResult<OperarioResponse> Crear([FromBody] CrearOperarioRequest request)
        {
            var operario = _operarioService.Crear(request);
            return StatusCode(StatusCodes.Status201Created, operario);
        }
    }
}
