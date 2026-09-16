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
        public ActionResult<IEnumerable<BahiaResponse>> ObtenerTodas()
        {
            return Ok(_bahiaService.ObtenerTodas());
        }

        [HttpGet("disponibles")]
        public ActionResult<IEnumerable<BahiaResponse>> ObtenerDisponibles()
        {
            return Ok(_bahiaService.ObtenerDisponibles());
        }
    }
}
