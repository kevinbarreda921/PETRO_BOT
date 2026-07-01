IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BD_PETROBOT')
BEGIN
    CREATE DATABASE [BD_PETROBOT];
END
GO

USE [BD_PETROBOT];
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'PETRO')
BEGIN
    EXEC('CREATE SCHEMA [PETRO]');
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL]') AND type in (N'U'))
BEGIN
    CREATE TABLE [PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL] (
        [ID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [NOMBRE_GRIFO] [nvarchar](150) NOT NULL,
        [PLANTILLA] [nvarchar](250) NULL,
        [CELDA_FECHA] [nvarchar](20) NULL,
        [CELDA_EESS] [nvarchar](20) NULL,
        [PALABRA_CLAVE_EESS] [nvarchar](250) NULL,
        [CELDA_TOTAL_DB5] [nvarchar](20) NULL,
        [CELDA_TOTAL_GLP] [nvarchar](20) NULL,
        [CELDA_TOTAL_GASOHOL_PREMIUM] [nvarchar](20) NULL,
        [CELDA_TOTAL_GASOHOL_REGULAR] [nvarchar](20) NULL,
        [FECHA_ACTUALIZACION] [datetime] NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabla [PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL] creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla [PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL] ya existe.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[PETRO].[PARTE_DIARIO_TOTAL]') AND type in (N'U'))
BEGIN
    CREATE TABLE [PETRO].[PARTE_DIARIO_TOTAL] (
        [ID] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [NOMBRE_GRIFO] [nvarchar](150) NULL,
        [FECHA] [date] NULL,
        [TOTAL_SALIDA_DB5] [decimal](18, 4) NULL,
        [TOTAL_SALIDA_GLP] [decimal](18, 4) NULL,
        [TOTAL_SALIDA_GASOHOL_PREMIUM] [decimal](18, 4) NULL,
        [TOTAL_SALIDA_GASOHOL_REGULAR] [decimal](18, 4) NULL,
        [ARCHIVO_ORIGEN] [nvarchar](250) NULL,
        [NOMBRE_HOJA] [nvarchar](150) NULL,
        [FECHA_REGISTRO] [datetime] NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabla [PETRO].[PARTE_DIARIO_TOTAL] creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla [PETRO].[PARTE_DIARIO_TOTAL] ya existe.';
END
GO
