using ExcelDataReader;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PETRO_BOT.Models.PrecioCompra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PETRO_BOT.Services.Services
{
    public class ImportarPrecioCompraService
    {
        private readonly string _connectionString;

        public ImportarPrecioCompraService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlPetroBot") 
                ?? "Data Source=.\\SQLEXPRESS;Initial Catalog=BD_PETROBOT;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Application Name=\"SQL Server Management Studio\"";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task AsegurarTablasCreadasAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                string sqlSchema = @"
                IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'PETRO')
                BEGIN
                    EXEC('CREATE SCHEMA [PETRO]');
                END";
                using (var cmdSchema = new SqlCommand(sqlSchema, conn))
                {
                    await cmdSchema.ExecuteNonQueryAsync();
                }

                string sqlData = @"
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
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[PETRO].[PRECIO_COMPRA]') AND name = N'CANTIDAD_GALONES')
                    BEGIN
                        ALTER TABLE [PETRO].[PRECIO_COMPRA] ADD [CANTIDAD_GALONES] [decimal](18, 4) NULL;
                    END
                END";
                using (var cmdData = new SqlCommand(sqlData, conn))
                {
                    await cmdData.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error asegurando tabla PrecioCompra: {ex.Message}");
            }
        }

        public async Task<(List<ArchivoProcesadoPrecioCompraLog> Logs, int Insertados)> ProcesarArchivosMasivoAsync(List<string> rutasArchivos)
        {
            await AsegurarTablasCreadasAsync();
            var logs = new List<ArchivoProcesadoPrecioCompraLog>();
            int totalInsertados = 0;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var ruta in rutasArchivos)
            {
                string nombreArchivo = Path.GetFileName(ruta);
                if (!File.Exists(ruta))
                {
                    logs.Add(new ArchivoProcesadoPrecioCompraLog { NombreArchivo = nombreArchivo, Estado = "Error", Mensaje = "Archivo no encontrado" });
                    continue;
                }

                try
                {
                    using var stream = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = ExcelReaderFactory.CreateReader(stream);

                    do
                    {
                        string nombreHoja = reader.Name;
                        var filas = new List<List<object?>>();
                        while (reader.Read())
                        {
                            var fila = new List<object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                fila.Add(reader.GetValue(i));
                            }
                            filas.Add(fila);
                        }

                        // La data comienza desde la fila 2 (índice 1 en la lista)
                        if (filas.Count < 2)
                        {
                            logs.Add(new ArchivoProcesadoPrecioCompraLog
                            {
                                NombreArchivo = nombreArchivo,
                                NombreHoja = nombreHoja,
                                Estado = "Alerta",
                                Mensaje = "La hoja no contiene datos a partir de la fila 2."
                            });
                            continue;
                        }

                        int contadorFila = 0;
                        for (int r = 1; r < filas.Count; r++)
                        {
                            var fila = filas[r];
                            if (EsFilaVacia(fila)) continue;

                            string descripcion = ExtraerTexto(fila, 7);      // Columna H (índice 7)
                            decimal cantidadGalones = ExtraerDecimal(fila, 8); // Columna I (índice 8)
                            decimal precio = ExtraerDecimal(fila, 14);       // Columna O (índice 14)

                            if (string.IsNullOrWhiteSpace(descripcion) && cantidadGalones == 0m && precio == 0m) continue;

                            contadorFila++;

                            string sqlInsert = @"
                                INSERT INTO [PETRO].[PRECIO_COMPRA]
                                (GRIFO, FILA, DESCRIPCION_PRODUCTO, CANTIDAD_GALONES, PRECIO_GALON, ARCHIVO_ORIGEN, FECHA_REGISTRO)
                                VALUES
                                (@Grifo, @Fila, @Descripcion, @Cantidad, @Precio, @Archivo, GETDATE())";

                            using (var cmd = new SqlCommand(sqlInsert, conn))
                            {
                                cmd.Parameters.AddWithValue("@Grifo", (object?)nombreHoja ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Fila", contadorFila);
                                cmd.Parameters.AddWithValue("@Descripcion", (object?)descripcion ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Cantidad", cantidadGalones);
                                cmd.Parameters.AddWithValue("@Precio", precio);
                                cmd.Parameters.AddWithValue("@Archivo", (object?)nombreArchivo ?? DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            totalInsertados++;
                            logs.Add(new ArchivoProcesadoPrecioCompraLog
                            {
                                NombreArchivo = nombreArchivo,
                                NombreHoja = nombreHoja,
                                Fila = contadorFila,
                                Estado = "Correcto",
                                DescripcionProducto = descripcion,
                                CantidadGalones = cantidadGalones,
                                PrecioGalon = precio,
                                Mensaje = $"Procesado OK. Fila Excel {r + 1} -> #{contadorFila}: {descripcion} | Galones: {cantidadGalones:N4} | S/ {precio:N4}"
                            });
                        }
                    } while (reader.NextResult());
                }
                catch (Exception ex)
                {
                    logs.Add(new ArchivoProcesadoPrecioCompraLog
                    {
                        NombreArchivo = nombreArchivo,
                        Estado = "Error",
                        Mensaje = $"Error procesando archivo: {ex.Message}"
                    });
                }
            }

            return (logs, totalInsertados);
        }

        private static bool EsFilaVacia(List<object?> fila)
        {
            if (fila == null || fila.Count == 0) return true;
            return fila.All(c => c == null || c == DBNull.Value || string.IsNullOrWhiteSpace(c.ToString()));
        }

        private static string ExtraerTexto(List<object?> fila, int colIndex)
        {
            if (fila == null || colIndex < 0 || colIndex >= fila.Count) return "";
            var val = fila[colIndex];
            if (val == null || val == DBNull.Value) return "";
            return val.ToString()?.Trim() ?? "";
        }

        private static decimal ExtraerDecimal(List<object?> fila, int colIndex)
        {
            if (fila == null || colIndex < 0 || colIndex >= fila.Count) return 0m;
            var obj = fila[colIndex];
            if (obj == null || obj == DBNull.Value) return 0m;
            if (obj is decimal d) return d;
            if (obj is double db) return (decimal)db;
            if (obj is float f) return (decimal)f;
            if (obj is int i) return (decimal)i;
            if (obj is long l) return (decimal)l;
            var str = obj.ToString()?.Trim().Replace("S/", "").Replace("$", "").Replace(" ", "");
            if (string.IsNullOrEmpty(str)) return 0m;
            if (decimal.TryParse(str, NumberStyles.Any, new CultureInfo("es-PE"), out decimal res))
                return res;
            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out res))
                return res;
            return 0m;
        }
    }
}
