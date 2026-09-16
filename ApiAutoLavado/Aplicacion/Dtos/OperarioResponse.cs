using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class OperarioResponse
    {
        public int Id { get; set; }

        public required string Nombres { get; set; }

        public required string Apellidos { get; set; }

        public required string Documento { get; set; }

        public required string Telefono { get; set; }

        public bool Activo { get; set; }

        public EstadoOperario Estado { get; set; }
    }
}
