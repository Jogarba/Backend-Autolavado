using ApiAutoLavado.Domain.Enums;
using ApiAutoLavado.Domain.Models;

namespace ApiAutoLavado.LogicaNegocio.Services
{
    public class BahiaService : IBahiaService
    {
        private readonly Dictionary<Guid, Bahia> _bahias = new();
        private readonly object _lock = new();

        public BahiaService()
        {
            Sembrar();
        }

        public IReadOnlyCollection<Bahia> ObtenerDisponibles()
        {
            lock (_lock)
            {
                return _bahias.Values
                    .Where(b => b.Estado == EstadoBahia.Disponible)
                    .OrderBy(b => b.NombreBahia)
                    .ToList();
            }
        }

        public Bahia? ObtenerPorId(Guid id)
        {
            lock (_lock)
            {
                return _bahias.TryGetValue(id, out var bahia) ? bahia : null;
            }
        }

        public bool Ocupar(Guid id)
        {
            lock (_lock)
            {
                if (!_bahias.TryGetValue(id, out var bahia) || bahia.Estado != EstadoBahia.Disponible)
                {
                    return false;
                }

                bahia.Estado = EstadoBahia.Ocupada;
                return true;
            }
        }

        public bool Liberar(Guid id)
        {
            lock (_lock)
            {
                if (!_bahias.TryGetValue(id, out var bahia))
                {
                    return false;
                }

                bahia.Estado = EstadoBahia.Disponible;
                return true;
            }
        }

        private void Sembrar()
        {
            var iniciales = new[]
            {
                new Bahia { NombreBahia = "Bahía 1", Tipo = TipoBahia.General, Estado = EstadoBahia.Disponible },
                new Bahia { NombreBahia = "Bahía 2", Tipo = TipoBahia.General, Estado = EstadoBahia.Disponible },
                new Bahia { NombreBahia = "Bahía 3", Tipo = TipoBahia.Detailing, Estado = EstadoBahia.Disponible },
                new Bahia { NombreBahia = "Bahía 4", Tipo = TipoBahia.Secado, Estado = EstadoBahia.Mantenimiento },
                new Bahia { NombreBahia = "Bahía 5", Tipo = TipoBahia.Detailing, Estado = EstadoBahia.Disponible }
            };

            foreach (var bahia in iniciales)
            {
                _bahias.Add(bahia.Id, bahia);
            }
        }
    }
}
