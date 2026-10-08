/*
    Instalación portable de la base de datos PAQTERIA.
    Ejecutar en una instancia de SQL Server donde la base [Paqteria] no exista
    o esté vacía. Se usan las rutas de datos y registro predeterminadas del servidor.
    El script no elimina bases ni datos existentes.

    Se conserva el esquema del archivo de origen. No se incluyen filas de datos:
    el archivo original no contenía instrucciones INSERT ni datos exportados.

    Los inicios de sesión pertenecen a cada servidor. El usuario [paqteria_user]
    solo se mapea si ya existe un login con ese nombre en la instancia.
*/
USE [master]
GO
IF DB_ID(N'Paqteria') IS NULL
    CREATE DATABASE [Paqteria]
GO
USE [Paqteria]
GO

-- Principal de base de datos conservado del script original; no crea un login.
IF DATABASE_PRINCIPAL_ID(N'paqteria_app') IS NULL
    CREATE USER [paqteria_app] WITHOUT LOGIN WITH DEFAULT_SCHEMA=[dbo]
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members AS drm
    WHERE drm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datareader')
      AND drm.member_principal_id = DATABASE_PRINCIPAL_ID(N'paqteria_app')
)
    ALTER ROLE [db_datareader] ADD MEMBER [paqteria_app]
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members AS drm
    WHERE drm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datawriter')
      AND drm.member_principal_id = DATABASE_PRINCIPAL_ID(N'paqteria_app')
)
    ALTER ROLE [db_datawriter] ADD MEMBER [paqteria_app]
GO

-- El login del usuario depende de cada servidor; si no existe, se omite.
IF SUSER_ID(N'paqteria_user') IS NOT NULL
   AND DATABASE_PRINCIPAL_ID(N'paqteria_user') IS NULL
    CREATE USER [paqteria_user] FOR LOGIN [paqteria_user] WITH DEFAULT_SCHEMA=[dbo]
GO
IF DATABASE_PRINCIPAL_ID(N'paqteria_user') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.database_role_members AS drm
       WHERE drm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datareader')
         AND drm.member_principal_id = DATABASE_PRINCIPAL_ID(N'paqteria_user')
   )
    ALTER ROLE [db_datareader] ADD MEMBER [paqteria_user]
GO
IF DATABASE_PRINCIPAL_ID(N'paqteria_user') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.database_role_members AS drm
       WHERE drm.role_principal_id = DATABASE_PRINCIPAL_ID(N'db_datawriter')
         AND drm.member_principal_id = DATABASE_PRINCIPAL_ID(N'paqteria_user')
   )
    ALTER ROLE [db_datawriter] ADD MEMBER [paqteria_user]
