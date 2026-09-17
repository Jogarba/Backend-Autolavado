using System.Data;
using Dapper;

namespace ApiAutoLavado.Persistencia
{
    internal sealed class InicializadorBaseDatos
    {
        private const string Esquema =
            """
            CREATE TABLE IF NOT EXISTS usuarios (
                id_usuario INT NOT NULL AUTO_INCREMENT,
                nombre_usuario VARCHAR(60) NOT NULL,
                contrasena_hash VARCHAR(100) NOT NULL,
                rol VARCHAR(20) NOT NULL,
                activo TINYINT(1) NOT NULL DEFAULT 1,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id_usuario),
                UNIQUE KEY nombre_usuario (nombre_usuario)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS operarios (
                id_operario INT NOT NULL AUTO_INCREMENT,
                nombres VARCHAR(60) NOT NULL,
                apellidos VARCHAR(60) NOT NULL,
                documento VARCHAR(15) NOT NULL,
                telefono VARCHAR(10) NOT NULL,
                usuario_id INT NULL,
                activo TINYINT(1) NOT NULL DEFAULT 1,
                estado VARCHAR(20) NOT NULL DEFAULT 'DISPONIBLE',
                fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id_operario),
                UNIQUE KEY documento (documento),
                UNIQUE KEY uq_operarios_usuario (usuario_id),
                CONSTRAINT fk_operarios_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id_usuario)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS servicios (
                id_servicio INT NOT NULL AUTO_INCREMENT,
                nombre VARCHAR(50) NOT NULL,
                tarifa_base DECIMAL(10,2) NOT NULL,
                tiempo_estimado_min INT NOT NULL,
                fases VARCHAR(255) NOT NULL DEFAULT 'EN_COLA,EN_PATIO,ENJABONADO,ENJUAGADO,SECADO,LISTO',
                PRIMARY KEY (id_servicio),
                UNIQUE KEY nombre (nombre)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS vehiculos (
                placa VARCHAR(6) PRIMARY KEY,
                tipo_vehiculo VARCHAR(20) NOT NULL,
                telefono_cliente VARCHAR(10) NOT NULL,
                fecha_primer_registro TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS bahias (
                id_bahia INT NOT NULL AUTO_INCREMENT,
                nombre VARCHAR(50) NOT NULL,
                estado VARCHAR(20) NOT NULL DEFAULT 'DISPONIBLE',
                fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id_bahia),
                UNIQUE KEY uq_bahias_nombre (nombre)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS turnos (
                id_turno BIGINT NOT NULL AUTO_INCREMENT,
                numero_turno VARCHAR(10) NOT NULL,
                placa VARCHAR(6) NOT NULL,
                id_servicio INT NOT NULL,
                id_operario INT NULL,
                id_bahia INT NULL,
                estado_actual VARCHAR(30) NOT NULL DEFAULT 'EN_COLA',
                fecha_ingreso TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                hash_consulta VARCHAR(64) NOT NULL,
                PRIMARY KEY (id_turno),
                UNIQUE KEY hash_consulta (hash_consulta),
                KEY idx_vehiculos_placa (placa),
                KEY idx_turnos_cola (estado_actual, fecha_ingreso),
                KEY idx_operarios_estado (id_operario),
                KEY idx_turnos_bahia (id_bahia),
                CONSTRAINT turnos_ibfk_1 FOREIGN KEY (id_servicio) REFERENCES servicios (id_servicio),
                CONSTRAINT turnos_ibfk_2 FOREIGN KEY (id_operario) REFERENCES operarios (id_operario),
                CONSTRAINT turnos_ibfk_4 FOREIGN KEY (id_bahia) REFERENCES bahias (id_bahia),
                CONSTRAINT turnos_ibfk_3 FOREIGN KEY (placa) REFERENCES vehiculos (placa)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

            CREATE TABLE IF NOT EXISTS reservas (
                id_reserva BIGINT NOT NULL AUTO_INCREMENT,
                codigo_reserva VARCHAR(10) NOT NULL,
                placa VARCHAR(6) NOT NULL,
                id_servicio INT NOT NULL,
                fecha_reserva DATE NOT NULL,
                hora_reserva TIME NOT NULL,
                estado VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE',
                fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id_reserva),
                UNIQUE KEY uq_codigo_reserva (codigo_reserva),
                KEY idx_reservas_fecha (fecha_reserva),
                KEY idx_reservas_placa (placa),
                CONSTRAINT fk_reservas_vehiculo FOREIGN KEY (placa) REFERENCES vehiculos (placa),
                CONSTRAINT fk_reservas_servicio FOREIGN KEY (id_servicio) REFERENCES servicios (id_servicio)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """;

