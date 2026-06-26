using ExcelDataReader;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PETRO_BOT.Models.ValidacionPrecio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PETRO_BOT.Services.Services
{
    public class ValidacionPrecioService
    {
        private readonly string _connectionString;

        public ValidacionPrecioService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SqlPetroBot") 
                ?? "Data Source=DESKTOP-OL0ABFN;Initial Catalog=BD_PETROBOT;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;Packet Size=4096;Application Name=\"SQL Server Management Studio\";Command Timeout=0";
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task<(bool Exito, string Mensaje, int RegistrosInsertados, long DemoraMs)> ProcesarArchivoVentaAsync(string rutaArchivo, string archivoOrigenNombre)
        {
            var sw = Stopwatch.StartNew();
            if (!File.Exists(rutaArchivo))
            {
                return (false, "El archivo no existe en la ruta especificada.", 0, 0);
            }

            try
            {
                using var stream = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = ExcelReaderFactory.CreateReader(stream);

                // 1. Leer fila 1 (índice 0)
                if (!reader.Read())
                {
                    return (false, "El archivo Excel está vacío.", 0, sw.ElapsedMilliseconds);
                }

                // 2. Leer fila 2 (índice 1) para validar título
                if (!reader.Read())
                {
                    return (false, "El archivo Excel no tiene suficientes filas.", 0, sw.ElapsedMilliseconds);
                }

                string fila2Texto = "";
                for (int c = 0; c < reader.FieldCount; c++)
                {
                    var val = reader.GetValue(c)?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        fila2Texto = val;
                        break;
                    }
                }

                if (!fila2Texto.StartsWith("REPORTE DE VENTAS DETALLADAS", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Archivo desconocido", 0, sw.ElapsedMilliseconds);
                }

                // Avanzar hasta la fila 7 (índice 6) donde están los títulos
                int filaActual = 2; // Ya leímos fila 2
                while (filaActual < 7 && reader.Read())
                {
                    filaActual++;
                }

                if (filaActual < 7)
                {
                    return (false, "No se encontró la fila 7 de encabezados en el Excel.", 0, sw.ElapsedMilliseconds);
                }

                // Mapeo dinámico de columnas de la fila 7
                var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int primerColumnaDatos = -1;
                for (int c = 0; c < reader.FieldCount; c++)
                {
                    string h = reader.GetValue(c)?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(h))
                    {
                        if (primerColumnaDatos == -1) primerColumnaDatos = c;
                        if (!colMap.ContainsKey(h))
                        {
                            colMap[h] = c;
                        }
                    }
                }

                if (primerColumnaDatos == -1) primerColumnaDatos = 0;

                // Crear DataTable con la estructura de [PETRO].[REPORTE_PRECIO_LISTA]
                var dt = new DataTable("REPORTE_PRECIO_LISTA");
                dt.Columns.Add("ARCHIVO_ORIGEN", typeof(string));
                dt.Columns.Add("FECHA_IMPORTACION", typeof(DateTime));
                dt.Columns.Add("CODIGO_LOCAL", typeof(string));
                dt.Columns.Add("NOMBRE_LOCAL", typeof(string));
                dt.Columns.Add("FECHA_TURNO", typeof(DateTime));
                dt.Columns.Add("FECHA_EMISION", typeof(DateTime));
                dt.Columns.Add("TIPO_DOCUMENTO", typeof(string));
                dt.Columns.Add("NUMERO_DOCUMENTO", typeof(string));
                dt.Columns.Add("CODIGO_CLIENTE", typeof(string));
                dt.Columns.Add("RAZON_SOCIAL", typeof(string));
                dt.Columns.Add("NRO_ITEM", typeof(int));
                dt.Columns.Add("CODIGO_PRODUCTO", typeof(string));
                dt.Columns.Add("SUB_CODIGO_PRODUCTO", typeof(string));
                dt.Columns.Add("DESCRIPCION_PRODUCTO", typeof(string));
                dt.Columns.Add("CANTIDAD", typeof(decimal));
                dt.Columns.Add("UNIDAD", typeof(string));
                dt.Columns.Add("PRECIO_UNITARIO_CON_IGV", typeof(decimal));
                dt.Columns.Add("PRECIO_LISTA", typeof(decimal));
                dt.Columns.Add("IGV_SOLES", typeof(decimal));
                dt.Columns.Add("IMPORTE_CON_IGV_SOLES", typeof(decimal));
                dt.Columns.Add("MTO_RECAUDO", typeof(decimal));
                dt.Columns.Add("MTO_DESCUENTO", typeof(decimal));
                dt.Columns.Add("ESTADO", typeof(string));
                dt.Columns.Add("FORMA_PAGO", typeof(string));

                DateTime fechaImportacion = DateTime.Now;

                // Leer fila 8 en adelante
                int registrosInsertados = 0;
                while (reader.Read())
                {
                    bool filaVacia = true;
                    for (int c = primerColumnaDatos; c < Math.Min(reader.FieldCount, primerColumnaDatos + 22); c++)
                    {
                        if (reader.GetValue(c) != null && !string.IsNullOrWhiteSpace(reader.GetValue(c).ToString()))
                        {
                            filaVacia = false;
                            break;
                        }
                    }
                    if (filaVacia) continue;

                    var row = dt.NewRow();
                    row["ARCHIVO_ORIGEN"] = archivoOrigenNombre;
                    row["FECHA_IMPORTACION"] = fechaImportacion;

                    row["CODIGO_LOCAL"] = GetStringVal(reader, colMap, "CODIGO DE LOCAL", primerColumnaDatos + 0);
                    row["NOMBRE_LOCAL"] = GetStringVal(reader, colMap, "NOMBRE DE LOCAL", primerColumnaDatos + 1);

                    var dtTurno = ParseDateTime(GetRawVal(reader, colMap, "FECHA TURNO", primerColumnaDatos + 2));
                    if (dtTurno.HasValue) row["FECHA_TURNO"] = dtTurno.Value.Date;
                    else row["FECHA_TURNO"] = DBNull.Value;

                    var dtEmision = ParseDateTime(GetRawVal(reader, colMap, "FECHA EMISION", primerColumnaDatos + 3));
                    if (dtEmision.HasValue) row["FECHA_EMISION"] = dtEmision.Value;
                    else row["FECHA_EMISION"] = DBNull.Value;

                    row["TIPO_DOCUMENTO"] = GetStringVal(reader, colMap, "TIPO DOCUMENTO", primerColumnaDatos + 4);
                    row["NUMERO_DOCUMENTO"] = GetStringVal(reader, colMap, "NUMERO DOCUMENTO", primerColumnaDatos + 5);
                    row["CODIGO_CLIENTE"] = GetStringVal(reader, colMap, "CODIGO DE CLIENTE", primerColumnaDatos + 6);
                    row["RAZON_SOCIAL"] = GetStringVal(reader, colMap, "RAZON SOCIAL O NOMBRE", primerColumnaDatos + 7);

                    var nroItemVal = ParseInt(GetRawVal(reader, colMap, "NRO. ITEM", primerColumnaDatos + 8));
                    if (nroItemVal.HasValue) row["NRO_ITEM"] = nroItemVal.Value;
                    else row["NRO_ITEM"] = DBNull.Value;

                    row["CODIGO_PRODUCTO"] = GetStringVal(reader, colMap, "CODIGO DE PRODUCTO", primerColumnaDatos + 9);
                    row["SUB_CODIGO_PRODUCTO"] = GetStringValPos(reader, primerColumnaDatos + 10);
                    row["DESCRIPCION_PRODUCTO"] = GetStringVal(reader, colMap, "DESCRIPCION DE PRODUCTO", primerColumnaDatos + 11);

                    var cant = ParseDecimal(GetRawVal(reader, colMap, "CANTIDAD", primerColumnaDatos + 12));
                    if (cant.HasValue) row["CANTIDAD"] = cant.Value; else row["CANTIDAD"] = DBNull.Value;

                    row["UNIDAD"] = GetStringVal(reader, colMap, "UNIDAD", primerColumnaDatos + 13);

                    var pUnit = ParseDecimal(GetRawVal(reader, colMap, "PRECIO UNITARIO CON IGV", primerColumnaDatos + 14));
                    if (pUnit.HasValue) row["PRECIO_UNITARIO_CON_IGV"] = pUnit.Value; else row["PRECIO_UNITARIO_CON_IGV"] = DBNull.Value;

                    var pLista = ParseDecimal(GetRawVal(reader, colMap, "PRECIO LISTA", primerColumnaDatos + 15));
                    if (pLista.HasValue) row["PRECIO_LISTA"] = pLista.Value; else row["PRECIO_LISTA"] = DBNull.Value;

                    var igv = ParseDecimal(GetRawVal(reader, colMap, "IGV (SOLES)", primerColumnaDatos + 16));
                    if (igv.HasValue) row["IGV_SOLES"] = igv.Value; else row["IGV_SOLES"] = DBNull.Value;

                    var impIgv = ParseDecimal(GetRawVal(reader, colMap, "IMPORTE CON IGV (SOLES)", primerColumnaDatos + 17));
                    if (impIgv.HasValue) row["IMPORTE_CON_IGV_SOLES"] = impIgv.Value; else row["IMPORTE_CON_IGV_SOLES"] = DBNull.Value;

                    var mtoRec = ParseDecimal(GetRawVal(reader, colMap, "MTO RECAUDO", primerColumnaDatos + 18));
                    if (mtoRec.HasValue) row["MTO_RECAUDO"] = mtoRec.Value; else row["MTO_RECAUDO"] = DBNull.Value;

                    var mtoDesc = ParseDecimal(GetRawVal(reader, colMap, "MTO DESCUENTO", primerColumnaDatos + 19));
                    if (mtoDesc.HasValue) row["MTO_DESCUENTO"] = mtoDesc.Value; else row["MTO_DESCUENTO"] = DBNull.Value;

                    row["ESTADO"] = GetStringVal(reader, colMap, "ESTADO", primerColumnaDatos + 20);
                    row["FORMA_PAGO"] = GetStringVal(reader, colMap, "FORMA DE PAGO", primerColumnaDatos + 21);

                    dt.Rows.Add(row);
                    registrosInsertados++;
                }

                if (dt.Rows.Count == 0)
                {
                    return (false, "El archivo no contenía registros de ventas en la fila 8 en adelante.", 0, sw.ElapsedMilliseconds);
                }

                // SqlBulkCopy a SQL Server
                using var sqlConnection = new SqlConnection(_connectionString);
                await sqlConnection.OpenAsync();

                using var bulkCopy = new SqlBulkCopy(sqlConnection)
                {
                    DestinationTableName = "PETRO.REPORTE_PRECIO_LISTA",
                    BatchSize = 10000,
                    BulkCopyTimeout = 300
                };

                foreach (DataColumn col in dt.Columns)
                {
                    bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                }

                await Task.Run(() => bulkCopy.WriteToServer(dt));

                sw.Stop();
                return (true, "Procesado correctamente", registrosInsertados, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, $"Error al procesar: {ex.Message}", 0, sw.ElapsedMilliseconds);
            }
        }

        private object? GetRawVal(IExcelDataReader reader, Dictionary<string, int> colMap, string headerName, int fallbackIdx)
        {
            if (colMap.TryGetValue(headerName, out int idx) && idx < reader.FieldCount)
                return reader.GetValue(idx);
            if (fallbackIdx >= 0 && fallbackIdx < reader.FieldCount)
                return reader.GetValue(fallbackIdx);
            return null;
        }

        private object? GetStringVal(IExcelDataReader reader, Dictionary<string, int> colMap, string headerName, int fallbackIdx)
        {
            var raw = GetRawVal(reader, colMap, headerName, fallbackIdx);
            if (raw == null || raw is DBNull) return DBNull.Value;
            string s = raw.ToString()?.Trim() ?? "";
            return string.IsNullOrEmpty(s) ? DBNull.Value : s;
        }

        private object? GetStringValPos(IExcelDataReader reader, int pos)
        {
            if (pos >= 0 && pos < reader.FieldCount)
            {
                var raw = reader.GetValue(pos);
                if (raw != null && !(raw is DBNull))
                {
                    string s = raw.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            return DBNull.Value;
        }

        private int? ParseInt(object? val)
        {
            if (val == null || val is DBNull) return null;
            if (val is int i) return i;
            if (val is double d) return (int)d;
            if (val is decimal dec) return (int)dec;
            if (int.TryParse(val.ToString()?.Trim(), out int res)) return res;
            return null;
        }

        private decimal? ParseDecimal(object? val)
        {
            if (val == null || val is DBNull) return null;
            if (val is decimal dec) return dec;
            if (val is double d) return (decimal)d;
            if (val is int i) return i;
            if (val is long l) return l;
            string s = val.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(s)) return null;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal res)) return res;
            if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("es-PE"), out res)) return res;
            return null;
        }

        private DateTime? ParseDateTime(object? val)
        {
            if (val == null || val is DBNull) return null;
            if (val is DateTime dt) return dt;
            if (val is double d)
            {
                try { return DateTime.FromOADate(d); } catch { }
            }
            string s = val.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(s)) return null;

            if (DateTime.TryParse(s, out DateTime dtParsed)) return dtParsed;

            string[] formats = new[] {
                "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy H:mm:ss", "dd/MM/yyyy hh:mm a", "dd/MM/yyyy h:mm a",
                "dd/MM/yy HH:mm:ss", "dd/MM/yy H:mm:ss", "dd/MM/yy hh:mm a", "dd/MM/yy h:mm a",
                "d/M/yyyy HH:mm:ss", "d/M/yyyy H:mm:ss", "d/M/yyyy hh:mm a", "d/M/yyyy h:mm a",
                "d/M/yy HH:mm:ss", "d/M/yy H:mm:ss", "d/M/yy hh:mm a", "d/M/yy h:mm a",
                "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy"
            };

            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtParsed))
                return dtParsed;

            return null;
        }
    }
}
