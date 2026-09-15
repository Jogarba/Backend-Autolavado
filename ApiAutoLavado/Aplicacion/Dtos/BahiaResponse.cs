using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class BahiaResponse
    {
        public int Id { get; set; }

        public required string NombreBahia { get; set; }

        public TipoBahia Tipo { get; set; }

        public EstadoBahia Estado { get; set; }
    }
}
