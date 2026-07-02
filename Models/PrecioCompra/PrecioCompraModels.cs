using System;
using System.Collections.Generic;

namespace PETRO_BOT.Models.PrecioCompra
{
    public class PrecioCompraRegistroModel
    {
        public long Id { get; set; }
        public string? Grifo { get; set; }
        public int Fila { get; set; }
        public string? DescripcionProducto { get; set; }
        public decimal CantidadGalones { get; set; }
        public decimal PrecioGalon { get; set; }
        public string? ArchivoOrigen { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }

    public class ArchivoProcesadoPrecioCompraLog
    {
        public string NombreArchivo { get; set; } = "";
        public string NombreHoja { get; set; } = "";
        public int Fila { get; set; }
        public string Estado { get; set; } = ""; // Correcto, Error, Alerta
        public string Mensaje { get; set; } = "";
        public string DescripcionProducto { get; set; } = "";
        public decimal CantidadGalones { get; set; }
        public decimal PrecioGalon { get; set; }
    }
}
