-- =====================================================================
-- Migración Incremento 1 - ApiAutoLavado
-- RF-003 (inicio de sesión con roles) y RF-004 (gestión de operarios)
-- Motor: MySQL 8.x
--
-- Nota: la aplicación también aplica esta migración automáticamente al
-- arrancar (InicializadorBaseDatos). Este script es para ejecutarlo de
-- forma manual sobre una base de datos que aún no tiene los cambios.
-- Ejecutar una sola vez sobre una BD sin migrar.
-- =====================================================================

-- 1. Tabla de usuarios (credenciales y rol)
CREATE TABLE IF NOT EXISTS usuarios (
    id_usuario INT NOT NULL AUTO_INCREMENT,
    nombre_usuario VARCHAR(60) NOT NULL,
    contrasena_hash VARCHAR(100) NOT NULL,   -- hash BCrypt (nunca texto plano)
    rol VARCHAR(20) NOT NULL,                -- 'ADMINISTRADOR' | 'OPERARIO'
    activo TINYINT(1) NOT NULL DEFAULT 1,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id_usuario),
    UNIQUE KEY nombre_usuario (nombre_usuario)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Relación Operario -> Usuario (1 a 1) y fecha de creación
--    (NULL permitido para los operarios que ya existían sin cuenta).
ALTER TABLE operarios
    ADD COLUMN usuario_id INT NULL AFTER telefono,
    ADD COLUMN fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD UNIQUE KEY uq_operarios_usuario (usuario_id),
    ADD CONSTRAINT fk_operarios_usuario
        FOREIGN KEY (usuario_id) REFERENCES usuarios (id_usuario);

-- 3. Usuario Administrador inicial de prueba
--    Credenciales: admin / Admin123*
--    Hash BCrypt (coste 11) de 'Admin123*':
--    $2a$11$Ed3MuwtHckT5yY0j3GfLA.9n/aWZCztsQZx/CDRBjO1hJjr9Wn99W
INSERT INTO usuarios (nombre_usuario, contrasena_hash, rol, activo, fecha_creacion)
SELECT 'admin',
       '$2a$11$Ed3MuwtHckT5yY0j3GfLA.9n/aWZCztsQZx/CDRBjO1hJjr9Wn99W',
       'ADMINISTRADOR',
       1,
       UTC_TIMESTAMP()
WHERE NOT EXISTS (SELECT 1 FROM usuarios WHERE nombre_usuario = 'admin');

-- =====================================================================
-- Rollback (ejecutar solo si se desea revertir el Incremento 1)
-- =====================================================================
-- ALTER TABLE operarios DROP FOREIGN KEY fk_operarios_usuario;
-- ALTER TABLE operarios DROP INDEX uq_operarios_usuario;
-- ALTER TABLE operarios DROP COLUMN usuario_id;
-- ALTER TABLE operarios DROP COLUMN fecha_creacion;
-- DROP TABLE IF EXISTS usuarios;
