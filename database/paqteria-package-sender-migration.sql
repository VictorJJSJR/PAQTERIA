USE [Paqteria];
GO

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.PAQUETES', N'U') IS NULL
    THROW 51000, 'No existe dbo.PAQUETES. Ejecuta primero el script de esquema de PAQTERIA.', 1;

IF COL_LENGTH(N'dbo.PAQUETES', N'id_cliente') IS NULL
    THROW 51001, 'No existe dbo.PAQUETES.id_cliente; revisa que sea la base PAQTERIA correcta.', 1;

IF COL_LENGTH(N'dbo.PAQUETES', N'nombre_remitente') IS NULL
    EXEC sys.sp_executesql N'ALTER TABLE dbo.PAQUETES ADD nombre_remitente varchar(150) NULL;';

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PAQUETES')
      AND name = N'id_cliente'
      AND is_nullable = 0
)
    ALTER TABLE dbo.PAQUETES ALTER COLUMN id_cliente int NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.PAQUETES')
      AND name = N'CK_PAQUETES_CLIENTE_O_REMITENTE'
)
    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.PAQUETES WITH CHECK ADD CONSTRAINT CK_PAQUETES_CLIENTE_O_REMITENTE
            CHECK (id_cliente IS NOT NULL OR
                (nombre_remitente IS NOT NULL AND LEN(LTRIM(RTRIM(nombre_remitente))) > 0));';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