        private readonly IFabricaConexion _fabrica;

        public InicializadorBaseDatos(IFabricaConexion fabrica)
        {
            _fabrica = fabrica;
        }

        public void Inicializar()
        {
            const int maxIntentos = 5;
            for (int intento = 1; intento <= maxIntentos; intento++)
            {
                try
                {
                    using var conexion = _fabrica.Crear();

                    foreach (var sentencia in Esquema.Split(
                                 ';',
                                 StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        conexion.Execute(sentencia);
                    }

                    AsegurarColumnas(conexion);
                    AsegurarEsquemaBahias(conexion);
                    AsegurarEsquemaTurnos(conexion);
                    AsegurarFasesServicios(conexion);
                    SembrarUsuarioAdministrador(conexion);
                    SembrarOperarios(conexion);
                    SembrarServicios(conexion);
                    SembrarBahias(conexion);
                    SincronizarEstadosHuerfanos(conexion);
                    
                    Console.WriteLine("[BaseDatos] Esquema y catálogos inicializados correctamente.");
                    return;
                }
                catch (Exception ex) when (intento < maxIntentos)
                {
                    var detalle = ex.InnerException is null
                        ? ex.Message
                        : $"{ex.Message} ({ex.InnerException.GetType().Name}: {ex.InnerException.Message})";

                    Console.WriteLine($"[BaseDatos] Advertencia: intento {intento}/{maxIntentos} falló al conectar a MySQL: {detalle}. Reintentando en 3s...");
                    Thread.Sleep(3000);
                }
            }
        }

        private static void SincronizarEstadosHuerfanos(IDbConnection conexion)
        {
            conexion.Execute(
                "UPDATE operarios SET estado = 'DISPONIBLE' " +
                "WHERE activo = 1 AND estado = 'OCUPADO' " +
                "AND id_operario NOT IN (" +
                "    SELECT id_operario FROM turnos " +
                "    WHERE id_operario IS NOT NULL AND estado_actual NOT IN ('FINALIZADO', 'CANCELADO'));");
        }

        private static void AsegurarFasesServicios(IDbConnection conexion)
        {
            var existeFases = conexion.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'servicios' AND COLUMN_NAME = 'fases'");

            if (existeFases == 0)
            {
                conexion.Execute(
                    "ALTER TABLE servicios ADD COLUMN fases VARCHAR(255) NOT NULL DEFAULT '' AFTER tiempo_estimado_min");
            }

            // Backfill para bases existentes (RN-05): cada servicio recibe su secuencia de fases.
            conexion.Execute(
                """
                UPDATE servicios SET fases = CASE nombre
                    WHEN 'LAVADO_GENERAL' THEN 'EN_COLA,EN_PATIO,ENJABONADO,ENJUAGADO,SECADO,LISTO'
                    WHEN 'POLICHADO'      THEN 'EN_COLA,EN_PATIO,ENJABONADO,ENJUAGADO,PULIDO,SECADO,LISTO'
                    WHEN 'DETAILING'      THEN 'EN_COLA,EN_PATIO,ENJABONADO,ENJUAGADO,PULIDO,DESINFECCION,SECADO,LISTO'
                    WHEN 'DESINFECCION'   THEN 'EN_COLA,EN_PATIO,DESINFECCION,SECADO,LISTO'
                    ELSE 'EN_COLA,EN_PATIO,ENJABONADO,ENJUAGADO,SECADO,LISTO'
                END
                WHERE fases IS NULL OR fases = '';
                """);

            // Servicios existentes: inserta "EN_PATIO" justo después de "EN_COLA"
            // para reflejar el nuevo estado intermedio (una sola vez).
            conexion.Execute(
                """
                UPDATE servicios
                SET fases = REPLACE(fases, 'EN_COLA', 'EN_COLA,EN_PATIO')
                WHERE fases LIKE '%EN_COLA%' AND fases NOT LIKE '%EN_PATIO%';
                """);
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

            var existeUsuarioId = conexion.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operarios' AND COLUMN_NAME = 'usuario_id'");

            if (existeUsuarioId == 0)
            {
                conexion.Execute("ALTER TABLE operarios ADD COLUMN usuario_id INT NULL AFTER telefono");
                conexion.Execute("ALTER TABLE operarios ADD UNIQUE KEY uq_operarios_usuario (usuario_id)");
                conexion.Execute(
                    "ALTER TABLE operarios ADD CONSTRAINT fk_operarios_usuario " +
                    "FOREIGN KEY (usuario_id) REFERENCES usuarios (id_usuario)");
            }

            var existeFechaCreacion = conexion.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'operarios' AND COLUMN_NAME = 'fecha_creacion'");

            if (existeFechaCreacion == 0)
            {
                conexion.Execute(
                    "ALTER TABLE operarios ADD COLUMN fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP");
            }

            // Turnos: columna id_bahia para bases creadas antes de las bahías.
            var existeIdBahia = conexion.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'turnos' AND COLUMN_NAME = 'id_bahia'");

            if (existeIdBahia == 0)
            {
                conexion.Execute("ALTER TABLE turnos ADD COLUMN id_bahia INT NULL AFTER id_operario");
                conexion.Execute("ALTER TABLE turnos ADD KEY idx_turnos_bahia (id_bahia)");
                conexion.Execute(
                    "ALTER TABLE turnos ADD CONSTRAINT turnos_ibfk_4 " +
                    "FOREIGN KEY (id_bahia) REFERENCES bahias (id_bahia)");
            }
        }

        private static void AsegurarEsquemaBahias(IDbConnection conexion)
        {
            var existeTabla = conexion.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.TABLES " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bahias'");

            if (existeTabla == 0)
            {
                return;
            }

            long ContarColumna(string columna) => conexion.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bahias' AND COLUMN_NAME = @Columna",
                new { Columna = columna });

