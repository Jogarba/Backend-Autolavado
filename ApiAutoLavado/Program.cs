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

// Servicio nativo OpenAPI de .NET 9 (ya lo tenías)
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // <-- Agrega esta línea aquí
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();