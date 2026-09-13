using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.LogicaNegocio.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/servicios")]
    public class ServiciosController : ControllerBase
    {
        private readonly IServicioService _servicioService;

        public ServiciosController(IServicioService servicioService)
        {
            _servicioService = servicioService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Servicio>> ObtenerTodos()
        {
            return Ok(_servicioService.ObtenerTodos());
        }
    }
}
