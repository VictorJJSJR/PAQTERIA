/*
   Permisos para que el servidor web se conecte a SQL Server con autenticación
   integrada como DESKTOP-NJOK690\DELL. Ejecutar con una cuenta administradora
   de SQL Server. El rol concede solo DML sobre dbo, sin permisos de servidor.
*/
USE [master];
GO

IF DB_ID(N'Paqteria') IS NULL
    THROW 51001, 'Primero debe existir la base de datos Paqteria.', 1;

IF SUSER_ID(N'DESKTOP-NJOK690\DELL') IS NULL
    CREATE LOGIN [DESKTOP-NJOK690\DELL] FROM WINDOWS WITH DEFAULT_DATABASE = [Paqteria];
GO

USE [Paqteria];
GO

IF DATABASE_PRINCIPAL_ID(N'DESKTOP-NJOK690\DELL') IS NULL
    CREATE USER [DESKTOP-NJOK690\DELL] FOR LOGIN [DESKTOP-NJOK690\DELL] WITH DEFAULT_SCHEMA = [dbo];
GO

IF DATABASE_PRINCIPAL_ID(N'PAQTERIA_WebAccess') IS NULL
    CREATE ROLE [PAQTERIA_WebAccess] AUTHORIZATION [dbo];
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [PAQTERIA_WebAccess];
GO

IF IS_ROLEMEMBER(N'PAQTERIA_WebAccess', N'DESKTOP-NJOK690\DELL') <> 1
    ALTER ROLE [PAQTERIA_WebAccess] ADD MEMBER [DESKTOP-NJOK690\DELL];
GO
