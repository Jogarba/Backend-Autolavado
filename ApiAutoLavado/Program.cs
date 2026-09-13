using Scalar.AspNetCore;
using ApiAutoLavado.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<IProductoService, ProductoService>();

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