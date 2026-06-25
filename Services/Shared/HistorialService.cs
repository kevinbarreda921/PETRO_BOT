using PETRO_BOT.Models.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PETRO_BOT.Services.Shared
{
    public class HistorialService
    {
        private readonly string _rutaArchivo;
        private readonly object _fileLock = new object();

        public HistorialService()
        {
            _rutaArchivo = Path.Combine(PETRO_BOT.Services.Services.ConfiguracionService.ObtenerRutaBase(), "Json", "historial_ejecuciones.json");
            
            // Ensure directory exists
            var dir = Path.GetDirectoryName(_rutaArchivo);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public void RegistrarEjecucion(string robotNombre, int archivos, int diasCorrectos, double tiempoSegundos)
        {
            lock (_fileLock)
            {
                var historial = ObtenerHistorialInternal();
                historial.Add(new HistorialEjecucion
                {
                    RobotNombre = robotNombre,
                    TotalArchivos = archivos,
                    TotalDiasCorrectos = diasCorrectos,
                    TiempoEjecucionSegundos = tiempoSegundos,
                    FechaHora = DateTime.Now
                });

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(historial, options);
                File.WriteAllText(_rutaArchivo, json);
            }
        }

        public List<HistorialEjecucion> ObtenerHistorial()
        {
            lock (_fileLock)
            {
                return ObtenerHistorialInternal();
            }
        }

        private List<HistorialEjecucion> ObtenerHistorialInternal()
        {
            if (!File.Exists(_rutaArchivo))
                return new List<HistorialEjecucion>();

            try
            {
                string json = File.ReadAllText(_rutaArchivo);
                return JsonSerializer.Deserialize<List<HistorialEjecucion>>(json) ?? new List<HistorialEjecucion>();
            }
            catch
            {
                return new List<HistorialEjecucion>();
            }
        }
    }
}
