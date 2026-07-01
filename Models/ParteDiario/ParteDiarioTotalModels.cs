using System;
using System.Collections.Generic;

namespace PETRO_BOT.Models.ParteDiario
{
    public class ConfiguracionParteDiarioTotalModel
    {
        public int Id { get; set; }
        public string NombreGrifo { get; set; } = "";
        public string? Plantilla { get; set; } = "";
        public string? CeldaFecha { get; set; } = "";
        public string? CeldaEess { get; set; } = "";
        public string? PalabraClaveEess { get; set; } = "";
        public string? CeldaTotalDb5 { get; set; } = "";
        public string? CeldaTotalGlp { get; set; } = "";
        public string? CeldaTotalGasoholPremium { get; set; } = "";
        public string? CeldaTotalGasoholRegular { get; set; } = "";
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // Auxiliary coordinates for UI (col index 0-based, row index 1-based)
        public int ColumnaFecha { get; set; } = -1;
        public int FilaFecha { get; set; } = -1;
        public int ColumnaEess { get; set; } = -1;
        public int FilaEess { get; set; } = -1;
        public int ColumnaTotalDb5 { get; set; } = -1;
        public int FilaTotalDb5 { get; set; } = -1;
        public int ColumnaTotalGlp { get; set; } = -1;
        public int FilaTotalGlp { get; set; } = -1;
        public int ColumnaTotalGasoholPremium { get; set; } = -1;
        public int FilaTotalGasoholPremium { get; set; } = -1;
        public int ColumnaTotalGasoholRegular { get; set; } = -1;
        public int FilaTotalGasoholRegular { get; set; } = -1;
    }

    public class ParteDiarioTotalRegistroModel
    {
        public long Id { get; set; }
        public string? NombreGrifo { get; set; }
        public DateTime? Fecha { get; set; }
        public decimal TotalSalidaDb5 { get; set; }
        public decimal TotalSalidaGlp { get; set; }
        public decimal TotalSalidaGasoholPremium { get; set; }
        public decimal TotalSalidaGasoholRegular { get; set; }
        public string? ArchivoOrigen { get; set; }
        public string? NombreHoja { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }

    public class ArchivoProcesadoParteDiarioLog
    {
        public string NombreArchivo { get; set; } = "";
        public string NombreHoja { get; set; } = "";
        public string Estado { get; set; } = ""; // Correcto, Error, SinConfiguracion
        public string Mensaje { get; set; } = "";
        public string GrifoDetectado { get; set; } = "";
        public DateTime? FechaLectura { get; set; }
    }
}
