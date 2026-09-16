using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/turnos")]
    public class TurnosController : ControllerBase
    {
        private readonly ITurnoService _turnoService;

        public TurnosController(ITurnoService turnoService)
        {
            _turnoService = turnoService;
        }

        [HttpGet("activos")]
        public ActionResult<IEnumerable<TurnoResponse>> ObtenerActivos()
        {
            return Ok(_turnoService.ObtenerActivos());
        }

        [HttpPost]
        public ActionResult<TurnoCreadoResponse> Crear([FromBody] CrearTurnoRequest request)
        {
            var response = _turnoService.Crear(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPatch("{id:long}/finalizar")]
        public ActionResult<TurnoResponse> Finalizar(long id)
        {
            return Ok(_turnoService.Finalizar(id));
        }

        [HttpPatch("{id:long}/cancelar")]
        public ActionResult<TurnoResponse> Cancelar(long id)
        {
            return Ok(_turnoService.Cancelar(id));
        }
    }
}
