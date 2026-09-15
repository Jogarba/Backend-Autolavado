using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class ServicioService : IServicioService
    {
        private readonly IServicioRepository _servicios;

        public ServicioService(IServicioRepository servicios)
        {
            _servicios = servicios;
        }

        public IReadOnlyCollection<ServicioResponse> ObtenerTodos()
        {
            return _servicios.ObtenerTodos()
                .OrderBy(s => s.Nombre)
                .Select(s => s.ToResponse())
                .ToList();
        }
    }
}
