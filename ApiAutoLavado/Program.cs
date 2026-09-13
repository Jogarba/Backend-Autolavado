using System.Text.Json;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using ApiAutoLavado.LogicaNegocio.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Almacenamiento en memoria (sin base de datos por ahora).
builder.Services.AddSingleton<IBahiaService, BahiaService>();
builder.Services.AddSingleton<IOperarioService, OperarioService>();
builder.Services.AddSingleton<IServicioService, ServicioService>();
builder.Services.AddSingleton<ITurnoService, TurnoService>();

// Servicio OpenAPI nativo
builder.Services.AddOpenApi();

var app = builder.Build();

// Habilitar OpenAPI y Scalar en todos los entornos (incluido Render)
app.MapOpenApi();
app.MapScalarApiReference();

// Endpoint de prueba en la raíz para comprobar que el servicio está vivo
app.MapGet("/", () => Results.Ok(new
{
    status = "Online",
    service = "API AutoLavado",
    docs = "/scalar/v1"
}));

// Descomentar solo si manejas certificados SSL directamente en la app:
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();