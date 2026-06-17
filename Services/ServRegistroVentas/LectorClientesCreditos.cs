using ExcelDataReader;
using PETRO_BOT.Models.Configuracion;
using System;
using System.Collections.Generic;
using System.IO;

namespace PETRO_BOT.Services.Services
{
    public class LectorClientesCreditos
    {
        public List<string> ObtenerClientes(List<string> filePaths, RegistroVentasGrifo grifo)
        {
            var clientes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var configGrifo = grifo.Configuracion;
            if (configGrifo == null) return new List<string>();

            int colLetraCreditoNombre = configGrifo.ColumnaCreditoNombre;
            if (colLetraCreditoNombre < 0) return new List<string>();

            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath)) continue;

                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                    do
                    {
                        int filaActual = 1;
                        bool leyendoClientes = false;
                        int flagclientecredito = 0;

                        while (reader.Read())
                        {
                            if (filaActual > configGrifo.FilaFinal) break;

                            if (!leyendoClientes && flagclientecredito == 0 && 
                                filaActual >= configGrifo.FilaCreditosNombre && 
                                filaActual <= configGrifo.FilaCreditosNombre + 10)
                            {
                                var valorNombre = (colLetraCreditoNombre >= 0 && colLetraCreditoNombre < reader.FieldCount) ? reader.GetValue(colLetraCreditoNombre) : null;
                                if (valorNombre != null && valorNombre.ToString()?.Trim().ToUpper().StartsWith("CLIENTE") == true)
                                {
                                    leyendoClientes = true;
                                }
                            }
                            else if (leyendoClientes)
                            {
                                var valorNombre = (colLetraCreditoNombre >= 0 && colLetraCreditoNombre < reader.FieldCount) ? reader.GetValue(colLetraCreditoNombre) : null;
                                if (valorNombre != null && !string.IsNullOrWhiteSpace(valorNombre.ToString()))
                                {
                                    string nombreLimpio = valorNombre.ToString()?.Trim() ?? string.Empty;
                                    if (grifo.Nombre.Equals("ACAPULCO", StringComparison.OrdinalIgnoreCase))
                                    {
                                        nombreLimpio = nombreLimpio.Length > 20 ? nombreLimpio.Substring(0, 20) : nombreLimpio;
                                    }
                                    if (!string.IsNullOrWhiteSpace(nombreLimpio))
                                    {
                                        clientes.Add(nombreLimpio);
                                    }
                                    flagclientecredito = 0;
                                }
                                else
                                {
                                    if (flagclientecredito == 1) leyendoClientes = false;
                                    flagclientecredito = 1;
                                }
                            }

                            filaActual++;
                        }
                    } while (reader.NextResult());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al leer clientes de crédito en {filePath}: {ex.Message}");
                }
            }

            return new List<string>(clientes);
        }
    }
}
