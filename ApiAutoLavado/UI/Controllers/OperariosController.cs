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

        [HttpGet]
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerTodos()
        {
            return Ok(_operarioService.ObtenerTodos());
        }

        [HttpGet("activos")]
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerActivos()
        {
            return Ok(_operarioService.ObtenerActivos());
        }

        [HttpGet("inactivos")]
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerInactivos()
        {
            return Ok(_operarioService.ObtenerInactivos());
        }

        [HttpGet("ocupados")]
        public ActionResult<IEnumerable<OperarioResponse>> ObtenerOcupados()
        {
            return Ok(_operarioService.ObtenerOcupados());
        }

        [HttpPost]
        public ActionResult<OperarioResponse> Crear([FromBody] CrearOperarioRequest request)
        {
            var operario = _operarioService.Crear(request);
            return StatusCode(StatusCodes.Status201Created, operario);
        }
    }
}
