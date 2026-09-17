-- =============================================================================
-- MIGRACIÓN INCREMENTO 4: BAHÍAS (el operario elige la bahía al iniciar el turno)
-- La aplicación aplica estos mismos cambios automáticamente al arrancar.
-- =============================================================================

-- 1. Catálogo de bahías
CREATE TABLE IF NOT EXISTS bahias (
    id_bahia INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(50) NOT NULL,
    estado VARCHAR(20) NOT NULL DEFAULT 'DISPONIBLE',
    fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id_bahia),
    UNIQUE KEY uq_bahias_nombre (nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Columna id_bahia en turnos (si no existe)
SET @existe_id_bahia := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'turnos'
      AND COLUMN_NAME = 'id_bahia'
);

SET @sql := IF(@existe_id_bahia = 0,
    'ALTER TABLE turnos ADD COLUMN id_bahia INT NULL AFTER id_operario',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 3. Bahías iniciales de ejemplo
INSERT INTO bahias (nombre, estado)
SELECT * FROM (
    SELECT 'BAHIA 1' AS nombre, 'DISPONIBLE' AS estado UNION ALL
    SELECT 'BAHIA 2', 'DISPONIBLE' UNION ALL
    SELECT 'BAHIA 3', 'DISPONIBLE' UNION ALL
    SELECT 'BAHIA 4', 'DISPONIBLE'
) AS nuevas
WHERE NOT EXISTS (SELECT 1 FROM bahias);
