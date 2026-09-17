using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiAutoLavado.Aplicacion.Catalogo
{
    /// <summary>
    /// Catálogo de fases del proceso de lavado (RN-05).
    /// Define la metadata de presentación de cada fase y la secuencia por defecto.
    /// La secuencia efectiva de cada servicio se persiste en <c>servicios.fases</c>.
    /// </summary>
    public static class CatalogoFases
    {
        public const string FaseInicial = "EN_COLA";
        public const string FasePatio = "EN_PATIO";
        public const string FaseFinal = "LISTO";

        public static readonly string[] SecuenciaPorDefecto =
        {
            "EN_COLA", "EN_PATIO", "ENJABONADO", "ENJUAGADO", "SECADO", "LISTO"
        };

        private static readonly string[] Terminales =
        {
            "LISTO", "FINALIZADO", "CANCELADO"
        };

        private static readonly IReadOnlyDictionary<string, (string Titulo, string Descripcion)> Metadatos =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                ["EN_COLA"] = ("En Cola", "Vehículo registrado esperando bahía u operario."),
                ["EN_PATIO"] = ("En Patio", "Vehículo ubicado en la bahía, listo para iniciar el servicio."),
                ["ENJABONADO"] = ("Enjabonado", "Aplicación de shampoo especializado y espumado activo."),
                ["ENJUAGADO"] = ("Enjuagado", "Retiro de jabón con agua a alta presión."),
                ["PULIDO"] = ("Pulido", "Pulido y abrillantado de la carrocería."),
                ["DESINFECCION"] = ("Desinfección", "Aplicación de desinfectante y ozono."),
                ["SECADO"] = ("Por Terminar / Secado", "Secado en microfibra, llantas y aspirado."),
                ["LISTO"] = ("Listo para Recoger", "Servicio listo para inspección y entrega.")
            };

        /// <summary>
        /// Devuelve la secuencia de fases de un servicio a partir del texto
        /// almacenado en <c>servicios.fases</c> (claves separadas por coma).
        /// Si no hay catálogo configurado se usa la secuencia por defecto.
        /// </summary>
        public static IReadOnlyList<string> ObtenerSecuencia(string? fases)
        {
            if (string.IsNullOrWhiteSpace(fases))
            {
                return SecuenciaPorDefecto;
            }

            var claves = fases
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalizar)
                .Where(c => c.Length > 0)
                .Distinct()
                .ToList();

            return claves.Count > 0 ? claves : SecuenciaPorDefecto;
        }

        public static bool EsTerminal(string fase)
            => Terminales.Contains(Normalizar(fase), StringComparer.OrdinalIgnoreCase);

        /// <summary>Claves de fase válidas del catálogo.</summary>
        public static IReadOnlyCollection<string> ClavesConocidas => Metadatos.Keys.ToList();

        public static bool EsFaseConocida(string clave)
            => !string.IsNullOrWhiteSpace(clave) && Metadatos.ContainsKey(Normalizar(clave));

        public static string Titulo(string clave)
            => Metadatos.TryGetValue(Normalizar(clave), out var meta) ? meta.Titulo : Normalizar(clave);

        public static string Descripcion(string clave)
            => Metadatos.TryGetValue(Normalizar(clave), out var meta)
                ? meta.Descripcion
                : "Fase del proceso de lavado.";

        /// <summary>
        /// Normaliza alias heredados (EN_PROGRESO, POR_INICIAR, etc.) a las claves
        /// del catálogo usando la secuencia del servicio.
        /// </summary>
        public static string NormalizarFase(string? fase, IReadOnlyList<string> secuencia)
        {
            var clave = Normalizar(fase);
            if (clave.Length == 0)
            {
                return clave;
            }

            switch (clave)
            {
                case "POR_INICIAR":
                    return FaseInicial;
                case "EN_PROGRESO":
                    return secuencia.Count > 1 ? secuencia[1] : FaseInicial;
                case "POR_TERMINAR":
                    var secado = secuencia.FirstOrDefault(c => c == "SECADO");
                    if (secado is not null)
                    {
                        return secado;
                    }
                    return secuencia.Count > 1 ? secuencia[^2] : FaseInicial;
                case "LISTO_PARA_RECOGER":
                    return FaseFinal;
                default:
                    return clave;
            }
        }

        private static string Normalizar(string? valor)
            => (valor ?? string.Empty).Trim().ToUpperInvariant();
    }
}
