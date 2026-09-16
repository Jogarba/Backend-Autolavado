using System.Text.Json.Serialization;

namespace ApiAutoLavado.Domain.Enums
{
    public enum EstadoTurno
    {
        [JsonStringEnumMemberName("RECEPCION")]
        Recepcion,

        [JsonStringEnumMemberName("FINALIZADO")]
        Finalizado,

        [JsonStringEnumMemberName("CANCELADO")]
        Cancelado
    }
}
