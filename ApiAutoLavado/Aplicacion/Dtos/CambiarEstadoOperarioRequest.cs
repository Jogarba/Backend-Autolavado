using System.ComponentModel.DataAnnotations;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    /// <summary>
    /// RF-03: el administrador determina la disponibilidad del operario.
    /// Valores aceptados: DISPONIBLE (o LIBRE), OCUPADO, INACTIVO.
    /// </summary>
    public class CambiarEstadoOperarioRequest
    {
        [Required]
        public required string Estado { get; set; }
    }
}
