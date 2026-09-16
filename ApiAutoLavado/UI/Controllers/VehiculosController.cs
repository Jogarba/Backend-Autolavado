using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.UI.Controllers
{
    [ApiController]
    [Route("api/v1/vehiculos")]
    public class VehiculosController : ControllerBase
    {
        private readonly IVehiculoRepository _vehiculos;

        public VehiculosController(IVehiculoRepository vehiculos)
        {
            _vehiculos = vehiculos;
        }

        [HttpGet("{placa}")]
        [AllowAnonymous]
        public ActionResult<Vehiculo> ObtenerPorPlaca(string placa)
        {
            var vehiculo = _vehiculos.ObtenerPorPlaca(placa.Trim().ToUpperInvariant());
            if (vehiculo == null)
            {
                return NotFound(new { mensaje = $"No se encontró vehículo con placa {placa}." });
            }

            return Ok(vehiculo);
        }

        [HttpGet("buscar")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<Vehiculo>> Buscar([FromQuery] string prefijo)
        {
            if (string.IsNullOrWhiteSpace(prefijo))
            {
                return Ok(new List<Vehiculo>());
            }

            var resultados = _vehiculos.BuscarPorPrefijo(prefijo.Trim().ToUpperInvariant());
            return Ok(resultados);
        }
    }
}
