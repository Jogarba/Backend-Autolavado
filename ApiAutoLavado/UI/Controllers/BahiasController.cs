using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/bahias")]
    [Authorize]
    public class BahiasController : ControllerBase
    {
        private readonly IBahiaService _bahiaService;

        public BahiasController(IBahiaService bahiaService)
        {
            _bahiaService = bahiaService;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public ActionResult<IEnumerable<BahiaResponse>> ObtenerTodas()
        {
            return Ok(_bahiaService.ObtenerTodas());
        }

        [HttpGet("disponibles")]
        public ActionResult<IEnumerable<BahiaResponse>> ObtenerDisponibles()
        {
            return Ok(_bahiaService.ObtenerDisponibles());
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public ActionResult<BahiaResponse> Crear([FromBody] CrearBahiaRequest request)
        {
            return StatusCode(StatusCodes.Status201Created, _bahiaService.Crear(request));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<BahiaResponse> Editar(int id, [FromBody] EditarBahiaRequest request)
        {
            return Ok(_bahiaService.Editar(id, request));
        }

        [HttpPatch("{id:int}/estado")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<BahiaResponse> CambiarEstado(int id, [FromBody] CambiarEstadoBahiaRequest request)
        {
            return Ok(_bahiaService.CambiarEstado(id, request.Estado));
        }
    }
}
