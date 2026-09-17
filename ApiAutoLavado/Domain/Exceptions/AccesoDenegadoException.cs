namespace ApiAutoLavado.Domain.Exceptions
{
    public class AccesoDenegadoException : Exception
    {
        public AccesoDenegadoException(string mensaje) : base(mensaje)
        {
        }
    }
}
