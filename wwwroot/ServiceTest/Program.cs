using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using PETRO_BOT.Services.ServRegistroVentas;

class Program
{
    static async Task Main()
    {
        string baseDir = @"c:\ProyectNet\PETRO_BOT";
        Directory.SetCurrentDirectory(baseDir);
        
        string origenPath = @"c:\ProyectNet\PETRO_BOT\wwwroot\uploads\Temp\Origen_bebbed61-e452-4a22-841a-a9b202dfc3a0_REGISTRO VENTAS -  2026-- Actualizado.xlsx";
        string destinoPath = @"c:\ProyectNet\PETRO_BOT\wwwroot\uploads\Temp\Destino_bd5dde17-0dfb-4cbc-a364-c4bbbd6d4b84_Total tarjetas y descuentos 2026.xlsx";
        
        var grifosDB = ConfiguracionService.ObtenerGrifosDB();
        int brasilId = grifosDB.Find(g => g.Nombre.Contains("BRASIL"))?.Id ?? 0;
        
        var procesador = new ProcesadorDescuentosExcelService();
        var fechas = new List<DateTime> { new DateTime(2026, 4, 1), new DateTime(2026, 4, 2), new DateTime(2026, 4, 3) };
        
        Console.WriteLine($"Running ProcesarDescuentosAsync for BRASIL (Id {brasilId})...");
        var result = await procesador.ProcesarDescuentosAsync(origenPath, destinoPath, new List<int> { brasilId }, fechas);
        
        foreach (var msg in result.MensajesError)
        {
            Console.WriteLine($"Error: {msg}");
        }
        Console.WriteLine($"Exito: {result.DiasProcesadosExito}, Errores: {result.DiasProcesadosError}");
    }
}
