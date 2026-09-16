using ApiAutoLavado.Domain.Exceptions;

namespace ApiAutoLavado.UI.Middleware
{
    public sealed class ManejadorExcepcionesMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ManejadorExcepcionesMiddleware> _logger;

        public ManejadorExcepcionesMiddleware(
            RequestDelegate next,
            ILogger<ManejadorExcepcionesMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (NoEncontradoException ex)
            {
                await EscribirRespuestaAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (CredencialesInvalidasException ex)
            {
                await EscribirRespuestaAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
            }
            catch (ReglaNegocioException ex)
            {
                await EscribirRespuestaAsync(context, StatusCodes.Status409Conflict, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado al procesar {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
                await EscribirRespuestaAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Ocurrió un error inesperado al procesar la solicitud.");
            }
        }

        private static async Task EscribirRespuestaAsync(HttpContext context, int codigo, string mensaje)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            await Results.Problem(statusCode: codigo, title: mensaje, detail: mensaje)
                .ExecuteAsync(context);
        }
    }
}
