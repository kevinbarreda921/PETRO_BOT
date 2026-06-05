using System.Collections.Generic;

namespace PETRO_BOT.Models.Configuracion
{
    public class ConfigRoot
    {
        public Dictionary<string, GrifoConfig> Grifos { get; set; } = new();
    }

    public class GrifoConfig
    {
        public LecturaConfig? Lectura { get; set; }
        public EscrituraConfig? Escritura { get; set; }
        public Dictionary<string, string>? FilasClientesCreditos { get; set; }
    }

    public class LecturaConfig
    {
        public int ColumnaFecha { get; set; } = 14;
        public int FilaFecha { get; set; } = 3;
        public int ColumnaCreditoNombre { get; set; } = 0;
        public int ColumnaCreditoMonto { get; set; } = 6;
        public int ColumnaVariaCombusNombre { get; set; } = 16;
        public int FilaVariaCombusNombre { get; set; } = -1;
        public int ColumnaVariaCombusMonto { get; set; } = 18;
        public int FilaVariaCombusMonto { get; set; } = -1;
        public string VariaCombusNombre { get; set; } = "";
        public int ColumnaHermesBanco { get; set; } = -1;
        public int FilaHermesBanco { get; set; } = -1;
        public int ColumnaHermesTipo { get; set; } = -1;
        public int FilaHermesTipo { get; set; } = -1;
        public int ColumnaHermesMonto { get; set; } = 14;
        public int FilaHermesMonto { get; set; } = -1;
        public string HermesPalabraClaveMonto { get; set; } = "";
        public int FilaFinal { get; set; } = 129;
        public int FilaCreditosNombre { get; set; } = 10;
        public int FilaCreditosMonto { get; set; } = 10;
        public Dictionary<string, string> MapeoFilas { get; set; } = new();
    }

    public class EscrituraConfig
    {
        public Dictionary<string, string> Columnas { get; set; } = new();
    }

    public class RegistroVentasGrifo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Plantilla { get; set; } = "";
        public RegistroVentasConfiguracion Configuracion { get; set; } = new();
        public RegistroVentasWriteConfig RegistroVentasWrite { get; set; } = new();
        public List<RegistroVentasClienteCredito> ClientesCredito { get; set; } = new();
    }

    public class RegistroVentasConfiguracion
    {
        public int Id { get; set; }
        public int GrifoId { get; set; }
        
        // Reading general columns
        public int ColumnaFecha { get; set; } = 14;
        public int FilaFecha { get; set; } = 3;
        public int ColumnaCreditoNombre { get; set; } = 0;
        public int ColumnaCreditoMonto { get; set; } = 6;
        public int ColumnaVariaCombusNombre { get; set; } = 16;
        public int FilaVariaCombusNombre { get; set; } = -1;
        public int ColumnaVariaCombusMonto { get; set; } = 18;
        public int FilaVariaCombusMonto { get; set; } = -1;
        public string VariaCombusNombre { get; set; } = "";
        public int ColumnaHermesBanco { get; set; } = -1;
        public int FilaHermesBanco { get; set; } = -1;
        public int ColumnaHermesTipo { get; set; } = -1;
        public int FilaHermesTipo { get; set; } = -1;
        public int ColumnaHermesMonto { get; set; } = 14;
        public int FilaHermesMonto { get; set; } = -1;
        public string HermesPalabraClaveMonto { get; set; } = "";
        public int FilaFinal { get; set; } = 129;
        public int FilaCreditosNombre { get; set; } = 10;
        public int FilaCreditosMonto { get; set; } = 10;

        // Sequential column and row mapping pairs
        public string Col_Venta_GPL { get; set; } = "";
        public string Fila_Venta_GPL { get; set; } = "";
        
        public string Col_Venta_GNV { get; set; } = "";
        public string Fila_Venta_GNV { get; set; } = "";
        
        public string Col_Total_venta_acumulada { get; set; } = "";
        public string Fila_Total_venta_acumulada { get; set; } = "";
        
        public string Col_Total_Tarjeta_de_Credito_Liquidos { get; set; } = "";
        public string Fila_Total_Tarjeta_de_Credito_Liquidos { get; set; } = "";
        
        public string Col_Total_Tarjeta_de_Credito_GLP { get; set; } = "";
        public string Fila_Total_Tarjeta_de_Credito_GLP { get; set; } = "";
        
        public string Col_Total_Tarjeta_de_Credito_GNV { get; set; } = "";
        public string Fila_Total_Tarjeta_de_Credito_GNV { get; set; } = "";
        
        public string Col_ErrorMaquina { get; set; } = "";
        public string Fila_ErrorMaquina { get; set; } = "";
        
        public string Col_Recaudo_Cofide_GNV { get; set; } = "";
        public string Fila_Recaudo_Cofide_GNV { get; set; } = "";
        
        public string Col_Gastos { get; set; } = "";
        public string Fila_Gastos { get; set; } = "";
        
        public string Col_Ventas_con_transferencia { get; set; } = "";
        public string Fila_Ventas_con_transferencia { get; set; } = "";

        // Writing only columns
        public string Col_DescuentoLiquidos { get; set; } = "";
        public string Col_DescuentoGLP { get; set; } = "";
        public string Col_Hermes_monto_liquido { get; set; } = "";
        public string Col_Hermes_monto_GLP { get; set; } = "";
        public string Col_Hermes_monto_GNV1 { get; set; } = "";
        public string Col_Hermes_monto_GNV2 { get; set; } = "";
    }

    public class RegistroVentasClienteCredito
    {
        public int Id { get; set; }
        public int GrifoId { get; set; }
        public string Columna { get; set; } = "";
        public string ClienteNombre { get; set; } = "";
    }

    public class RegistroVentasWriteConfig
    {
        public int Id { get; set; }
        public int GrifoId { get; set; }
        public string NombreHoja { get; set; } = "";
        public int FilaSeleccion { get; set; } = 10;

        // Columns from "Columnas Excel Maestro"
        public string Venta_GPL { get; set; } = "";
        public string Venta_GNV { get; set; } = "";
        public string Total_venta_acumulada { get; set; } = "";
        public string Total_Tarjeta_de_Credito_Liquidos { get; set; } = "";
        public string Total_Tarjeta_de_Credito_GLP { get; set; } = "";
        public string Total_Tarjeta_de_Credito_GNV { get; set; } = "";
        public string ErrorMaquina { get; set; } = "";
        public string Recaudo_Cofide_GNV { get; set; } = "";
        public string Gastos { get; set; } = "";
        public string Ventas_con_transferencia { get; set; } = "";
        public string DescuentoLiquidos { get; set; } = "";
        public string DescuentoGLP { get; set; } = "";
        public string Hermes_monto_liquido { get; set; } = "";
        public string Hermes_monto_GLP { get; set; } = "";
        public string Hermes_monto_GNV1 { get; set; } = "";
        public string Hermes_monto_GNV2 { get; set; } = "";
    }
}
