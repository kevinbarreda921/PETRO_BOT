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

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[PETRO].[REPORTE_PRECIO_LISTA]') AND type in (N'U'))
BEGIN
    CREATE TABLE [PETRO].[REPORTE_PRECIO_LISTA] (
        [ID] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ARCHIVO_ORIGEN] [nvarchar](250) NULL,
        [FECHA_IMPORTACION] [datetime] NOT NULL DEFAULT GETDATE(),
        [CODIGO_LOCAL] [nvarchar](50) NULL,
        [NOMBRE_LOCAL] [nvarchar](150) NULL,
        [FECHA_TURNO] [date] NULL,
        [FECHA_EMISION] [datetime] NULL,
        [TIPO_DOCUMENTO] [nvarchar](50) NULL,
        [NUMERO_DOCUMENTO] [nvarchar](50) NULL,
        [CODIGO_CLIENTE] [nvarchar](50) NULL,
        [RAZON_SOCIAL] [nvarchar](250) NULL,
        [NRO_ITEM] [int] NULL,
        [CODIGO_PRODUCTO] [nvarchar](50) NULL,
        [SUB_CODIGO_PRODUCTO] [nvarchar](50) NULL,
        [DESCRIPCION_PRODUCTO] [nvarchar](200) NULL,
        [CANTIDAD] [decimal](18, 4) NULL,
        [UNIDAD] [nvarchar](20) NULL,
        [PRECIO_UNITARIO_CON_IGV] [decimal](18, 4) NULL,
        [PRECIO_LISTA] [decimal](18, 4) NULL,
        [IGV_SOLES] [decimal](18, 4) NULL,
        [IMPORTE_CON_IGV_SOLES] [decimal](18, 4) NULL,
        [MTO_RECAUDO] [decimal](18, 4) NULL,
        [MTO_DESCUENTO] [decimal](18, 4) NULL,
        [ESTADO] [nvarchar](50) NULL,
        [FORMA_PAGO] [nvarchar](100) NULL
    );
    PRINT 'Tabla [PETRO].[REPORTE_PRECIO_LISTA] creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla [PETRO].[REPORTE_PRECIO_LISTA] ya existe.';
END
GO