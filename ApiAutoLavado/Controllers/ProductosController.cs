using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Models.Dtos;
using ApiAutoLavado.Services;

namespace ApiAutoLavado.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductosController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Models.Producto>> ObtenerTodos()
        {
            return Ok(_productoService.ObtenerTodos());
        }

        [HttpGet("{id:int}")]
        public ActionResult<Models.Producto> ObtenerPorId(int id)
        {
            var producto = _productoService.ObtenerPorId(id);
            return producto is null ? NotFound() : Ok(producto);
        }

        [HttpPost]
        public ActionResult<Models.Producto> Crear([FromBody] CrearProductoRequest request)
        {
            var producto = _productoService.Crear(request);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = producto.Id }, producto);
        }

        [HttpPut("{id:int}")]
        public ActionResult<Models.Producto> Actualizar(int id, [FromBody] CrearProductoRequest request)
        {
            var producto = _productoService.Actualizar(id, request);
            return producto is null ? NotFound() : Ok(producto);
        }

        [HttpDelete("{id:int}")]
        public IActionResult Eliminar(int id)
        {
            return _productoService.Eliminar(id) ? NoContent() : NotFound();
        }
    }
}
