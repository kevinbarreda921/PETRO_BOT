USE [BD_PETROBOT]
GO
/****** Object:  StoredProcedure [PETRO].[SP_REPORTE_PRECIO_LISTA]    Script Date: 27/06/2026 10:34:55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [PETRO].[SP_REPORTE_PRECIO_LISTA]
    @NOMBRE_LOCAL NVARCHAR(150),
    @FECHA_TURNO_MES VARCHAR(6)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Sumamos las cantidades usando TODO el universo de datos (sin obviar nada)
    WITH CantidadesTotales AS (
        SELECT 
            @NOMBRE_LOCAL AS NOMBRE_LOCAL,
            FECHA_TURNO,
            DESCRIPCION_PRODUCTO,
            PRECIO_LISTA,
            SUM(CANTIDAD) AS CANTIDAD_TOTAL
        FROM [PETRO].[REPORTE_PRECIO_LISTA]
        WHERE NOMBRE_LOCAL = @NOMBRE_LOCAL
          AND FECHA_TURNO_MES = @FECHA_TURNO_MES
          AND ESTADO = 'ACTIVO'
          AND DESCRIPCION_PRODUCTO NOT LIKE '%GAS LICUADO%'
          AND DESCRIPCION_PRODUCTO NOT LIKE '%GLP%'
        GROUP BY FECHA_TURNO, DESCRIPCION_PRODUCTO, PRECIO_LISTA
    ),

    -- 2. Enumeramos secuencialmente cada transacción individual usando la hora de FECHA_EMISION (Excluyendo las 00:00:00)
    VentasConHora AS (
        SELECT 
            FECHA_TURNO,
            DESCRIPCION_PRODUCTO,
            PRECIO_LISTA,
            FECHA_EMISION,
            ROW_NUMBER() OVER (
                PARTITION BY FECHA_TURNO, DESCRIPCION_PRODUCTO 
                ORDER BY FECHA_EMISION ASC
            ) AS SecuenciaTemporal
        FROM [PETRO].[REPORTE_PRECIO_LISTA]
        WHERE NOMBRE_LOCAL = @NOMBRE_LOCAL
          AND FECHA_TURNO_MES = @FECHA_TURNO_MES
          AND ESTADO = 'ACTIVO'
          AND DESCRIPCION_PRODUCTO NOT LIKE '%GAS LICUADO%'
          AND DESCRIPCION_PRODUCTO NOT LIKE '%GLP%'
          AND CAST(FECHA_EMISION AS TIME) <> '00:00:00'
    ),

    -- 3. Obtenemos el orden final de las filas por producto/día basándonos en cuál apareció primero en el tiempo
    OrdenPreciosReal AS (
        SELECT 
            FECHA_TURNO,
            DESCRIPCION_PRODUCTO,
            PRECIO_LISTA,
            ROW_NUMBER() OVER (
                PARTITION BY FECHA_TURNO, DESCRIPCION_PRODUCTO 
                ORDER BY MIN(SecuenciaTemporal) ASC
            ) AS ID_FILA
        FROM VentasConHora
        GROUP BY FECHA_TURNO, DESCRIPCION_PRODUCTO, PRECIO_LISTA
    ),

    -- 4. Cruzamos las sumas acumuladas completas con la fila cronológica calculada
    DatosConsolidados AS (
        SELECT 
            C.NOMBRE_LOCAL,
            C.FECHA_TURNO,
            C.DESCRIPCION_PRODUCTO,
            C.PRECIO_LISTA,
            C.CANTIDAD_TOTAL,
            -- Resguardo si un precio se cargó únicamente con hora 00:00:00
            ISNULL(O.ID_FILA, ROW_NUMBER() OVER (PARTITION BY C.FECHA_TURNO, C.DESCRIPCION_PRODUCTO ORDER BY C.PRECIO_LISTA ASC)) AS ID_FILA
        FROM CantidadesTotales C
        LEFT JOIN OrdenPreciosReal O 
            ON C.FECHA_TURNO = O.FECHA_TURNO 
            AND C.DESCRIPCION_PRODUCTO = O.DESCRIPCION_PRODUCTO 
            AND C.PRECIO_LISTA = O.PRECIO_LISTA
    ),

    -- 5. Universo de filas limpio para expandir la matriz horizontal
    UniversoFilas AS (
        SELECT DISTINCT NOMBRE_LOCAL, FECHA_TURNO, ID_FILA 
        FROM DatosConsolidados
    )

    -- 6. Pivot condicional final formateado estrictamente a 2 decimales
    SELECT 
        U.NOMBRE_LOCAL,
        U.FECHA_TURNO,
        
        -- DIESEL DB5
        CAST(ISNULL(MAX(CASE WHEN D.DESCRIPCION_PRODUCTO LIKE '%DIESEL DB5%' THEN D.CANTIDAD_TOTAL END), 0.00) AS DECIMAL(18,2)) AS [DIESEL_DB5_CANTIDAD_TOTAL],
        CAST(ISNULL(MAX(CASE WHEN D.DESCRIPCION_PRODUCTO LIKE '%DIESEL DB5%' THEN D.PRECIO_LISTA END), 0.00) AS DECIMAL(18,2)) AS [DIESEL_DB5_PRECIO_LISTA],
        
        -- GASOHOL REGULAR
        CAST(ISNULL(MAX(CASE WHEN D.DESCRIPCION_PRODUCTO LIKE '%REGULAR%' THEN D.CANTIDAD_TOTAL END), 0.00) AS DECIMAL(18,2)) AS [GASOHOL_REGULAR_CANTIDAD_TOTAL],
        CAST(ISNULL(MAX(CASE WHEN D.DESCRIPCION_PRODUCTO LIKE '%REGULAR%' THEN D.PRECIO_LISTA END), 0.00) AS DECIMAL(18,2)) AS [GASOHOL_REGULAR_PRECIO_LISTA],

        -- GASOHOL PREMIUM
        CAST(ISNULL(MAX(CASE WHEN D.DESCRIPCION_PRODUCTO LIKE '%PREMIUM%' THEN D.CANTIDAD_TOTAL END), 0.00) AS DECIMAL(18,2)) AS [GASOHOL_PREMIUM_CANTIDAD_TOTAL],
        CAST(ISNULL(MAX(CASE WHEN D.DESCRIPCION_PRODUCTO LIKE '%PREMIUM%' THEN D.PRECIO_LISTA END), 0.00) AS DECIMAL(18,2)) AS [GASOHOL_PREMIUM_PRECIO_LISTA]

    FROM UniversoFilas U
    LEFT JOIN DatosConsolidados D 
        ON U.FECHA_TURNO = D.FECHA_TURNO 
        AND U.ID_FILA = D.ID_FILA
    GROUP BY 
        U.NOMBRE_LOCAL,
        U.FECHA_TURNO,
        U.ID_FILA
    ORDER BY 
        U.FECHA_TURNO ASC, 
        U.ID_FILA ASC;
END;
