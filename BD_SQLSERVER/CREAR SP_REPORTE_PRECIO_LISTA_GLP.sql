USE [BD_PETROBOT]
GO
/****** Object:  StoredProcedure [PETRO].[SP_REPORTE_PRECIO_LISTA_GLP]    Script Date: 27/06/2026 10:34:57 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [PETRO].[SP_REPORTE_PRECIO_LISTA_GLP]
    @NOMBRE_LOCAL NVARCHAR(150),
    @FECHA_TURNO_MES VARCHAR(6)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Calculamos las sumas totales de cantidad por cada precio del día (Sin omitir nada)
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
          AND (DESCRIPCION_PRODUCTO LIKE '%GAS LICUADO%' OR DESCRIPCION_PRODUCTO LIKE '%GLP%')
        GROUP BY FECHA_TURNO, DESCRIPCION_PRODUCTO, PRECIO_LISTA
    ),

    -- 2. Identificamos el orden REAL de las ventas del día usando la hora de FECHA_EMISION (Excluyendo 00:00:00)
    VentasConHora AS (
        SELECT 
            FECHA_TURNO,
            DESCRIPCION_PRODUCTO,
            PRECIO_LISTA,
            FECHA_EMISION,
            -- Numeramos todas las transacciones del día por su hora exacta
            ROW_NUMBER() OVER (
                PARTITION BY FECHA_TURNO, DESCRIPCION_PRODUCTO 
                ORDER BY FECHA_EMISION ASC
            ) AS SecuenciaTemporal
        FROM [PETRO].[REPORTE_PRECIO_LISTA]
        WHERE NOMBRE_LOCAL = @NOMBRE_LOCAL
          AND FECHA_TURNO_MES = @FECHA_TURNO_MES
          AND ESTADO = 'ACTIVO'
          AND (DESCRIPCION_PRODUCTO LIKE '%GAS LICUADO%' OR DESCRIPCION_PRODUCTO LIKE '%GLP%')
          AND CAST(FECHA_EMISION AS TIME) <> '00:00:00'
    ),

    -- 3. Obtenemos el orden de los precios basados estrictamente en cuándo aparecieron por primera vez en el día
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

    -- 4. Cruzamos las cantidades totales con el orden cronológico real que hallamos
    DatosConsolidados AS (
        SELECT 
            C.NOMBRE_LOCAL,
            C.FECHA_TURNO,
            C.PRECIO_LISTA,
            C.CANTIDAD_TOTAL,
            -- Resguardo por si un precio solo existió con hora 00:00:00
            ISNULL(O.ID_FILA, ROW_NUMBER() OVER (PARTITION BY C.FECHA_TURNO, C.DESCRIPCION_PRODUCTO ORDER BY C.PRECIO_LISTA ASC)) AS ID_FILA
        FROM CantidadesTotales C
        LEFT JOIN OrdenPreciosReal O 
            ON C.FECHA_TURNO = O.FECHA_TURNO 
            AND C.DESCRIPCION_PRODUCTO = O.DESCRIPCION_PRODUCTO 
            AND C.PRECIO_LISTA = O.PRECIO_LISTA
    )

    -- 5. Resultado final limpio, ordenado cronológicamente por el primer precio que abrió el día
    SELECT 
        D.NOMBRE_LOCAL,
        D.FECHA_TURNO,
        CAST(D.CANTIDAD_TOTAL AS DECIMAL(18,2)) AS [GLP_CANTIDAD_TOTAL],
        CAST(D.PRECIO_LISTA AS DECIMAL(18,2)) AS [GLP_PRECIO_LISTA]
    FROM DatosConsolidados D
    ORDER BY 
        D.FECHA_TURNO ASC, 
        D.ID_FILA ASC;
END;
