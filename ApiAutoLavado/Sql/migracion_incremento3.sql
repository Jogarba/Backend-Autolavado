-- =============================================================================
-- MIGRACIÓN INCREMENTO 3
-- Fases dinámicas por servicio (RF-04 / RN-05)
-- La aplicación aplica estos mismos cambios automáticamente al arrancar.
-- =============================================================================

-- 1. Columna de fases en el catálogo de servicios (si no existe)
SET @existe_fases := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'servicios'
      AND COLUMN_NAME = 'fases'
);

SET @sql := IF(@existe_fases = 0,
    'ALTER TABLE servicios ADD COLUMN fases VARCHAR(255) NOT NULL DEFAULT '''' AFTER tiempo_estimado_min',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Secuencia de fases por servicio
UPDATE servicios SET fases = CASE nombre
    WHEN 'LAVADO_GENERAL' THEN 'EN_COLA,ENJABONADO,ENJUAGADO,SECADO,LISTO'
    WHEN 'POLICHADO'      THEN 'EN_COLA,ENJABONADO,ENJUAGADO,PULIDO,SECADO,LISTO'
    WHEN 'DETAILING'      THEN 'EN_COLA,ENJABONADO,ENJUAGADO,PULIDO,DESINFECCION,SECADO,LISTO'
    WHEN 'DESINFECCION'   THEN 'EN_COLA,DESINFECCION,SECADO,LISTO'
    ELSE 'EN_COLA,ENJABONADO,ENJUAGADO,SECADO,LISTO'
END
WHERE fases IS NULL OR fases = '';
