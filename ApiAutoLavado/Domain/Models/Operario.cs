namespace ApiAutoLavado.Domain.Models
{
    public class Operario
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Nombres { get; set; }

        public required string Apellidos { get; set; }

        public required string Documento { get; set; }

        public required string Telefono { get; set; }

        public bool Activo { get; set; } = true;
    }
}
