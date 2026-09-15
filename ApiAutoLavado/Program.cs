using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using ApiAutoLavado.Aplicacion.Repositorios;
using ApiAutoLavado.Aplicacion.Services;
using ApiAutoLavado.Persistencia;
using ApiAutoLavado.Persistencia.Repositorios;
using ApiAutoLavado.UI.Middleware;

// Carga las variables del archivo .env (CONECTION_STRING) antes de construir la aplicación
CargadorEnv.Cargar();

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
    options.KnownIPNetworks.Clear();
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

// Capa de persistencia (MySQL)
var cadenaConexion = ConstructorConexion.NormalizarMySql(
    Environment.GetEnvironmentVariable("CONECTION_STRING")
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "No se encontró la variable CONECTION_STRING. Defínala en el archivo .env en la raíz del proyecto."));

builder.Services.AddSingleton<IFabricaConexion>(_ => new FabricaConexionMySql(cadenaConexion));
builder.Services.AddSingleton<InicializadorBaseDatos>();
builder.Services.AddSingleton<IBahiaRepository, BahiaRepository>();
builder.Services.AddSingleton<IOperarioRepository, OperarioRepository>();
builder.Services.AddSingleton<IServicioRepository, ServicioRepository>();
builder.Services.AddSingleton<ITurnoRepository, TurnoRepository>();

// Capa de aplicación (casos de uso)
builder.Services.AddSingleton<IBahiaService, BahiaService>();
builder.Services.AddSingleton<IOperarioService, OperarioService>();
builder.Services.AddSingleton<IServicioService, ServicioService>();
builder.Services.AddSingleton<ITurnoService, TurnoService>();

// Documentación OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Crea el esquema y siembra los catálogos si la base de datos está vacía
using (var alcance = app.Services.CreateScope())
{
    alcance.ServiceProvider.GetRequiredService<InicializadorBaseDatos>().Inicializar();
}

// 0. Manejo global de excepciones (siempre el primero)
app.UseMiddleware<ManejadorExcepcionesMiddleware>();

// 1. Traduce http a https según el proxy de Render
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