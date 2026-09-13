using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.LogicaNegocio.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/bahias")]
    public class BahiasController : ControllerBase
    {
        private readonly IBahiaService _bahiaService;

        public BahiasController(IBahiaService bahiaService)
        {
            _bahiaService = bahiaService;
        }

        [HttpGet("disponibles")]
        public ActionResult<IEnumerable<Bahia>> ObtenerDisponibles()
        {
            return Ok(_bahiaService.ObtenerDisponibles());
        }
    }
}
