using System.Data;
using Dapper;

namespace ApiAutoLavado.Persistencia
{
    internal sealed class InicializadorBaseDatos
    {
        private const string Esquema =
            """
            CREATE TABLE IF NOT EXISTS bahias (
                id_bahia INT NOT NULL AUTO_INCREMENT,
                nombre_bahia VARCHAR(30) NOT NULL,
                tipo VARCHAR(30) NOT NULL,
                estado VARCHAR(20) NOT NULL DEFAULT 'DISPONIBLE',
                PRIMARY KEY (id_bahia),
                UNIQUE KEY nombre_bahia (nombre_bahia)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS operarios (
                id_operario INT NOT NULL AUTO_INCREMENT,
                nombres VARCHAR(60) NOT NULL,
                apellidos VARCHAR(60) NOT NULL,
                documento VARCHAR(15) NOT NULL,
                telefono VARCHAR(10) NOT NULL,
                activo TINYINT(1) NOT NULL DEFAULT 1,
                estado VARCHAR(20) NOT NULL DEFAULT 'DISPONIBLE',
                PRIMARY KEY (id_operario),
                UNIQUE KEY documento (documento)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS servicios (
                id_servicio INT NOT NULL AUTO_INCREMENT,
                nombre VARCHAR(50) NOT NULL,
                tarifa_base DECIMAL(10,2) NOT NULL,
                tiempo_estimado_min INT NOT NULL,
                PRIMARY KEY (id_servicio),
                UNIQUE KEY nombre (nombre)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS turnos (
                id_turno BIGINT NOT NULL AUTO_INCREMENT,
                numero_turno VARCHAR(10) NOT NULL,
                placa VARCHAR(6) NOT NULL,
                tipo_vehiculo VARCHAR(20) NOT NULL,
                telefono_cliente VARCHAR(10) NOT NULL,
                id_servicio INT NOT NULL,
                id_operario INT NOT NULL,
                id_bahia INT NOT NULL,
                estado_actual VARCHAR(30) NOT NULL DEFAULT 'RECEPCION',
                fecha_ingreso TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                hash_consulta VARCHAR(64) NOT NULL,
                PRIMARY KEY (id_turno),
                UNIQUE KEY hash_consulta (hash_consulta),
                KEY id_servicio (id_servicio),
                KEY id_operario (id_operario),
                KEY id_bahia (id_bahia),
                CONSTRAINT turnos_ibfk_1 FOREIGN KEY (id_servicio) REFERENCES servicios (id_servicio),
                CONSTRAINT turnos_ibfk_2 FOREIGN KEY (id_operario) REFERENCES operarios (id_operario),
                CONSTRAINT turnos_ibfk_3 FOREIGN KEY (id_bahia) REFERENCES bahias (id_bahia)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """;

        private readonly IFabricaConexion _fabrica;

        public InicializadorBaseDatos(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public void Inicializar()
        {
            using var conexion = _fabrica.Crear();

            foreach (var sentencia in Esquema.Split(
                         ';',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                conexion.Execute(sentencia);
            }

            AsegurarColumnas(conexion);
            SembrarBahias(conexion);
            SembrarOperarios(conexion);
            SembrarServicios(conexion);
        }

        private static void AsegurarColumnas(IDbConnection conexion)
        {
            var existeEstado = conexion.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operarios' AND COLUMN_NAME = 'estado'");

            if (existeEstado == 0)
            {
                conexion.Execute(
                    "ALTER TABLE operarios ADD COLUMN estado VARCHAR(20) NOT NULL DEFAULT 'DISPONIBLE' AFTER activo");
            }
        }

        private static void SembrarBahias(IDbConnection conexion)
        {
            if (conexion.ExecuteScalar<long>("SELECT COUNT(*) FROM bahias") > 0)
            {
                return;
            }

            var bahias = new[]
            {
                new { Nombre = "Bahía 1", Tipo = "GENERAL", Estado = "DISPONIBLE" },
                new { Nombre = "Bahía 2", Tipo = "GENERAL", Estado = "DISPONIBLE" },
                new { Nombre = "Bahía 3", Tipo = "DETAILING", Estado = "DISPONIBLE" },
                new { Nombre = "Bahía 4", Tipo = "SECADO", Estado = "MANTENIMIENTO" },
                new { Nombre = "Bahía 5", Tipo = "DETAILING", Estado = "DISPONIBLE" }
            };

            conexion.Execute(
                "INSERT INTO bahias (nombre_bahia, tipo, estado) VALUES (@Nombre, @Tipo, @Estado)",
                bahias);
        }

        private static void SembrarOperarios(IDbConnection conexion)
        {
            if (conexion.ExecuteScalar<long>("SELECT COUNT(*) FROM operarios") > 0)
            {
                return;
            }

            var operarios = new[]
            {
                new { Nombres = "Andrés", Apellidos = "Díaz", Documento = "1001004", Telefono = "3004444444", Activo = 1 },
                new { Nombres = "Carlos", Apellidos = "Ruiz", Documento = "1001003", Telefono = "3003333333", Activo = 0 },
                new { Nombres = "Juan", Apellidos = "Pérez", Documento = "1001001", Telefono = "3001111111", Activo = 1 },
                new { Nombres = "María", Apellidos = "Gómez", Documento = "1001002", Telefono = "3002222222", Activo = 1 }
            };

            conexion.Execute(
                "INSERT INTO operarios (nombres, apellidos, documento, telefono, activo) " +
                "VALUES (@Nombres, @Apellidos, @Documento, @Telefono, @Activo)",
                operarios);
        }

        private static void SembrarServicios(IDbConnection conexion)
        {
            if (conexion.ExecuteScalar<long>("SELECT COUNT(*) FROM servicios") > 0)
            {
                return;
            }

            var servicios = new[]
            {
                new { Nombre = "LAVADO_GENERAL", Tarifa = 15_000m, Tiempo = 30 },
                new { Nombre = "POLICHADO", Tarifa = 80_000m, Tiempo = 120 },
                new { Nombre = "DETAILING", Tarifa = 150_000m, Tiempo = 240 },
                new { Nombre = "DESINFECCION", Tarifa = 40_000m, Tiempo = 45 }
            };

            conexion.Execute(
                "INSERT INTO servicios (nombre, tarifa_base, tiempo_estimado_min) " +
                "VALUES (@Nombre, @Tarifa, @Tiempo)",
                servicios);
        }
    }
}
