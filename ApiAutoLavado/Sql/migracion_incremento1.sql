-- 1. Catálogo de Servicios y Tiempos
CREATE TABLE IF NOT EXISTS servicios (
    id_servicio INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(50) NOT NULL,
    tarifa_base DECIMAL(10,2) NOT NULL,
    tiempo_estimado_min INT NOT NULL,
    PRIMARY KEY (id_servicio),
    UNIQUE KEY nombre (nombre)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Usuarios del Sistema
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

-- 3. Catálogo de Operarios con Estado de Disponibilidad
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

-- 4. Entidad Vehículos (Persistencia histórica para autocompletado)
CREATE TABLE IF NOT EXISTS vehiculos (
    placa VARCHAR(6) PRIMARY KEY,
    tipo_vehiculo VARCHAR(20) NOT NULL,
    telefono_cliente VARCHAR(10) NOT NULL,
    fecha_primer_registro TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 5. Tabla Transaccional de Turnos (Sin bahía)
CREATE TABLE IF NOT EXISTS turnos (
    id_turno BIGINT NOT NULL AUTO_INCREMENT,
    numero_turno VARCHAR(10) NOT NULL,
    placa VARCHAR(6) NOT NULL,
    id_servicio INT NOT NULL,
    id_operario INT NULL,
    estado_actual VARCHAR(30) NOT NULL DEFAULT 'EN_COLA',
    fecha_ingreso TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    hash_consulta VARCHAR(64) NOT NULL,
    PRIMARY KEY (id_turno),
    UNIQUE KEY hash_consulta (hash_consulta),
    KEY idx_vehiculos_placa (placa),
    KEY idx_turnos_cola (estado_actual, fecha_ingreso),
    KEY idx_operarios_estado (id_operario),
    CONSTRAINT turnos_ibfk_1 FOREIGN KEY (id_servicio) REFERENCES servicios (id_servicio),
    CONSTRAINT turnos_ibfk_2 FOREIGN KEY (id_operario) REFERENCES operarios (id_operario),
    CONSTRAINT turnos_ibfk_3 FOREIGN KEY (placa) REFERENCES vehiculos (placa)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