            // Bases creadas antes del incremento 4 usaban 'nombre_bahia' y 'tipo'.
            // El repositorio actual espera 'nombre' y 'fecha_creacion'.
            if (ContarColumna("nombre") == 0)
            {
                if (ContarColumna("nombre_bahia") > 0)
                {
                    conexion.Execute(
                        "ALTER TABLE bahias CHANGE COLUMN nombre_bahia nombre VARCHAR(50) NOT NULL");
                }
                else
                {
                    conexion.Execute(
                        "ALTER TABLE bahias ADD COLUMN nombre VARCHAR(50) NOT NULL DEFAULT ''");
                }
            }

            // 'tipo' ya no se utiliza: se deja opcional para no romper los INSERT actuales.
            if (ContarColumna("tipo") > 0)
            {
                conexion.Execute(
                    "ALTER TABLE bahias MODIFY COLUMN tipo VARCHAR(30) NULL DEFAULT NULL");
            }

            if (ContarColumna("fecha_creacion") == 0)
            {
                conexion.Execute(
                    "ALTER TABLE bahias ADD COLUMN fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP");
            }
        }

        private static void AsegurarEsquemaTurnos(IDbConnection conexion)
        {
            var existeTabla = conexion.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM information_schema.TABLES " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'turnos'");

            if (existeTabla == 0)
            {
                return;
            }

            // Bases creadas con el esquema previo al incremento 4 tenían columnas
            // NOT NULL que ya no se escriben (la bahía se asigna después, el
            // operario puede quedar en cola y el tipo/teléfono viven en vehiculos).
            void HacerNula(string columna, string definicion)
            {
                var esNotNula = conexion.ExecuteScalar<long>(
                    "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'turnos' " +
                    "AND COLUMN_NAME = @Columna AND IS_NULLABLE = 'NO'",
                    new { Columna = columna });

                if (esNotNula > 0)
                {
                    conexion.Execute($"ALTER TABLE turnos MODIFY COLUMN {columna} {definicion} NULL");
                }
            }

            HacerNula("id_operario", "INT");
            HacerNula("id_bahia", "INT");
            HacerNula("tipo_vehiculo", "VARCHAR(20)");
            HacerNula("telefono_cliente", "VARCHAR(10)");

