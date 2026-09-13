using System.ComponentModel.DataAnnotations;
using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.UI.Dtos
{
    public class CrearTurnoRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "La placa debe tener 6 caracteres.")]
        public required string Placa { get; set; }

        [Required]
        public TipoVehiculo? TipoVehiculo { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "El teléfono debe tener 10 dígitos.")]
        [RegularExpression("^[0-9]+$", ErrorMessage = "El teléfono solo debe contener números.")]
        public required string TelefonoCliente { get; set; }

        public Guid IdServicio { get; set; }

        public Guid IdOperario { get; set; }

        public Guid IdBahia { get; set; }
    }
}
