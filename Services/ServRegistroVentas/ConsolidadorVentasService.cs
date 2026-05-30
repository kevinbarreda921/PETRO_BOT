using OfficeOpenXml;
using PETRO_BOT.Models;
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
                Console.WriteLine("[x] No se encontró ningún archivo Excel maestro en la carpeta Registro_ventas.");
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

                // Buscamos la hoja cuyo nombre contenga el grifoObjetivo de forma flexible (ignora mayúsculas)
                var hojaEPPlus = workbook.Worksheets.FirstOrDefault(w => w.Name.IndexOf(grifoObjetivo, StringComparison.OrdinalIgnoreCase) >= 0);
                
                if (hojaEPPlus == null)
                {
                    LoggerService.Error(grifoObjetivo, "MAESTRO", $"No se encontró ninguna hoja para el grifo {grifoObjetivo} en EPPlus.");
                    continue;
                }

                // LoggerService.Info(grifoObjetivo, archivoGrifoActual.Archivo, $"Hoja encontrada y lista para procesar.");
                
                // Mapeamos las filas de fechas directamente desde memoria (sin leer el archivo de nuevo)
                var mapaFechasFilas = _escritor.MapearFechasHoja(hojaEPPlus);

                var fechasDelGrifo = archivoGrifoActual.ListVenta
                    .Where(v => !string.IsNullOrEmpty(v.Dia))
                    .Select(v => v.Dia!)
                    .Distinct()
                    .ToList();

                // Obtener configuración desde la base de datos
                var grifoDB = grifosList.FirstOrDefault(g => g.Nombre.Equals(grifoObjetivo, StringComparison.OrdinalIgnoreCase));
                if (grifoDB == null)
                {
                    Console.WriteLine($"[!] No se encontró configuración en la base de datos para el grifo: {grifoObjetivo}");
                    continue;
                }

                var configGrifo = grifoDB.Configuracion;

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
                AddCol(configGrifo.Col_Venta_GPL, "Venta_GPL");
                AddCol(configGrifo.Col_Venta_GNV, "Venta_GNV");
                AddCol(configGrifo.Col_Total_venta_acumulada, "Total_venta_acumulada");
                AddCol(configGrifo.Col_Total_Tarjeta_de_Credito_Liquidos, "Total_Tarjeta_de_Credito_Liquidos");
                AddCol(configGrifo.Col_Total_Tarjeta_de_Credito_GLP, "Total_Tarjeta_de_Credito_GLP");
                AddCol(configGrifo.Col_Total_Tarjeta_de_Credito_GNV, "Total_Tarjeta_de_Credito_GNV");
                AddCol(configGrifo.Col_ErrorMaquina, "ErrorMaquina");
                AddCol(configGrifo.Col_Recaudo_Cofide_GNV, "Recaudo_Cofide_GNV");
                AddCol(configGrifo.Col_Gastos, "Gastos");
                AddCol(configGrifo.Col_Ventas_con_transferencia, "Ventas_con_transferencia");
                AddCol(configGrifo.Col_DescuentoLiquidos, "DescuentoLiquidos");
                AddCol(configGrifo.Col_DescuentoGLP, "DescuentoGLP");
                AddCol(configGrifo.Col_Hermes_monto_liquido, "Hermes_monto_liquido");
                AddCol(configGrifo.Col_Hermes_monto_GLP, "Hermes_monto_GLP");
                AddCol(configGrifo.Col_Hermes_monto_GNV1, "Hermes_monto_GNV1");
                AddCol(configGrifo.Col_Hermes_monto_GNV2, "Hermes_monto_GNV2");

                foreach (string fechaABuscar in fechasDelGrifo)
                {
                    if (mapaFechasFilas.TryGetValue(fechaABuscar, out int filaDestino))
                    {
                        LoggerService.Info(grifoObjetivo, archivoGrifoActual.Archivo, $" El grifo {grifoObjetivo} del dia {fechaABuscar} procesado correctamente");

                        var ventaParaEscribir = archivoGrifoActual.ListVenta.FirstOrDefault(v => v.Dia == fechaABuscar);
                        if (ventaParaEscribir != null)
                        {
                            _escritor.EscribirFila(hojaEPPlus, ventaParaEscribir, filaDestino, columnasEscritura);
                            
                            if (clienteAColumna.Count > 0)
                            {
                                _escritor.EscribirClientesCredito(hojaEPPlus, ventaParaEscribir, filaDestino, clienteAColumna, grifoObjetivo, archivoGrifoActual.Archivo ?? "DESCONOCIDO");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[x] La fecha {fechaABuscar} NO EXISTE en el Maestro para el grifo {grifoObjetivo}");
                    }
                }
            }

            Console.WriteLine("Guardando archivo Excel...");
            package.Save();
            Console.WriteLine($"[✓] Archivo Excel guardado correctamente.");
        }
    }
}
