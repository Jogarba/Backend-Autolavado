using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Services;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/servicios")]
    [Authorize]
    public class ServiciosController : ControllerBase
    {
        private readonly IServicioService _servicioService;

        public ServiciosController(IServicioService servicioService)
        {
            _servicioService = servicioService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ServicioResponse>> ObtenerTodos()
        {
            return Ok(_servicioService.ObtenerTodos());
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public ActionResult<ServicioResponse> Crear([FromBody] CrearServicioRequest request)
        {
            return StatusCode(StatusCodes.Status201Created, _servicioService.Crear(request));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public ActionResult<ServicioResponse> Editar(int id, [FromBody] EditarServicioRequest request)
        {
            return Ok(_servicioService.Editar(id, request));
        }
    }
}
