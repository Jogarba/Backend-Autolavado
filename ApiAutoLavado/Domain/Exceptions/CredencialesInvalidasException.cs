namespace ApiAutoLavado.Domain.Exceptions
{
    public class CredencialesInvalidasException : Exception
    {
        public CredencialesInvalidasException(string message) : base(message)
        {
        }
    }
}
