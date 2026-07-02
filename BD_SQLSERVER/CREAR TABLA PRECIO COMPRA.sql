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

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[PETRO].[PRECIO_COMPRA]') AND type in (N'U'))
BEGIN
    CREATE TABLE [PETRO].[PRECIO_COMPRA] (
        [ID] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [GRIFO] [nvarchar](150) NULL,
        [FILA] [int] NULL,
        [DESCRIPCION_PRODUCTO] [nvarchar](250) NULL,
        [CANTIDAD_GALONES] [decimal](18, 4) NULL,
        [PRECIO_GALON] [decimal](18, 4) NULL,
        [ARCHIVO_ORIGEN] [nvarchar](250) NULL,
        [FECHA_REGISTRO] [datetime] NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabla [PETRO].[PRECIO_COMPRA] creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla [PETRO].[PRECIO_COMPRA] ya existe.';
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[PETRO].[PRECIO_COMPRA]') AND name = N'CANTIDAD_GALONES')
    BEGIN
        ALTER TABLE [PETRO].[PRECIO_COMPRA] ADD [CANTIDAD_GALONES] [decimal](18, 4) NULL;
        PRINT 'Columna [CANTIDAD_GALONES] añadida a la tabla existente.';
    END
END
GO
