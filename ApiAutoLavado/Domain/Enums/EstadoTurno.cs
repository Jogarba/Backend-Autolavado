using System.Text.Json.Serialization;

namespace ApiAutoLavado.Domain.Enums
{
    public enum EstadoTurno
    {
        [JsonStringEnumMemberName("RECEPCION")]
        Recepcion
    }
}
