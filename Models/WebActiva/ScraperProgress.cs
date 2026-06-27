namespace PETRO_BOT.Models.WebActiva
{
    public class ScraperProgress
    {
        public string PasoActual { get; set; } = "";
        public int GrifosProcesados { get; set; }
        public int TotalGrifos { get; set; }
        public string UltimoMensajeLog { get; set; } = "";
        public bool DescargaIniciada { get; set; }
        public bool Finalizado { get; set; }
        public bool ConError { get; set; }
        public string MensajeError { get; set; } = "";
    }
}