            // Consistencia del nuevo estado: un turno con bahía no puede estar "EN_COLA".
            conexion.Execute(
                "UPDATE turnos SET estado_actual = 'EN_PATIO' " +
                "WHERE id_bahia IS NOT NULL AND estado_actual = 'EN_COLA'");
        }

        private static void SembrarUsuarioAdministrador(IDbConnection conexion)
        {
            if (conexion.ExecuteScalar<long>("SELECT COUNT(*) FROM usuarios") > 0)
            {
                return;
            }

            var nombreUsuario = Environment.GetEnvironmentVariable("ADMIN_USUARIO") ?? "admin";
            var contrasena = Environment.GetEnvironmentVariable("ADMIN_CONTRASENA") ?? "Admin123*";

            conexion.Execute(
                "INSERT INTO usuarios (nombre_usuario, contrasena_hash, rol, activo, fecha_creacion) " +
                "VALUES (@NombreUsuario, @ContrasenaHash, 'ADMINISTRADOR', 1, UTC_TIMESTAMP())",
                new
                {
                    NombreUsuario = nombreUsuario,
                    ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(contrasena)
                });
        }



        private static void SembrarOperarios(IDbConnection conexion)
        {
            if (conexion.ExecuteScalar<long>("SELECT COUNT(*) FROM operarios") > 0)
            {
                return;
            }

            var contrasena = Environment.GetEnvironmentVariable("OPERARIO_CONTRASENA") ?? "Operario123*";
            var hash = BCrypt.Net.BCrypt.HashPassword(contrasena);

            var operarios = new[]
            {
                new { Nombres = "Andrés", Apellidos = "Díaz", Documento = "1001004", Telefono = "3004444444", Activo = 1 },
                new { Nombres = "Carlos", Apellidos = "Ruiz", Documento = "1001003", Telefono = "3003333333", Activo = 0 },
                new { Nombres = "Juan", Apellidos = "Pérez", Documento = "1001001", Telefono = "3001111111", Activo = 1 },
                new { Nombres = "María", Apellidos = "Gómez", Documento = "1001002", Telefono = "3002222222", Activo = 1 }
            };

            foreach (var operario in operarios)
            {
                // Cada operario sembrado recibe credenciales para poder iniciar sesión (RF-03).
                conexion.Execute(
                    "INSERT INTO usuarios (nombre_usuario, contrasena_hash, rol, activo, fecha_creacion) " +
                    "VALUES (@NombreUsuario, @ContrasenaHash, 'OPERARIO', @Activo, UTC_TIMESTAMP())",
                    new
                    {
                        NombreUsuario = operario.Documento,
                        ContrasenaHash = hash,
                        operario.Activo
                    });

                var usuarioId = conexion.ExecuteScalar<int>("SELECT LAST_INSERT_ID()");

                conexion.Execute(
                    "INSERT INTO operarios (nombres, apellidos, documento, telefono, usuario_id, activo, estado) " +
                    "VALUES (@Nombres, @Apellidos, @Documento, @Telefono, @UsuarioId, @Activo, @Estado)",
                    new
                    {
                        operario.Nombres,
                        operario.Apellidos,
                        operario.Documento,
                        operario.Telefono,
                        UsuarioId = usuarioId,
                        operario.Activo,
                        Estado = operario.Activo == 1 ? "DISPONIBLE" : "INACTIVO"
                    });
            }
        }

        private static void SembrarServicios(IDbConnection conexion)
        {
            if (conexion.ExecuteScalar<long>("SELECT COUNT(*) FROM servicios") > 0)
            {
                return;
            }

            var servicios = new[]
            {
                new
                {
                    Nombre = "LAVADO_GENERAL",
                    Tarifa = 15_000m,
                    Tiempo = 30,
                    Fases = "EN_COLA,EN_PATIO,ENJABONADO,ENJUAGADO,SECADO,LISTO"
                },
                new
                {
                    Nombre = "POLICHADO",
                    Tarifa = 80_000m,
                    Tiempo = 120,
                    Fases = "EN_COLA,EN_PATIO,ENJABONADO,ENJUAGADO,PULIDO,SECADO,LISTO"
                },
                new
                {
                    Nombre = "DETAILING",
                    Tarifa = 150_000m,
                    Tiempo = 240,
                    Fases = "EN_COLA,EN_PATIO,ENJABONADO,ENJUAGADO,PULIDO,DESINFECCION,SECADO,LISTO"
                },
                new
                {
                    Nombre = "DESINFECCION",
                    Tarifa = 40_000m,
                    Tiempo = 45,
                    Fases = "EN_COLA,EN_PATIO,DESINFECCION,SECADO,LISTO"
                }
            };

            conexion.Execute(
                "INSERT INTO servicios (nombre, tarifa_base, tiempo_estimado_min, fases) " +
                "VALUES (@Nombre, @Tarifa, @Tiempo, @Fases)",
                servicios);
        }

        private static void SembrarBahias(IDbConnection conexion)
        {
            if (conexion.ExecuteScalar<long>("SELECT COUNT(*) FROM bahias") > 0)
            {
                return;
            }

            var bahias = new[]
            {
                new { Nombre = "BAHIA 1" },
                new { Nombre = "BAHIA 2" },
                new { Nombre = "BAHIA 3" },
                new { Nombre = "BAHIA 4" }
            };

            conexion.Execute(
                "INSERT INTO bahias (nombre, estado) VALUES (@Nombre, 'DISPONIBLE')",
                bahias);
        }
    }
}
