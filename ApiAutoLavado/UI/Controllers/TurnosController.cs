using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.UI.Dtos;
using ApiAutoLavado.LogicaNegocio.Services;

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
        public ActionResult<IEnumerable<Turno>> ObtenerActivos()
        {
            return Ok(_turnoService.ObtenerActivos());
        }

        [HttpPost]
        public ActionResult<TurnoCreadoResponse> Crear([FromBody] CrearTurnoRequest request)
        {
            try
            {
                var response = _turnoService.Crear(request);
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (NoEncontradoException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (ReglaNegocioException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
        }
    }
}
