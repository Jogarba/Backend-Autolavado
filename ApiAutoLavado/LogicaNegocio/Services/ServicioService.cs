using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.LogicaNegocio.Services
{
    public class ServicioService : IServicioService
    {
        private readonly Dictionary<Guid, Servicio> _servicios = new();
        private readonly object _lock = new();

        public ServicioService()
        {
            Sembrar();
        }

        public IReadOnlyCollection<Servicio> ObtenerTodos()
        {
            lock (_lock)
            {
                return _servicios.Values.OrderBy(s => s.Nombre).ToList();
            }
        }

        public Servicio? ObtenerPorId(Guid id)
        {
            lock (_lock)
            {
                return _servicios.TryGetValue(id, out var servicio) ? servicio : null;
            }
        }

        private void Sembrar()
        {
            var iniciales = new[]
            {
                new Servicio { Nombre = "LAVADO_GENERAL", PrecioBase = 15_000m, TiempoEstimadoMin = 30 },
                new Servicio { Nombre = "POLICHADO", PrecioBase = 80_000m, TiempoEstimadoMin = 120 },
                new Servicio { Nombre = "DETAILING", PrecioBase = 150_000m, TiempoEstimadoMin = 240 },
                new Servicio { Nombre = "DESINFECCION", PrecioBase = 40_000m, TiempoEstimadoMin = 45 }
            };

            foreach (var servicio in iniciales)
            {
                _servicios.Add(servicio.Id, servicio);
            }
        }
    }
}
