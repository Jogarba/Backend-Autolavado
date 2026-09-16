namespace ApiAutoLavado.Aplicacion.Configuracion
{
    public class JwtOpciones
    {
        public required string Key { get; set; }

        public required string Issuer { get; set; }

        public required string Audience { get; set; }

        public int ExpiracionMinutos { get; set; } = 60;
    }
}
