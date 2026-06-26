namespace PETRO_BOT.Models.ValidacionPrecio
{
    public class ArchivoImportadoVenta
    {
        public int Contador { get; set; }
        public string ArchivoImportado { get; set; } = string.Empty;
        public int CantidadRegistros { get; set; }
        public int Estado { get; set; } // 1: Success, 0: Error
        public string DescripcionEstado { get; set; } = string.Empty;
        public string RutaCompleta { get; set; } = string.Empty;
        public string Grifo { get; set; } = string.Empty;
        public long DemoraMs { get; set; }
    }
}
