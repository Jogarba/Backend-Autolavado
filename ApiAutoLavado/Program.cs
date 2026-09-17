using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ApiAutoLavado.Aplicacion.Configuracion;
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

// Misma política para el generador de OpenAPI (usa las opciones de Minimal APIs, no las de MVC)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Configurar lectura de cabeceras de proxy de Render (resuelve el error de Mixed Content)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Habilitar CORS para permitir peticiones y SignalR WebSockets desde cualquier frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configurar SignalR para eventos en tiempo real
builder.Services.AddSignalR();

// La búsqueda combina variable de entorno y configuración. IConfiguration es
// insensible a mayúsculas, lo que evita fallos en Linux/Render cuando la variable
// se define como "connection_string" o "Connection_String".
var nombresConexion = new[]
{
    "CONECTION_STRING", "CONNECTION_STRING", "DATABASE_URL", "MYSQL_URL",
    "MYSQL_CONNECTION_STRING", "MYSQL_DATABASE_URL", "DB_CONNECTION_STRING"
};

string? cadenaCruda = null;
string? nombreConexion = null;

foreach (var nombre in nombresConexion)
{
    var valor = Environment.GetEnvironmentVariable(nombre) ?? builder.Configuration[nombre];
    if (!string.IsNullOrWhiteSpace(valor))
    {
        cadenaCruda = valor;
        nombreConexion = nombre;
        break;
    }
}

if (string.IsNullOrWhiteSpace(cadenaCruda))
{
    cadenaCruda = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(cadenaCruda))
    {
        nombreConexion = "ConnectionStrings:DefaultConnection";
    }
}

if (string.IsNullOrWhiteSpace(cadenaCruda))
{
    throw new InvalidOperationException(
        "No se encontró la cadena de conexión MySQL. Defina CONNECTION_STRING (o CONECTION_STRING / DATABASE_URL / MYSQL_URL) en las variables de entorno del servicio.");
}

var cadenaConexion = ConstructorConexion.NormalizarMySql(cadenaCruda);

Console.WriteLine(
    $"[BaseDatos] Cadena de conexión leída desde '{nombreConexion}'. Destino: {ConstructorConexion.DescribirDestino(cadenaConexion)}");

builder.Services.AddSingleton<IFabricaConexion>(_ => new FabricaConexionMySql(cadenaConexion));
builder.Services.AddSingleton<IFabricaTransacciones, FabricaTransaccionesMySql>();
builder.Services.AddSingleton<InicializadorBaseDatos>();
builder.Services.AddSingleton<IVehiculoRepository, VehiculoRepository>();
builder.Services.AddSingleton<IOperarioRepository, OperarioRepository>();
builder.Services.AddSingleton<IServicioRepository, ServicioRepository>();
builder.Services.AddSingleton<ITurnoRepository, TurnoRepository>();
builder.Services.AddSingleton<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddSingleton<IReservaRepository, ReservaRepository>();

// Configuración de autenticación JWT (roles en claims)
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["JWT_KEY"]
    ?? builder.Configuration["Jwt:Key"]
    ?? "AutolavadoSuperSecretJwtKey2025_Min32CharsLong!";

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["JWT_ISSUER"]
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "ApiAutoLavado";

var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["JWT_AUDIENCE"]
    ?? builder.Configuration["Jwt:Audience"]
    ?? "ApiAutoLavadoClientes";

var rawExp = Environment.GetEnvironmentVariable("JWT_EXPIRACION_MINUTOS")
    ?? builder.Configuration["JWT_EXPIRACION_MINUTOS"]
    ?? builder.Configuration["Jwt:ExpiracionMinutos"];

var jwtOpciones = new JwtOpciones
{
    Key = jwtKey,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpiracionMinutos = int.TryParse(rawExp, out var minutosJwt) ? minutosJwt : 60
};

builder.Services.AddSingleton(jwtOpciones);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOpciones.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOpciones.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpciones.Key)),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "unique_name"
        };
    });

builder.Services.AddAuthorization();

// Capa de aplicación (casos de uso)
builder.Services.AddSingleton<ITurnoRealtimeNotifier, TurnoRealtimeNotifier>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IOperarioService, OperarioService>();
builder.Services.AddSingleton<IServicioService, ServicioService>();
builder.Services.AddSingleton<ITurnoService, TurnoService>();
builder.Services.AddSingleton<IReservaService, ReservaService>();

// Documentación OpenAPI
builder.Services.AddOpenApi(options =>
{
    // Los enums se serializan como string; el generador los marcaba como integer.
    options.AddSchemaTransformer((schema, context, _) =>
    {
        var tipo = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (tipo.IsEnum)
        {
            schema.Type = JsonSchemaType.String;

            if (schema.Enum is { } valores)
            {
                for (var i = valores.Count - 1; i >= 0; i--)
                {
                    if (valores[i] is null || valores[i].GetValueKind() == JsonValueKind.Null)
                    {
                        valores.RemoveAt(i);
                    }
                }
            }
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Crea el esquema y siembra los catálogos si la base de datos está vacía
try
{
    using var alcance = app.Services.CreateScope();
    alcance.ServiceProvider.GetRequiredService<InicializadorBaseDatos>().Inicializar();
}
catch (Exception ex)
{
    var detalle = ex.InnerException is null
        ? ex.Message
        : $"{ex.Message} ({ex.InnerException.GetType().Name}: {ex.InnerException.Message})";

    Console.WriteLine($"[Inicializador] Advertencia al inicializar base de datos: {detalle}");

    // Solo ante un fallo se sondea la red para saber si es DNS, timeout o rechazo.
    await DiagnosticoConexion.ProbarAsync(cadenaConexion);
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ApiAutoLavado.UI.Hubs.TurnosHub>("/hubs/turnos");

app.Run();