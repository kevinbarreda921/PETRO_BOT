using ExcelDataReader;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PETRO_BOT.Models.ParteDiario;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PETRO_BOT.Services.Services
{
    public class ParteDiarioTotalService
    {
        private readonly string _connectionString;

        public ParteDiarioTotalService(IConfiguration configuration)
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

                string sqlConfig = @"
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
                END";
                using (var cmdConfig = new SqlCommand(sqlConfig, conn))
                {
                    await cmdConfig.ExecuteNonQueryAsync();
                }

                string sqlData = @"
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
                END";
                using (var cmdData = new SqlCommand(sqlData, conn))
                {
                    await cmdData.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error asegurando tablas ParteDiarioTotal: {ex.Message}");
            }
        }

        public async Task<List<ConfiguracionParteDiarioTotalModel>> ObtenerConfiguracionesAsync()
        {
            await AsegurarTablasCreadasAsync();
            var lista = new List<ConfiguracionParteDiarioTotalModel>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT ID, NOMBRE_GRIFO, PLANTILLA, CELDA_FECHA, CELDA_EESS, PALABRA_CLAVE_EESS, CELDA_TOTAL_DB5, CELDA_TOTAL_GLP, CELDA_TOTAL_GASOHOL_PREMIUM, CELDA_TOTAL_GASOHOL_REGULAR, FECHA_ACTUALIZACION FROM [PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL] ORDER BY NOMBRE_GRIFO";
            using var cmd = new SqlCommand(sql, conn);
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var item = new ConfiguracionParteDiarioTotalModel
                {
                    Id = rdr.GetInt32(0),
                    NombreGrifo = rdr.GetString(1),
                    Plantilla = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                    CeldaFecha = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                    CeldaEess = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                    PalabraClaveEess = rdr.IsDBNull(5) ? "" : rdr.GetString(5),
                    CeldaTotalDb5 = rdr.IsDBNull(6) ? "" : rdr.GetString(6),
                    CeldaTotalGlp = rdr.IsDBNull(7) ? "" : rdr.GetString(7),
                    CeldaTotalGasoholPremium = rdr.IsDBNull(8) ? "" : rdr.GetString(8),
                    CeldaTotalGasoholRegular = rdr.IsDBNull(9) ? "" : rdr.GetString(9),
                    FechaActualizacion = rdr.GetDateTime(10)
                };
                AsignarCoordenadasPorCelda(item);
                lista.Add(item);
            }
            return lista;
        }

        public async Task GuardarConfiguracionAsync(ConfiguracionParteDiarioTotalModel config)
        {
            await AsegurarTablasCreadasAsync();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            if (config.Id > 0)
            {
                string sqlUpdate = @"
                    UPDATE [PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL]
                    SET NOMBRE_GRIFO = @NombreGrifo,
                        PLANTILLA = @Plantilla,
                        CELDA_FECHA = @CeldaFecha,
                        CELDA_EESS = @CeldaEess,
                        PALABRA_CLAVE_EESS = @PalabraClaveEess,
                        CELDA_TOTAL_DB5 = @CeldaTotalDb5,
                        CELDA_TOTAL_GLP = @CeldaTotalGlp,
                        CELDA_TOTAL_GASOHOL_PREMIUM = @CeldaTotalGasoholPremium,
                        CELDA_TOTAL_GASOHOL_REGULAR = @CeldaTotalGasoholRegular,
                        FECHA_ACTUALIZACION = GETDATE()
                    WHERE ID = @Id";
                using var cmd = new SqlCommand(sqlUpdate, conn);
                cmd.Parameters.AddWithValue("@NombreGrifo", config.NombreGrifo.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@Plantilla", (object?)config.Plantilla ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaFecha", (object?)config.CeldaFecha ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaEess", (object?)config.CeldaEess ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PalabraClaveEess", (object?)config.PalabraClaveEess ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaTotalDb5", (object?)config.CeldaTotalDb5 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaTotalGlp", (object?)config.CeldaTotalGlp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaTotalGasoholPremium", (object?)config.CeldaTotalGasoholPremium ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaTotalGasoholRegular", (object?)config.CeldaTotalGasoholRegular ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", config.Id);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                string sqlInsert = @"
                    INSERT INTO [PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL]
                    (NOMBRE_GRIFO, PLANTILLA, CELDA_FECHA, CELDA_EESS, PALABRA_CLAVE_EESS, CELDA_TOTAL_DB5, CELDA_TOTAL_GLP, CELDA_TOTAL_GASOHOL_PREMIUM, CELDA_TOTAL_GASOHOL_REGULAR, FECHA_ACTUALIZACION)
                    VALUES
                    (@NombreGrifo, @Plantilla, @CeldaFecha, @CeldaEess, @PalabraClaveEess, @CeldaTotalDb5, @CeldaTotalGlp, @CeldaTotalGasoholPremium, @CeldaTotalGasoholRegular, GETDATE());
                    SELECT SCOPE_IDENTITY();";
                using var cmd = new SqlCommand(sqlInsert, conn);
                cmd.Parameters.AddWithValue("@NombreGrifo", config.NombreGrifo.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@Plantilla", (object?)config.Plantilla ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaFecha", (object?)config.CeldaFecha ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaEess", (object?)config.CeldaEess ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PalabraClaveEess", (object?)config.PalabraClaveEess ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaTotalDb5", (object?)config.CeldaTotalDb5 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaTotalGlp", (object?)config.CeldaTotalGlp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaTotalGasoholPremium", (object?)config.CeldaTotalGasoholPremium ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CeldaTotalGasoholRegular", (object?)config.CeldaTotalGasoholRegular ?? DBNull.Value);
                var newId = await cmd.ExecuteScalarAsync();
                if (newId != null) config.Id = Convert.ToInt32(newId);
            }
        }

        public async Task EliminarConfiguracionDatosAsync(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string sql = @"
                UPDATE [PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL]
                SET CELDA_FECHA = NULL, CELDA_EESS = NULL, PALABRA_CLAVE_EESS = NULL,
                    CELDA_TOTAL_DB5 = NULL, CELDA_TOTAL_GLP = NULL, CELDA_TOTAL_GASOHOL_PREMIUM = NULL, CELDA_TOTAL_GASOHOL_REGULAR = NULL,
                    FECHA_ACTUALIZACION = GETDATE()
                WHERE ID = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EliminarGrifoAsync(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string sql = "DELETE FROM [PETRO].[CONFIGURACION_PARTE_DIARIO_TOTAL] WHERE ID = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static void AsignarCoordenadasPorCelda(ConfiguracionParteDiarioTotalModel model)
        {
            (model.ColumnaFecha, model.FilaFecha) = ConvertirCeldaACoordenadas(model.CeldaFecha);
            (model.ColumnaEess, model.FilaEess) = ConvertirCeldaACoordenadas(model.CeldaEess);
            (model.ColumnaTotalDb5, model.FilaTotalDb5) = ConvertirCeldaACoordenadas(model.CeldaTotalDb5);
            (model.ColumnaTotalGlp, model.FilaTotalGlp) = ConvertirCeldaACoordenadas(model.CeldaTotalGlp);
            (model.ColumnaTotalGasoholPremium, model.FilaTotalGasoholPremium) = ConvertirCeldaACoordenadas(model.CeldaTotalGasoholPremium);
            (model.ColumnaTotalGasoholRegular, model.FilaTotalGasoholRegular) = ConvertirCeldaACoordenadas(model.CeldaTotalGasoholRegular);
        }

        public static (int colIndex, int rowNumber) ConvertirCeldaACoordenadas(string? celda)
        {
            if (string.IsNullOrWhiteSpace(celda)) return (-1, -1);
            celda = celda.Trim().ToUpper();
            var match = Regex.Match(celda, @"^([A-Z]+)(\d+)$");
            if (!match.Success) return (-1, -1);

            string colLetters = match.Groups[1].Value;
            int rowNumber = int.Parse(match.Groups[2].Value);

            int colIndex = 0;
            for (int i = 0; i < colLetters.Length; i++)
            {
                colIndex = colIndex * 26 + (colLetters[i] - 'A' + 1);
            }
            return (colIndex - 1, rowNumber); // colIndex 0-based, rowNumber 1-based
        }

        public static string ObtenerNombreCelda(int colIndex, int rowNumber)
        {
            if (colIndex < 0 || rowNumber <= 0) return "";
            string colLetter = "";
            int col = colIndex + 1;
            while (col > 0)
            {
                int modulo = (col - 1) % 26;
                colLetter = Convert.ToChar('A' + modulo) + colLetter;
                col = (col - modulo) / 26;
            }
            return $"{colLetter}{rowNumber}";
        }

        public async Task<(List<ArchivoProcesadoParteDiarioLog> Logs, int Insertados)> ProcesarArchivosMasivoAsync(List<string> rutasArchivos)
        {
            var logs = new List<ArchivoProcesadoParteDiarioLog>();
            int totalInsertados = 0;

            var configuraciones = await ObtenerConfiguracionesAsync();
            if (configuraciones.Count == 0)
            {
                logs.Add(new ArchivoProcesadoParteDiarioLog
                {
                    NombreArchivo = "N/A",
                    Estado = "Error",
                    Mensaje = "No existen configuraciones guardadas en PETRO.CONFIGURACION_PARTE_DIARIO_TOTAL."
                });
                return (logs, 0);
            }

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var ruta in rutasArchivos)
            {
                string nombreArchivo = Path.GetFileName(ruta);
                if (!File.Exists(ruta))
                {
                    logs.Add(new ArchivoProcesadoParteDiarioLog { NombreArchivo = nombreArchivo, Estado = "Error", Mensaje = "Archivo no encontrado" });
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

                        // Buscar qué configuración hace match con la hoja
                        ConfiguracionParteDiarioTotalModel? configCoincidente = null;
                        foreach (var config in configuraciones)
                        {
                            if (string.IsNullOrWhiteSpace(config.PalabraClaveEess) || config.ColumnaEess < 0 || config.FilaEess <= 0)
                                continue;

                            int rowIndex = config.FilaEess - 1;
                            int colIndex = config.ColumnaEess;

                            if (rowIndex >= 0 && rowIndex < filas.Count && colIndex >= 0 && colIndex < filas[rowIndex].Count)
                            {
                                var valCelda = filas[rowIndex][colIndex]?.ToString()?.Trim() ?? "";
                                if (string.Equals(valCelda, config.PalabraClaveEess.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                    valCelda.Contains(config.PalabraClaveEess.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    configCoincidente = config;
                                    break;
                                }
                            }
                        }

                        if (configCoincidente == null)
                        {
                            logs.Add(new ArchivoProcesadoParteDiarioLog
                            {
                                NombreArchivo = nombreArchivo,
                                NombreHoja = nombreHoja,
                                Estado = "SinConfiguracion",
                                Mensaje = "No se encontró configuración compatible según Input 3 / Palabra Clave EESS"
                            });
                            continue;
                        }

                        // Extraer valores con la configuración coincidente
                        DateTime? fechaValor = ExtraerFecha(filas, configCoincidente.ColumnaFecha, configCoincidente.FilaFecha - 1);
                        decimal totalDb5 = ExtraerDecimal(filas, configCoincidente.ColumnaTotalDb5, configCoincidente.FilaTotalDb5 - 1);
                        decimal totalGlp = ExtraerDecimal(filas, configCoincidente.ColumnaTotalGlp, configCoincidente.FilaTotalGlp - 1);
                        decimal totalPrem = ExtraerDecimal(filas, configCoincidente.ColumnaTotalGasoholPremium, configCoincidente.FilaTotalGasoholPremium - 1);
                        decimal totalReg = ExtraerDecimal(filas, configCoincidente.ColumnaTotalGasoholRegular, configCoincidente.FilaTotalGasoholRegular - 1);

                        // Insertar en PETRO.PARTE_DIARIO_TOTAL
                        string sqlInsert = @"
                            INSERT INTO [PETRO].[PARTE_DIARIO_TOTAL]
                            (NOMBRE_GRIFO, FECHA, TOTAL_SALIDA_DB5, TOTAL_SALIDA_GLP, TOTAL_SALIDA_GASOHOL_PREMIUM, TOTAL_SALIDA_GASOHOL_REGULAR, ARCHIVO_ORIGEN, NOMBRE_HOJA, FECHA_REGISTRO)
                            VALUES
                            (@Grifo, @Fecha, @Db5, @Glp, @Prem, @Reg, @Archivo, @Hoja, GETDATE())";

                        using (var cmd = new SqlCommand(sqlInsert, conn))
                        {
                            cmd.Parameters.AddWithValue("@Grifo", configCoincidente.NombreGrifo);
                            cmd.Parameters.AddWithValue("@Fecha", (object?)fechaValor ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Db5", totalDb5);
                            cmd.Parameters.AddWithValue("@Glp", totalGlp);
                            cmd.Parameters.AddWithValue("@Prem", totalPrem);
                            cmd.Parameters.AddWithValue("@Reg", totalReg);
                            cmd.Parameters.AddWithValue("@Archivo", nombreArchivo);
                            cmd.Parameters.AddWithValue("@Hoja", nombreHoja);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        totalInsertados++;
                        logs.Add(new ArchivoProcesadoParteDiarioLog
                        {
                            NombreArchivo = nombreArchivo,
                            NombreHoja = nombreHoja,
                            Estado = "Correcto",
                            GrifoDetectado = configCoincidente.NombreGrifo,
                            FechaLectura = fechaValor,
                            Mensaje = $"Procesado OK. DB5: {totalDb5:N2}, GLP: {totalGlp:N2}, Prem: {totalPrem:N2}, Reg: {totalReg:N2}"
                        });

                    } while (reader.NextResult());
                }
                catch (Exception ex)
                {
                    logs.Add(new ArchivoProcesadoParteDiarioLog
                    {
                        NombreArchivo = nombreArchivo,
                        Estado = "Error",
                        Mensaje = $"Error procesando archivo: {ex.Message}"
                    });
                }
            }

            return (logs, totalInsertados);
        }

        private static DateTime? ExtraerFecha(List<List<object?>> filas, int col, int row)
        {
            if (row < 0 || row >= filas.Count || col < 0 || col >= filas[row].Count) return null;
            var obj = filas[row][col];
            if (obj == null || obj == DBNull.Value) return null;
            if (obj is DateTime d) return d;
            var str = obj.ToString()?.Trim();
            if (string.IsNullOrEmpty(str)) return null;
            if (double.TryParse(str, out double serial))
            {
                try { return DateTime.FromOADate(serial); } catch { }
            }
            if (DateTime.TryParse(str, new CultureInfo("es-PE"), DateTimeStyles.None, out DateTime parsed))
                return parsed;
            if (DateTime.TryParse(str, out parsed))
                return parsed;
            return null;
        }

        private static decimal ExtraerDecimal(List<List<object?>> filas, int col, int row)
        {
            if (row < 0 || row >= filas.Count || col < 0 || col >= filas[row].Count) return 0m;
            var obj = filas[row][col];
            if (obj == null || obj == DBNull.Value) return 0m;
            if (obj is decimal d) return d;
            if (obj is double db) return (decimal)db;
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
