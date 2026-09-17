using System.Collections.Generic;
using System.Linq;
using ApiAutoLavado.Aplicacion.Catalogo;
using ApiAutoLavado.Aplicacion.Dtos;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Domain.Exceptions;
using ApiAutoLavado.Domain.Models;

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

        public ServicioResponse Crear(CrearServicioRequest request)
        {
            var servicio = new Servicio
            {
                Nombre = request.Nombre.Trim().ToUpperInvariant(),
                PrecioBase = request.PrecioBase,
                TiempoEstimadoMin = request.TiempoEstimadoMin,
                Fases = ValidarFases(request.Fases)
            };

            var id = _servicios.Crear(servicio);
            if (id == 0)
            {
                throw new ReglaNegocioException($"Ya existe un servicio con el nombre {servicio.Nombre}.");
            }

            servicio.Id = id;
            return servicio.ToResponse();
        }

        public ServicioResponse Editar(int id, EditarServicioRequest request)
        {
            var servicio = _servicios.ObtenerPorId(id)
                ?? throw new NoEncontradoException($"No existe un servicio con id {id}.");

            servicio.Nombre = request.Nombre.Trim().ToUpperInvariant();
            servicio.PrecioBase = request.PrecioBase;
            servicio.TiempoEstimadoMin = request.TiempoEstimadoMin;
            servicio.Fases = ValidarFases(request.Fases);

            if (!_servicios.Actualizar(servicio))
            {
                throw new ReglaNegocioException($"Ya existe un servicio con el nombre {servicio.Nombre}.");
            }

            return servicio.ToResponse();
        }

        /// <summary>
        /// Normaliza y valida la secuencia de fases (RN-05): debe ser no vacía, sin
        /// duplicados, con fases conocidas, iniciar en EN_COLA y terminar en LISTO.
        /// </summary>
        private static string ValidarFases(List<string>? fases)
        {
            if (fases is null || fases.Count < 2)
            {
                throw new ReglaNegocioException("Debe indicar al menos dos fases para el servicio.");
            }

            var normalizadas = new List<string>();
            foreach (var fase in fases)
            {
                var limpia = (fase ?? string.Empty).Trim().ToUpperInvariant();
                if (limpia.Length == 0)
                {
                    throw new ReglaNegocioException("Las fases no pueden estar vacías.");
                }

                if (normalizadas.Contains(limpia))
                {
                    throw new ReglaNegocioException($"La fase {limpia} está repetida.");
                }

                if (!CatalogoFases.EsFaseConocida(limpia))
                {
                    throw new ReglaNegocioException(
                        $"Fase no reconocida: {limpia}. Fases válidas: {string.Join(", ", CatalogoFases.ClavesConocidas)}.");
                }

                normalizadas.Add(limpia);
            }

            if (normalizadas[0] != CatalogoFases.FaseInicial)
            {
                throw new ReglaNegocioException($"La primera fase debe ser {CatalogoFases.FaseInicial}.");
            }

            if (normalizadas[^1] != CatalogoFases.FaseFinal)
            {
                throw new ReglaNegocioException($"La última fase debe ser {CatalogoFases.FaseFinal}.");
            }

            return string.Join(",", normalizadas);
        }
    }
}
