using OfficeOpenXml;
using PETRO_BOT.Models;
using PETRO_BOT.Models.Configuracion;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PETRO_BOT.Services.Services
{
    public class ProcesadorDescuentosExcelService
    {
        public class ResultadoProceso
        {
            public int GrifosProcesados { get; set; }
            public int DiasProcesadosExito { get; set; }
            public int DiasProcesadosError { get; set; }
            public List<string> MensajesError { get; set; } = new List<string>();
            public string ArchivoResultado { get; set; } = "";
            public TimeSpan TiempoEjecucion { get; set; }
        }

        public class DescuentoData
        {
            public DateTime Fecha { get; set; }
            public decimal TarjetaLiquidos { get; set; }
            public decimal TarjetaGLP { get; set; }
            public decimal DescLiquidos { get; set; }
            public decimal DescGLP { get; set; }
        }

        private static bool TryParseDecimal(object? valor, out decimal parsed)
        {
            parsed = 0m;
            if (valor == null) return false;
            if (valor is decimal dec) { parsed = dec; return true; }
            if (valor is double dbl) { parsed = (decimal)dbl; return true; }
            if (valor is int integer) { parsed = (decimal)integer; return true; }

            string str = valor.ToString() ?? "";
            str = str.Replace(",", ".");
            return decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed);
        }

        public async Task<ResultadoProceso> ProcesarDescuentosAsync(
            string rutaExcelOrigen, 
            string rutaExcelDestino, 
            List<int> grifoIds, 
            List<DateTime> fechasAProcesar)
        {
            var resultado = new ResultadoProceso();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var grifosDB = ConfiguracionService.ObtenerGrifosDB().Where(g => grifoIds.Contains(g.Id)).ToList();
            
            if (grifosDB.Count == 0 || fechasAProcesar.Count == 0)
            {
                resultado.MensajesError.Add("No hay grifos o fechas seleccionadas para procesar.");
                return resultado;
            }

            ExcelPackage.License.SetNonCommercialPersonal("PETROBOT");

            try
            {
                using var streamOrigen = new FileStream(rutaExcelOrigen, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var packageOrigen = new ExcelPackage(streamOrigen);

                string carpetaSalida = Path.Combine(Path.GetDirectoryName(rutaExcelDestino) ?? "", "Output");
                if (!Directory.Exists(carpetaSalida)) Directory.CreateDirectory(carpetaSalida);
                string archivoSalida = Path.Combine(carpetaSalida, $"DescuentosProcesados_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                
                File.Copy(rutaExcelDestino, archivoSalida, true);

                using var streamDestino = new FileStream(archivoSalida, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using var packageDestino = new ExcelPackage(streamDestino);

                foreach (var grifo in grifosDB)
                {
                    var lecturaConfig = grifo.RegistroVentasWrite;
                    var escrituraConfig = grifo.RegistroDescuentosWrite;

                    if (lecturaConfig == null || escrituraConfig == null)
                    {
                        resultado.MensajesError.Add($"Grifo '{grifo.Nombre}': Configuración incompleta.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(lecturaConfig.NombreHoja) || string.IsNullOrWhiteSpace(escrituraConfig.NombreHoja))
                    {
                        resultado.MensajesError.Add($"Grifo '{grifo.Nombre}': Faltan nombres de hojas en la configuración.");
                        continue;
                    }

                    var hojaOrigen = packageOrigen.Workbook.Worksheets.FirstOrDefault(h => h.Name.Equals(lecturaConfig.NombreHoja, StringComparison.OrdinalIgnoreCase));
                    if (hojaOrigen == null)
                    {
                        resultado.MensajesError.Add($"Grifo '{grifo.Nombre}': No se encontró la hoja origen '{lecturaConfig.NombreHoja}'.");
                        continue;
                    }

                    var hojaDestino = packageDestino.Workbook.Worksheets.FirstOrDefault(h => h.Name.Equals(escrituraConfig.NombreHoja, StringComparison.OrdinalIgnoreCase));
                    if (hojaDestino == null)
                    {
                        resultado.MensajesError.Add($"Grifo '{grifo.Nombre}': No se encontró la hoja destino '{escrituraConfig.NombreHoja}'.");
                        continue;
                    }

                    var mapaFechasOrigen = MapearFechasHoja(hojaOrigen, 2); // Origin date is always in column B (2)

                    int colFechaDestino = ConfiguracionService.GetExcelColumnIndex(escrituraConfig.ColumnaFecha ?? "");
                    if (colFechaDestino < 0)
                    {
                        resultado.MensajesError.Add($"Grifo '{grifo.Nombre}': Columna Fecha Destino no configurada.");
                        continue;
                    }
                    colFechaDestino += 1; // Convertir de base 0 (GetExcelColumnIndex) a base 1 (EPPlus)

                    var mapaFechasDestino = MapearFechasHoja(hojaDestino, colFechaDestino);

                    int diasProcesados = 0;

                    foreach (var fecha in fechasAProcesar)
                    {
                        string fechaStr = fecha.ToString("d/MM/yyyy");
                        string fechaStrCorto = fecha.ToString("dd/MM/yyyy");
                        string fechaBuscar = mapaFechasOrigen.Keys.FirstOrDefault(k => k == fechaStr || k == fechaStrCorto || k.Contains(fecha.ToString("d/MM"))) ?? "";

                        if (string.IsNullOrEmpty(fechaBuscar) || !mapaFechasOrigen.TryGetValue(fechaBuscar, out int filaOrigen))
                        {
                            resultado.MensajesError.Add($"Grifo '{grifo.Nombre}': El día {fecha:dd/MM/yyyy} no tiene información en el origen.");
                            resultado.DiasProcesadosError++;
                            continue;
                        }

                        var data = LeerValoresOrigen(hojaOrigen, filaOrigen, lecturaConfig);
                        
                        if (!data.TieneDatos)
                        {
                            resultado.MensajesError.Add($"Grifo '{grifo.Nombre}': El día {fecha:dd/MM/yyyy} existe en origen pero no tiene datos para escribir.");
                            resultado.DiasProcesadosError++;
                            continue;
                        }

                        string fechaDestBuscar = mapaFechasDestino.Keys.FirstOrDefault(k => k == fechaStr || k == fechaStrCorto || k.Contains(fecha.ToString("d/MM"))) ?? "";
                        if (string.IsNullOrEmpty(fechaDestBuscar) || !mapaFechasDestino.TryGetValue(fechaDestBuscar, out int filaDestino))
                        {
                            resultado.MensajesError.Add($"Grifo '{grifo.Nombre}': Día {fecha:dd/MM/yyyy} no encontrado para escribir en destino.");
                            resultado.DiasProcesadosError++;
                            continue;
                        }

                        EscribirValoresDestino(hojaDestino, filaDestino, data, escrituraConfig);
                        resultado.DiasProcesadosExito++;
                        diasProcesados++;
                    }

                    if (diasProcesados > 0)
                    {
                        resultado.GrifosProcesados++;
                    }
                }

                await packageDestino.SaveAsync();
                resultado.ArchivoResultado = archivoSalida;
            }
            catch (Exception ex)
            {
                resultado.MensajesError.Add($"Error crítico durante el procesamiento: {ex.Message}");
            }

            timer.Stop();
            resultado.TiempoEjecucion = timer.Elapsed;
            return resultado;
        }

        private (bool TieneDatos, decimal TarjetaLiquidos, decimal TarjetaGLP, decimal DescLiquidos, decimal DescGLP) LeerValoresOrigen(ExcelWorksheet hoja, int fila, RegistroVentasWriteConfig config)
        {
            decimal tLiq = LeerCelda(hoja, fila, config.Total_Tarjeta_de_Credito_Liquidos);
            decimal tGlp = LeerCelda(hoja, fila, config.Total_Tarjeta_de_Credito_GLP);
            decimal dLiq = LeerCelda(hoja, fila, config.DescuentoLiquidos);
            decimal dGlp = LeerCelda(hoja, fila, config.DescuentoGLP);

            bool tieneDatos = (tLiq != 0 || tGlp != 0 || dLiq != 0 || dGlp != 0);
            return (tieneDatos, tLiq, tGlp, dLiq, dGlp);
        }

        private void EscribirValoresDestino(ExcelWorksheet hoja, int fila, (bool TieneDatos, decimal TarjetaLiquidos, decimal TarjetaGLP, decimal DescLiquidos, decimal DescGLP) data, RegistroDescuentosWriteConfig config)
        {
            EscribirCelda(hoja, fila, config.TarjetaLiquidos, data.TarjetaLiquidos);
            EscribirCelda(hoja, fila, config.TarjetaGLP, data.TarjetaGLP);
            EscribirCelda(hoja, fila, config.DescLiquidos, data.DescLiquidos);
            EscribirCelda(hoja, fila, config.DescGLP, data.DescGLP);
        }

        private decimal LeerCelda(ExcelWorksheet hoja, int fila, string? colName)
        {
            if (string.IsNullOrWhiteSpace(colName)) return 0;
            var val = hoja.Cells[$"{colName}{fila}"].Value;
            if (TryParseDecimal(val, out decimal result)) return result;
            return 0;
        }

        private void EscribirCelda(ExcelWorksheet hoja, int fila, string? colName, decimal valor)
        {
            if (string.IsNullOrWhiteSpace(colName)) return;
            if (valor == 0) return; // Optional: Only write non-zero
            hoja.Cells[$"{colName}{fila}"].Value = valor;
        }

        private Dictionary<string, int> MapearFechasHoja(ExcelWorksheet hoja, int columnaIndex)
        {
            var mapa = new Dictionary<string, int>();
            int dimRow = hoja.Dimension?.End.Row ?? 0;
            int maxRow = Math.Max(dimRow, 400); // Buscar al menos hasta la fila 400
            maxRow = Math.Min(maxRow, 2000); // Límite de seguridad
            
            for (int r = 1; r <= maxRow; r++)
            {
                var valRaw = hoja.Cells[r, columnaIndex].Value;
                if (valRaw == null) continue;

                string valStr = valRaw.ToString()?.Trim() ?? "";
                if (valRaw is DateTime dt)
                {
                    valStr = dt.ToString("d/MM/yyyy");
                }
                else if (valRaw is double dbl)
                {
                    try { valStr = DateTime.FromOADate(dbl).ToString("d/MM/yyyy"); } catch { }
                }
                
                valStr = valStr.Replace(" 00:00:00", "").Replace(" 12:00:00 a. m.", "").Replace(" 12:00:00 a. m.", "").Trim();
                
                if (!string.IsNullOrEmpty(valStr) && !mapa.ContainsKey(valStr))
                {
                    mapa[valStr] = r;
                }
            }
            return mapa;
        }
    }
}
