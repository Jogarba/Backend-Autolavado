using System.ComponentModel.DataAnnotations;
using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class CrearTurnoRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "La placa debe tener 6 caracteres.")]
        public required string Placa { get; set; }

        public TipoVehiculo? TipoVehiculo { get; set; }

        [StringLength(10, MinimumLength = 10, ErrorMessage = "El teléfono debe tener 10 dígitos.")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "El teléfono solo debe contener números.")]
        public string? TelefonoCliente { get; set; }

        public required int IdServicio { get; set; }
    }
}
