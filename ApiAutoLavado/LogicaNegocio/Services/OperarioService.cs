using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;
using ApiAutoLavado.UI.Dtos;

namespace ApiAutoLavado.LogicaNegocio.Services
{
    public class OperarioService : IOperarioService
    {
        private readonly Dictionary<Guid, Operario> _operarios = new();
        private readonly object _lock = new();

        public OperarioService()
        {
            Sembrar();
        }

        public IReadOnlyCollection<Operario> ObtenerActivos()
        {
            lock (_lock)
            {
                return _operarios.Values
                    .Where(o => o.Activo)
                    .OrderBy(o => o.Nombres)
                    .ToList();
            }
        }

        public Operario? ObtenerPorId(Guid id)
        {
            lock (_lock)
            {
                return _operarios.TryGetValue(id, out var operario) ? operario : null;
            }
        }

        public Operario Crear(CrearOperarioRequest request)
        {
            lock (_lock)
            {
                var documento = request.Documento.Trim();

                if (_operarios.Values.Any(o => string.Equals(o.Documento, documento, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ReglaNegocioException($"Ya existe un operario con el documento {documento}.");
                }

                var operario = new Operario
                {
                    Nombres = request.Nombres.Trim(),
                    Apellidos = request.Apellidos.Trim(),
                    Documento = documento,
                    Telefono = request.Telefono.Trim(),
                    Activo = request.Activo
                };

                _operarios.Add(operario.Id, operario);
                return operario;
            }
        }

        private void Sembrar()
        {
            var iniciales = new[]
            {
                new Operario { Nombres = "Andrés", Apellidos = "Díaz", Documento = "1001004", Telefono = "3004444444", Activo = true },
                new Operario { Nombres = "Carlos", Apellidos = "Ruiz", Documento = "1001003", Telefono = "3003333333", Activo = false },
                new Operario { Nombres = "Juan", Apellidos = "Pérez", Documento = "1001001", Telefono = "3001111111", Activo = true },
                new Operario { Nombres = "María", Apellidos = "Gómez", Documento = "1001002", Telefono = "3002222222", Activo = true }
            };

            foreach (var operario in iniciales)
            {
                _operarios.Add(operario.Id, operario);
            }
        }
    }
}
