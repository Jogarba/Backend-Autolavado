namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class ServicioResponse
    {
        public int Id { get; set; }

        public required string Nombre { get; set; }

        public decimal PrecioBase { get; set; }

        public int TiempoEstimadoMin { get; set; }
    }
}
