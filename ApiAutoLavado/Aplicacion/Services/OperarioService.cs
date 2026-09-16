using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.Aplicacion.Services
{
    public class OperarioService : IOperarioService
    {
        private readonly IOperarioRepository _operarios;

        public OperarioService(IOperarioRepository operarios)
        {
            _operarios = operarios;
        }

        public IReadOnlyCollection<OperarioResponse> ObtenerActivos()
        {
            return _operarios.ObtenerTodos()
                .Where(o => o.EstaDisponible)
                .OrderBy(o => o.Nombres)
                .Select(o => o.ToResponse())
                .ToList();
        }

        public OperarioResponse Crear(CrearOperarioRequest request)
        {
            var operario = new Operario
            {
                Nombres = request.Nombres.Trim(),
                Apellidos = request.Apellidos.Trim(),
                Documento = request.Documento.Trim(),
                Telefono = request.Telefono.Trim(),
                Activo = request.Activo
            };

            var id = _operarios.IntentarAgregar(operario);
            if (id is null)
            {
                throw new ReglaNegocioException($"Ya existe un operario con el documento {operario.Documento}.");
            }

            operario.Id = id.Value;
            return operario.ToResponse();
        }
    }
}
