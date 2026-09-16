using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/turnos")]
    [Authorize]
    public class TurnosController : ControllerBase
    {
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

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

        [HttpGet("tablero")]
        public ActionResult<TableroTurnosResponse> ObtenerTablero()
        {
            return Ok(_turnoService.ObtenerTablero());
        }

        [HttpPost]
        public ActionResult<TurnoCreadoResponse> Crear([FromBody] CrearTurnoRequest request)
        {
            var response = _turnoService.Crear(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpGet("trazabilidad/{identificador}")]
        [AllowAnonymous]
        public ActionResult<TrazabilidadPublicaResponse> ObtenerTrazabilidad(string identificador)
        {
            // RNF-05: la consulta pública no expone teléfono, tarifas ni apellidos.
            var response = _turnoService.ObtenerTrazabilidad(identificador).ToPublica();
            return Ok(response);
        }

        [HttpGet("live/{identificador}")]
        [AllowAnonymous]
        public async Task TransmitirEnVivo(string identificador, CancellationToken cancellationToken)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            try
            {
                var trazabilidadInicial = _turnoService.ObtenerTrazabilidad(identificador).ToPublica();
                var jsonInicial = JsonSerializer.Serialize(trazabilidadInicial, OpcionesJson);
                await Response.WriteAsync($"data: {jsonInicial}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken);
                    try
                    {
                        var trazabilidad = _turnoService.ObtenerTrazabilidad(identificador).ToPublica();
                        var json = JsonSerializer.Serialize(trazabilidad, OpcionesJson);
                        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                    }
                    catch
                    {
                        // Si se eliminó o no se encuentra temporalmente
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cliente cerró la conexión
            }
        }

        [HttpPatch("{id:long}/fase")]
        public ActionResult<TurnoResponse> ActualizarFase(long id, [FromBody] ActualizarFaseRequest request)
        {
            return Ok(_turnoService.ActualizarFase(id, request.NuevaFase));
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
