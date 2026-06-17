namespace PETRO_BOT.Models
{
    public class ProgresoProceso
    {
        public int ArchivoActual { get; set; }
        public int TotalArchivos { get; set; }
        public PETRO_BOT.Models.Log.LogProcesoGrifo? LogCompletado { get; set; }
    }
}
