using System;

namespace PETRO_BOT.Models.Log
{
    public class HistorialEjecucion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RobotNombre { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public int TotalArchivos { get; set; }
        public int TotalDiasCorrectos { get; set; }
        public double TiempoEjecucionSegundos { get; set; }
    }
}
