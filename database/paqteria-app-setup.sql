/*
   Preparación aditiva para la aplicación web de PAQTERIA.
   Ejecutar sobre la base ya creada por el script de la base de datos.
   No borra tablas ni datos. Es idempotente y se puede ejecutar más de una vez.
*/
USE [Paqteria];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'dbo.USUARIOS', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ROLES', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PAQUETES', N'U') IS NULL
   OR OBJECT_ID(N'dbo.INCIDENCIAS_ENTREGA', N'U') IS NULL
    THROW 51000, 'La base Paqteria no tiene las tablas requeridas del script original.', 1;

IF OBJECT_ID(N'dbo.APP_CAMBIOS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.APP_CAMBIOS
    (
        id_cambio bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_APP_CAMBIOS PRIMARY KEY,
        entidad varchar(32) NOT NULL,
        fecha_utc datetime2(7) NOT NULL CONSTRAINT DF_APP_CAMBIOS_fecha_utc DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_APP_CAMBIOS_fecha_utc ON dbo.APP_CAMBIOS(fecha_utc);
END;

IF COL_LENGTH(N'dbo.PAQUETES', N'fecha_actualizacion') IS NULL
    ALTER TABLE dbo.PAQUETES ADD fecha_actualizacion datetime2(7) NULL;
IF COL_LENGTH(N'dbo.PAQUETES', N'version_fila') IS NULL
    ALTER TABLE dbo.PAQUETES ADD version_fila rowversion NOT NULL;
GO
UPDATE dbo.PAQUETES
    SET fecha_actualizacion = COALESCE(fecha_creacion, SYSUTCDATETIME())
    WHERE fecha_actualizacion IS NULL;

IF COL_LENGTH(N'dbo.INCIDENCIAS_ENTREGA', N'estado') IS NULL
    ALTER TABLE dbo.INCIDENCIAS_ENTREGA ADD estado varchar(50) NOT NULL
        CONSTRAINT DF_INCIDENCIAS_ENTREGA_estado DEFAULT ('Abierta');
IF COL_LENGTH(N'dbo.INCIDENCIAS_ENTREGA', N'severidad') IS NULL
    ALTER TABLE dbo.INCIDENCIAS_ENTREGA ADD severidad varchar(20) NOT NULL
        CONSTRAINT DF_INCIDENCIAS_ENTREGA_severidad DEFAULT ('Media');
IF COL_LENGTH(N'dbo.INCIDENCIAS_ENTREGA', N'fecha_actualizacion') IS NULL
    ALTER TABLE dbo.INCIDENCIAS_ENTREGA ADD fecha_actualizacion datetime2(7) NULL;
IF COL_LENGTH(N'dbo.INCIDENCIAS_ENTREGA', N'version_fila') IS NULL
    ALTER TABLE dbo.INCIDENCIAS_ENTREGA ADD version_fila rowversion NOT NULL;
GO
UPDATE dbo.INCIDENCIAS_ENTREGA
    SET fecha_actualizacion = COALESCE(fecha_hora, SYSUTCDATETIME())
    WHERE fecha_actualizacion IS NULL;

INSERT INTO dbo.ROLES(nombre)
SELECT roles.nombre
FROM (VALUES ('Administrator'), ('Warehouse Manager'), ('Customer'), ('Driver')) AS roles(nombre)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ROLES existing
    WHERE UPPER(LTRIM(RTRIM(existing.nombre))) = UPPER(roles.nombre)
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ROLES') AND name = N'UX_ROLES_nombre')
    CREATE UNIQUE INDEX UX_ROLES_nombre ON dbo.ROLES(nombre);
GO

CREATE OR ALTER TRIGGER dbo.TR_PAQUETES_APP_CAMBIOS
ON dbo.PAQUETES
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        UPDATE package SET fecha_actualizacion = SYSUTCDATETIME()
        FROM dbo.PAQUETES package INNER JOIN inserted changed ON changed.id_paquete = package.id_paquete;
    ELSE IF EXISTS (SELECT 1 FROM inserted)
        UPDATE package SET fecha_actualizacion = COALESCE(package.fecha_actualizacion, package.fecha_creacion, SYSUTCDATETIME())
        FROM dbo.PAQUETES package INNER JOIN inserted added ON added.id_paquete = package.id_paquete
        WHERE package.fecha_actualizacion IS NULL;

    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('packages');
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_INCIDENCIAS_ENTREGA_APP_CAMBIOS
ON dbo.INCIDENCIAS_ENTREGA
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        UPDATE incident SET fecha_actualizacion = SYSUTCDATETIME()
        FROM dbo.INCIDENCIAS_ENTREGA incident INNER JOIN inserted changed ON changed.id_incidencia = incident.id_incidencia;
    ELSE IF EXISTS (SELECT 1 FROM inserted)
        UPDATE incident SET fecha_actualizacion = COALESCE(incident.fecha_actualizacion, incident.fecha_hora, SYSUTCDATETIME())
        FROM dbo.INCIDENCIAS_ENTREGA incident INNER JOIN inserted added ON added.id_incidencia = incident.id_incidencia
        WHERE incident.fecha_actualizacion IS NULL;

    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('incidents');
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_USUARIOS_APP_CAMBIOS
ON dbo.USUARIOS
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('users');
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_ROLES_APP_CAMBIOS
ON dbo.ROLES
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('users');
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_TURNOS_REPARTIDOR_APP_CAMBIOS
ON dbo.TURNOS_REPARTIDOR
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('drivers');
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_UNIDADES_APP_CAMBIOS
ON dbo.UNIDADES
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('drivers');
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_CENTROS_DISTRIBUCION_APP_CAMBIOS
ON dbo.CENTROS_DISTRIBUCION
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('packages');
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_HISTORIAL_SEGUIMIENTO_APP_CAMBIOS
ON dbo.HISTORIAL_SEGUIMIENTO
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('packages');
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_PARADAS_RUTA_APP_CAMBIOS
ON dbo.PARADAS_RUTA
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted) OR EXISTS (SELECT 1 FROM deleted)
        INSERT INTO dbo.APP_CAMBIOS(entidad) VALUES ('drivers');
END;
GO

COMMIT TRANSACTION;
GO
