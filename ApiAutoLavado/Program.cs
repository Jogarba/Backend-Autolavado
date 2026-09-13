using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using ApiAutoLavado.LogicaNegocio.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuración de controladores y formato JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configurar lectura de cabeceras de proxy de Render (resuelve el error de Mixed Content)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Habilitar CORS para permitir peticiones desde el navegador / frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Servicios en memoria
builder.Services.AddSingleton<IBahiaService, BahiaService>();
builder.Services.AddSingleton<IOperarioService, OperarioService>();
builder.Services.AddSingleton<IServicioService, ServicioService>();
builder.Services.AddSingleton<ITurnoService, TurnoService>();

// Documentación OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// 1. Debe ejecutarse de primero para traducir http a https según el proxy de Render
app.UseForwardedHeaders();

// 2. Middleware de CORS
app.UseCors();

// 3. Documentación interactiva de Scalar y OpenAPI
app.MapOpenApi();
app.MapScalarApiReference();

// Endpoint de verificación en la raíz
app.MapGet("/", () => Results.Ok(new
{
    status = "Online",
    service = "API AutoLavado",
    docs = "/scalar/v1"
}));

app.UseAuthorization();

app.MapControllers();

app.Run();