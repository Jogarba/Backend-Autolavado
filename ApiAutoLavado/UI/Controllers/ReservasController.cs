using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/reservas")]
    public class ReservasController : ControllerBase
    {
        private readonly IReservaService _reservaService;

        public ReservasController(IReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        [HttpGet("disponibilidad")]
        [AllowAnonymous]
        public ActionResult<DisponibilidadFechaResponse> ConsultarDisponibilidad([FromQuery] string? fecha)
        {
            DateOnly fechaConsulta;
            if (string.IsNullOrWhiteSpace(fecha) || !DateOnly.TryParse(fecha, out fechaConsulta))
            {
                fechaConsulta = DateOnly.FromDateTime(DateTime.Now);
            }

            var disponibilidad = _reservaService.ConsultarDisponibilidad(fechaConsulta);
            return Ok(disponibilidad);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult<ReservaResponse> Crear([FromBody] CrearReservaRequest request)
        {
            var response = _reservaService.Crear(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpGet("{codigo}")]
        [AllowAnonymous]
        public ActionResult<ReservaResponse> ObtenerPorCodigo(string codigo)
        {
            var response = _reservaService.ObtenerPorCodigo(codigo);
            return Ok(response);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public ActionResult<IEnumerable<ReservaResponse>> ObtenerTodas([FromQuery] string? fecha, [FromQuery] string? placa)
        {
            if (!string.IsNullOrWhiteSpace(placa))
            {
                return Ok(_reservaService.ObtenerPorPlaca(placa));
            }

            if (!string.IsNullOrWhiteSpace(fecha) && DateOnly.TryParse(fecha, out var fechaFiltro))
            {
                return Ok(_reservaService.ObtenerPorFecha(fechaFiltro));
            }

            return Ok(_reservaService.ObtenerTodas());
        }

        [HttpPatch("{id:long}/cancelar")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<ReservaResponse> Cancelar(long id)
        {
            var response = _reservaService.Cancelar(id);
            return Ok(response);
        }

        [HttpPost("{id:long}/iniciar-turno")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<TurnoCreadoResponse> IniciarTurno(long id)
        {
            var turno = _reservaService.ConvertirEnTurno(id);
            return StatusCode(StatusCodes.Status201Created, turno);
        }
    }
}
