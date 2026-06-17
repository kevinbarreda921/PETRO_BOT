using OfficeOpenXml;
using PETRO_BOT.Models;
using PETRO_BOT.Models.Configuracion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PETRO_BOT.Services.Services
{
    public class ConsolidadorVentasService
    {
        private readonly LectorExcelService _lector;
        private readonly EscritorExcelService _escritor;

        public ConsolidadorVentasService()
        {
            _lector = new LectorExcelService();
            _escritor = new EscritorExcelService();
        }

        public void Procesar(string tarea, string dia)
        {
            //var CONTADOR_dES = 1;
            Console.WriteLine("Iniciando lectura de archivos...");
            System.Collections.Concurrent.ConcurrentBag<ArchivoGrifo> listaGrifosProcesar;
            if (tarea == "Procesar Día" || tarea == "Procesar Hoy")
            {
                listaGrifosProcesar = _lector.LeerPartesDiarios_PorDia(dia);
            }
            else
            {
                listaGrifosProcesar = _lector.LeerPartesDiarios();
            }


            string rutaProyecto = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
            string carpetaRegistroVentas = Path.Combine(rutaProyecto, "wwwroot", "uploads", "RegistroVentas");

            var archivosExcel = Directory.GetFiles(carpetaRegistroVentas, "*.xlsx");
            if (archivosExcel.Length == 0)
            {
                LoggerService.Error("MAESTRO", "MAESTRO", "No se encontró ningún archivo Excel maestro en la carpeta Registro_ventas.");
                return;
            }
            string rutaExcel = archivosExcel[0];

            // Eliminamos la pasada inicial redundante de ExcelDataReader


            Console.WriteLine("Abriendo archivo Excel maestro con EPPlus...");
            ExcelPackage.License.SetNonCommercialPersonal("PETROBOT");
            using var package = new ExcelPackage(new FileInfo(rutaExcel));
            package.Compression = CompressionLevel.BestSpeed; // Reducir overhead de CPU al comprimir
            var workbook = package.Workbook;
            var grifosList = ConfiguracionService.ObtenerGrifosDB();

            foreach (var archivoGrifoActual in listaGrifosProcesar)
            {
                string? grifoObjetivo = archivoGrifoActual.Grifo;
                if (string.IsNullOrEmpty(grifoObjetivo)) continue;

                //LoggerService.Info(grifoObjetivo, archivoGrifoActual.Archivo ?? "", $" CONTADOR {CONTADOR_dES}");
                //CONTADOR_dES++;

                // Obtener configuración desde la base de datos
                var grifoDB = grifosList.FirstOrDefault(g => g.Nombre.Equals(grifoObjetivo, StringComparison.OrdinalIgnoreCase));
                if (grifoDB == null)
                {
                    LoggerService.Error(grifoObjetivo, "MAESTRO", $"El grifo {grifoObjetivo}, Estación: N/A del dia N/A: ERROR: No se encontró configuración en la base de datos para el grifo: {grifoObjetivo}");
                    continue;
                }

                // Buscamos la hoja usando el nombre configurado en RegistroVentasWrite, o por defecto con el nombre del grifo
                string? sheetName = grifoDB.RegistroVentasWrite?.NombreHoja;
                var hojaEPPlus = !string.IsNullOrWhiteSpace(sheetName) ? workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase)) : null;
                if (hojaEPPlus == null)
                {
                    hojaEPPlus = workbook.Worksheets.FirstOrDefault(w => w.Name.IndexOf(grifoObjetivo, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                
                if (hojaEPPlus == null)
                {
                    LoggerService.Error(grifoObjetivo, "MAESTRO", $"No se encontró ninguna hoja para el grifo {grifoObjetivo} en EPPlus.");
                    continue;
                }

                // Mapeamos las filas de fechas directamente desde memoria (sin leer el archivo de nuevo)
                var mapaFechasFilas = _escritor.MapearFechasHoja(hojaEPPlus);

                var fechasDelGrifo = archivoGrifoActual.ListVenta
                    .Where(v => !string.IsNullOrEmpty(v.Dia))
                    .Select(v => v.Dia!)
                    .Distinct()
                    .ToList();

                var configGrifo = grifoDB.Configuracion;
                var configWrite = grifoDB.RegistroVentasWrite ?? new RegistroVentasWriteConfig();

                // Invertir diccionario de clientes una sola vez por Grifo
                var clienteAColumna = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (grifoDB.ClientesCredito != null)
                {
                    foreach (var cli in grifoDB.ClientesCredito)
                    {
                        if (!string.IsNullOrWhiteSpace(cli.ClienteNombre) && !string.IsNullOrWhiteSpace(cli.Columna))
                        {
                            clienteAColumna[cli.ClienteNombre.Trim()] = cli.Columna.Trim();
                        }
                    }
                }

                // Construir el diccionario de columnas de escritura dinámicamente para pasarlo a EscribirFila
                var columnasEscritura = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                void AddCol(string colLetter, string propName)
                {
                    columnasEscritura[propName] = colLetter ?? "";
                }
                AddCol(configWrite.Venta_GPL, "Venta_GPL");
                AddCol(configWrite.Venta_GNV, "Venta_GNV");
                AddCol(configWrite.Total_venta_acumulada, "Total_venta_acumulada");
                AddCol(configWrite.Total_Tarjeta_de_Credito_Liquidos, "Total_Tarjeta_de_Credito_Liquidos");
                AddCol(configWrite.Total_Tarjeta_de_Credito_GLP, "Total_Tarjeta_de_Credito_GLP");
                AddCol(configWrite.Total_Tarjeta_de_Credito_GNV, "Total_Tarjeta_de_Credito_GNV");
                AddCol(configWrite.ErrorMaquina, "ErrorMaquina");
                AddCol(configWrite.Recaudo_Cofide_GNV, "Recaudo_Cofide_GNV");
                AddCol(configWrite.Gastos, "Gastos");
                AddCol(configWrite.Ventas_con_transferencia, "Ventas_con_transferencia");
                AddCol(configWrite.DescuentoLiquidos, "DescuentoLiquidos");
                AddCol(configWrite.DescuentoGLP, "DescuentoGLP");
                AddCol(configWrite.Hermes_monto_liquido, "Hermes_monto_liquido");
                AddCol(configWrite.Hermes_monto_GLP, "Hermes_monto_GLP");
                AddCol(configWrite.Hermes_monto_GNV1, "Hermes_monto_GNV1");
                AddCol(configWrite.Hermes_monto_GNV2, "Hermes_monto_GNV2");

                foreach (string fechaABuscar in fechasDelGrifo)
                {
                    if (mapaFechasFilas.TryGetValue(fechaABuscar, out int filaDestino))
                    {
                        var ventaParaEscribir = archivoGrifoActual.ListVenta.FirstOrDefault(v => v.Dia == fechaABuscar);
                        
                        // Validar si ya existe información en las columnas de escritura configuradas en la fila de destino
                        bool yaTieneData = false;
                        string columnaConData = "";
                        foreach (var colLetter in columnasEscritura.Values)
                        {
                            if (!string.IsNullOrWhiteSpace(colLetter))
                            {
                                var cellVal = hojaEPPlus.Cells[$"{colLetter}{filaDestino}"].Value;
                                if (cellVal != null)
                                {
                                    string valStr = cellVal.ToString()?.Trim() ?? "";
                                    if (!string.IsNullOrEmpty(valStr) && valStr != "0" && valStr != "0.00" && valStr != "0,00" && valStr != "0.0")
                                    {
                                        yaTieneData = true;
                                        columnaConData = colLetter;
                                        break;
                                    }
                                }
                            }
                        }

                        if (yaTieneData)
                        {
                            string estacion = ventaParaEscribir != null ? ventaParaEscribir.EESS ?? "N/A" : "N/A";
                            LoggerService.Error(grifoObjetivo, archivoGrifoActual.Archivo ?? "DESCONOCIDO", $"El grifo {grifoObjetivo}, Estación: {estacion} del dia {fechaABuscar}: ERROR: El día {fechaABuscar} ya tiene data en el Registro de Ventas (Celda {columnaConData}{filaDestino}). No se sobrescribió.");
                            continue;
                        }

                        if (ventaParaEscribir != null)
                        {
                            _escritor.EscribirFila(hojaEPPlus, ventaParaEscribir, filaDestino, columnasEscritura);
                            
                            if (clienteAColumna.Count > 0)
                            {
                                _escritor.EscribirClientesCredito(hojaEPPlus, ventaParaEscribir, filaDestino, clienteAColumna, grifoObjetivo, archivoGrifoActual.Archivo ?? "DESCONOCIDO");
                            }

                            string eessMsg = !string.IsNullOrWhiteSpace(ventaParaEscribir.EESS) ? $", Estación: {ventaParaEscribir.EESS}" : "";
                            LoggerService.Info(grifoObjetivo, archivoGrifoActual.Archivo ?? "DESCONOCIDO", $"El grifo {grifoObjetivo}{eessMsg} del dia {fechaABuscar} guardado con éxito");
                            
                            if (!string.IsNullOrWhiteSpace(ventaParaEscribir.EESS))
                            {
                                string eessLimpio = ventaParaEscribir.EESS.ToLower();
                                string grifoLimpio = grifoObjetivo.ToLower();
                                if (!eessLimpio.Contains(grifoLimpio) && !grifoLimpio.Contains(eessLimpio))
                                {
                                    LoggerService.Error(grifoObjetivo, archivoGrifoActual.Archivo ?? "DESCONOCIDO", $"El grifo {grifoObjetivo}, Estación: {ventaParaEscribir.EESS} del dia {fechaABuscar}: ADVERTENCIA: El nombre de la estación leído en el Parte Diario ('{ventaParaEscribir.EESS}') no coincide con el grifo configurado '{grifoObjetivo}'");
                                }
                            }
                        }
                    }
                    else
                    {
                        var vEscribir = archivoGrifoActual.ListVenta.FirstOrDefault(v => v.Dia == fechaABuscar);
                        string est = vEscribir != null ? vEscribir.EESS ?? "N/A" : "N/A";
                        LoggerService.Error(grifoObjetivo, archivoGrifoActual.Archivo ?? "DESCONOCIDO", $"El grifo {grifoObjetivo}, Estación: {est} del dia {fechaABuscar}: ERROR: La fecha {fechaABuscar} NO EXISTE en el Maestro. No se pudo guardar la data.");
                    }
                }
            }

            Console.WriteLine("Guardando archivo Excel...");
            package.Save();
            Console.WriteLine($"[✓] Archivo Excel guardado correctamente.");
        }
    }
}