GO
/****** Objeto: Table [dbo].[APP_CAMBIOS] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[APP_CAMBIOS](
	[id_cambio] [bigint] IDENTITY(1,1) NOT NULL,
	[entidad] [varchar](32) NOT NULL,
	[fecha_utc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_APP_CAMBIOS] PRIMARY KEY CLUSTERED 
(
	[id_cambio] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[CENTROS_DISTRIBUCION] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CENTROS_DISTRIBUCION](
	[id_centro] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](150) NOT NULL,
	[ciudad] [varchar](100) NOT NULL,
	[direccion] [varchar](255) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id_centro] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[COMPROBANTES_ENTREGA] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[COMPROBANTES_ENTREGA](
	[id_comprobante] [int] IDENTITY(1,1) NOT NULL,
	[id_paquete] [int] NOT NULL,
	[id_repartidor] [int] NOT NULL,
	[nombre_receptor] [varchar](150) NOT NULL,
	[url_foto_evidencia] [varchar](500) NULL,
	[url_firma_receptor] [varchar](500) NULL,
	[fecha_hora] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id_comprobante] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[HISTORIAL_SEGUIMIENTO] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HISTORIAL_SEGUIMIENTO](
	[id_historial] [int] IDENTITY(1,1) NOT NULL,
	[id_paquete] [int] NOT NULL,
	[titulo] [varchar](150) NOT NULL,
	[descripcion] [text] NULL,
	[fecha_hora] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id_historial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[INCIDENCIAS_ENTREGA] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[INCIDENCIAS_ENTREGA](
	[id_incidencia] [int] IDENTITY(1,1) NOT NULL,
	[id_paquete] [int] NOT NULL,
	[id_repartidor] [int] NOT NULL,
	[tipo_incidencia] [varchar](100) NOT NULL,
	[comentario] [text] NULL,
	[url_foto_reporte] [varchar](500) NULL,
	[fecha_hora] [datetime2](7) NULL,
	[estado] [varchar](50) NOT NULL,
	[severidad] [varchar](20) NOT NULL,
	[fecha_actualizacion] [datetime2](7) NULL,
	[version_fila] [timestamp] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id_incidencia] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[PAQUETES] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PAQUETES](
	[id_paquete] [int] IDENTITY(1,1) NOT NULL,
	[folio] [varchar](50) NOT NULL,
	[id_cliente] [int] NULL,
	[id_centro_origen] [int] NOT NULL,
	[direccion_origen] [varchar](255) NOT NULL,
	[direccion_destino] [varchar](255) NOT NULL,
	[coordenadas_destino] [varchar](100) NULL,
	[peso_kg] [decimal](10, 2) NOT NULL,
	[tamano_etiqueta] [varchar](50) NULL,
	[es_prioritario] [bit] NULL,
	[es_fragil] [bit] NULL,
	[estado_actual] [varchar](50) NOT NULL,
	[fecha_creacion] [datetime2](7) NULL,
	[fecha_actualizacion] [datetime2](7) NULL,
	[version_fila] [timestamp] NOT NULL,
	[nombre_remitente] [varchar](150) NULL,
PRIMARY KEY CLUSTERED 
(
	[id_paquete] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[folio] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[PARADAS_RUTA] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PARADAS_RUTA](
	[id_parada] [int] IDENTITY(1,1) NOT NULL,
	[id_turno] [int] NOT NULL,
	[id_paquete] [int] NOT NULL,
	[orden_secuencia] [int] NOT NULL,
	[estado_parada] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id_parada] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[ROLES] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ROLES](
	[id_rol] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](100) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id_rol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[TICKETS_SOPORTE] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TICKETS_SOPORTE](
	[id_ticket] [int] IDENTITY(1,1) NOT NULL,
	[id_cliente] [int] NOT NULL,
	[id_paquete] [int] NOT NULL,
	[motivo] [varchar](150) NOT NULL,
	[descripcion] [text] NULL,
	[estado] [varchar](50) NOT NULL,
	[fecha_creacion] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id_ticket] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[TURNOS_REPARTIDOR] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TURNOS_REPARTIDOR](
	[id_turno] [int] IDENTITY(1,1) NOT NULL,
	[id_repartidor] [int] NOT NULL,
	[id_unidad] [int] NOT NULL,
	[fecha_turno] [date] NOT NULL,
	[estado_turno] [varchar](50) NOT NULL,
	[total_paquetes] [int] NULL,
	[tiempo_estimado_horas] [decimal](5, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[id_turno] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[UNIDADES] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UNIDADES](
	[id_unidad] [int] IDENTITY(1,1) NOT NULL,
	[id_centro] [int] NOT NULL,
	[codigo_unidad] [varchar](50) NOT NULL,
	[placas] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id_unidad] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[codigo_unidad] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Table [dbo].[USUARIOS] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[USUARIOS](
	[id_usuario] [int] IDENTITY(1,1) NOT NULL,
	[id_rol] [int] NOT NULL,
	[nombre] [varchar](150) NOT NULL,
	[email] [varchar](150) NOT NULL,
	[telefono] [varchar](20) NULL,
	[password_hash] [varchar](255) NOT NULL,
	[fecha_registro] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id_usuario] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Objeto: Index [IX_APP_CAMBIOS_fecha_utc] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
CREATE NONCLUSTERED INDEX [IX_APP_CAMBIOS_fecha_utc] ON [dbo].[APP_CAMBIOS]
(
	[fecha_utc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Objeto: Index [UX_ROLES_nombre] Fecha de script: 08/10/2026 11:30:12 a. m. ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_ROLES_nombre] ON [dbo].[ROLES]
(
	[nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[APP_CAMBIOS] ADD  CONSTRAINT [DF_APP_CAMBIOS_fecha_utc]  DEFAULT (sysutcdatetime()) FOR [fecha_utc]
GO
ALTER TABLE [dbo].[COMPROBANTES_ENTREGA] ADD  DEFAULT (getdate()) FOR [fecha_hora]
GO
ALTER TABLE [dbo].[HISTORIAL_SEGUIMIENTO] ADD  DEFAULT (getdate()) FOR [fecha_hora]
GO
ALTER TABLE [dbo].[INCIDENCIAS_ENTREGA] ADD  DEFAULT (getdate()) FOR [fecha_hora]
GO
ALTER TABLE [dbo].[INCIDENCIAS_ENTREGA] ADD  CONSTRAINT [DF_INCIDENCIAS_ENTREGA_estado]  DEFAULT ('Abierta') FOR [estado]
GO
ALTER TABLE [dbo].[INCIDENCIAS_ENTREGA] ADD  CONSTRAINT [DF_INCIDENCIAS_ENTREGA_severidad]  DEFAULT ('Media') FOR [severidad]
GO
ALTER TABLE [dbo].[PAQUETES] ADD  DEFAULT ((0)) FOR [es_prioritario]
GO
ALTER TABLE [dbo].[PAQUETES] ADD  DEFAULT ((0)) FOR [es_fragil]
GO
ALTER TABLE [dbo].[PAQUETES] ADD  DEFAULT (getdate()) FOR [fecha_creacion]
GO
ALTER TABLE [dbo].[TICKETS_SOPORTE] ADD  DEFAULT (getdate()) FOR [fecha_creacion]
GO
ALTER TABLE [dbo].[TURNOS_REPARTIDOR] ADD  DEFAULT ((0)) FOR [total_paquetes]
GO
ALTER TABLE [dbo].[USUARIOS] ADD  DEFAULT (getdate()) FOR [fecha_registro]
GO
ALTER TABLE [dbo].[COMPROBANTES_ENTREGA]  WITH CHECK ADD  CONSTRAINT [FK_COMPROBANTES_PAQUETES] FOREIGN KEY([id_paquete])
REFERENCES [dbo].[PAQUETES] ([id_paquete])
GO
ALTER TABLE [dbo].[COMPROBANTES_ENTREGA] CHECK CONSTRAINT [FK_COMPROBANTES_PAQUETES]
GO
ALTER TABLE [dbo].[COMPROBANTES_ENTREGA]  WITH CHECK ADD  CONSTRAINT [FK_COMPROBANTES_REPARTIDOR] FOREIGN KEY([id_repartidor])
REFERENCES [dbo].[USUARIOS] ([id_usuario])
GO
ALTER TABLE [dbo].[COMPROBANTES_ENTREGA] CHECK CONSTRAINT [FK_COMPROBANTES_REPARTIDOR]
GO
ALTER TABLE [dbo].[HISTORIAL_SEGUIMIENTO]  WITH CHECK ADD  CONSTRAINT [FK_HISTORIAL_PAQUETES] FOREIGN KEY([id_paquete])
REFERENCES [dbo].[PAQUETES] ([id_paquete])
GO
ALTER TABLE [dbo].[HISTORIAL_SEGUIMIENTO] CHECK CONSTRAINT [FK_HISTORIAL_PAQUETES]
GO
ALTER TABLE [dbo].[INCIDENCIAS_ENTREGA]  WITH CHECK ADD  CONSTRAINT [FK_INCIDENCIAS_PAQUETES] FOREIGN KEY([id_paquete])
REFERENCES [dbo].[PAQUETES] ([id_paquete])
GO
ALTER TABLE [dbo].[INCIDENCIAS_ENTREGA] CHECK CONSTRAINT [FK_INCIDENCIAS_PAQUETES]
GO
ALTER TABLE [dbo].[INCIDENCIAS_ENTREGA]  WITH CHECK ADD  CONSTRAINT [FK_INCIDENCIAS_REPARTIDOR] FOREIGN KEY([id_repartidor])
REFERENCES [dbo].[USUARIOS] ([id_usuario])
GO
ALTER TABLE [dbo].[INCIDENCIAS_ENTREGA] CHECK CONSTRAINT [FK_INCIDENCIAS_REPARTIDOR]
GO
ALTER TABLE [dbo].[PAQUETES]  WITH CHECK ADD  CONSTRAINT [FK_PAQUETES_CENTRO_ORIGEN] FOREIGN KEY([id_centro_origen])
REFERENCES [dbo].[CENTROS_DISTRIBUCION] ([id_centro])
GO
ALTER TABLE [dbo].[PAQUETES] CHECK CONSTRAINT [FK_PAQUETES_CENTRO_ORIGEN]
GO
ALTER TABLE [dbo].[PAQUETES]  WITH CHECK ADD  CONSTRAINT [FK_PAQUETES_CLIENTE] FOREIGN KEY([id_cliente])
REFERENCES [dbo].[USUARIOS] ([id_usuario])
GO
ALTER TABLE [dbo].[PAQUETES] CHECK CONSTRAINT [FK_PAQUETES_CLIENTE]
GO
ALTER TABLE [dbo].[PARADAS_RUTA]  WITH CHECK ADD  CONSTRAINT [FK_PARADAS_PAQUETES] FOREIGN KEY([id_paquete])
REFERENCES [dbo].[PAQUETES] ([id_paquete])
GO
ALTER TABLE [dbo].[PARADAS_RUTA] CHECK CONSTRAINT [FK_PARADAS_PAQUETES]
GO
ALTER TABLE [dbo].[PARADAS_RUTA]  WITH CHECK ADD  CONSTRAINT [FK_PARADAS_TURNOS] FOREIGN KEY([id_turno])
REFERENCES [dbo].[TURNOS_REPARTIDOR] ([id_turno])
GO
ALTER TABLE [dbo].[PARADAS_RUTA] CHECK CONSTRAINT [FK_PARADAS_TURNOS]
GO
ALTER TABLE [dbo].[TICKETS_SOPORTE]  WITH CHECK ADD  CONSTRAINT [FK_TICKETS_CLIENTE] FOREIGN KEY([id_cliente])
REFERENCES [dbo].[USUARIOS] ([id_usuario])
GO
ALTER TABLE [dbo].[TICKETS_SOPORTE] CHECK CONSTRAINT [FK_TICKETS_CLIENTE]
GO
ALTER TABLE [dbo].[TICKETS_SOPORTE]  WITH CHECK ADD  CONSTRAINT [FK_TICKETS_PAQUETES] FOREIGN KEY([id_paquete])
REFERENCES [dbo].[PAQUETES] ([id_paquete])
GO
ALTER TABLE [dbo].[TICKETS_SOPORTE] CHECK CONSTRAINT [FK_TICKETS_PAQUETES]
GO
ALTER TABLE [dbo].[TURNOS_REPARTIDOR]  WITH CHECK ADD  CONSTRAINT [FK_TURNOS_REPARTIDOR] FOREIGN KEY([id_repartidor])
REFERENCES [dbo].[USUARIOS] ([id_usuario])
GO
ALTER TABLE [dbo].[TURNOS_REPARTIDOR] CHECK CONSTRAINT [FK_TURNOS_REPARTIDOR]
GO
ALTER TABLE [dbo].[TURNOS_REPARTIDOR]  WITH CHECK ADD  CONSTRAINT [FK_TURNOS_UNIDADES] FOREIGN KEY([id_unidad])
REFERENCES [dbo].[UNIDADES] ([id_unidad])
GO
ALTER TABLE [dbo].[TURNOS_REPARTIDOR] CHECK CONSTRAINT [FK_TURNOS_UNIDADES]
GO
ALTER TABLE [dbo].[UNIDADES]  WITH CHECK ADD  CONSTRAINT [FK_UNIDADES_CENTROS] FOREIGN KEY([id_centro])
REFERENCES [dbo].[CENTROS_DISTRIBUCION] ([id_centro])
GO
ALTER TABLE [dbo].[UNIDADES] CHECK CONSTRAINT [FK_UNIDADES_CENTROS]
GO
ALTER TABLE [dbo].[USUARIOS]  WITH CHECK ADD  CONSTRAINT [FK_USUARIOS_ROLES] FOREIGN KEY([id_rol])
REFERENCES [dbo].[ROLES] ([id_rol])
GO
ALTER TABLE [dbo].[USUARIOS] CHECK CONSTRAINT [FK_USUARIOS_ROLES]
GO
ALTER TABLE [dbo].[PAQUETES]  WITH CHECK ADD  CONSTRAINT [CK_PAQUETES_CLIENTE_O_REMITENTE] CHECK  (([id_cliente] IS NOT NULL OR [nombre_remitente] IS NOT NULL AND len(ltrim(rtrim([nombre_remitente])))>(0)))
GO
ALTER TABLE [dbo].[PAQUETES] CHECK CONSTRAINT [CK_PAQUETES_CLIENTE_O_REMITENTE]
GO
