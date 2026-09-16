-- =============================================================================
-- MIGRACIÓN INCREMENTO 2: MÓDULO DEL CLIENTE (RESERVAS Y MONITOREO EN VIVO)
-- Sistema de Trazabilidad y Gestión de Turnos - "AutoLavado Express Sincelejo"
-- =============================================================================

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
