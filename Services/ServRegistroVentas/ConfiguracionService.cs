using Microsoft.Data.Sqlite;
using PETRO_BOT.Models.Configuracion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PETRO_BOT.Services.Services
{
    public static class ConfiguracionService
    {
        private static ConfigRoot _configGlobal = new();
        private static bool _isLoading = false;
        
        public static ConfigRoot ConfigGlobal
        {
            get
            {
                if (!_isLoading)
                {
                    CargarConfiguracion();
                }
                return _configGlobal;
            }
        }

        static ConfiguracionService()
        {
            CargarConfiguracion();
        }

        public static string ObtenerWebRootPath(string fallbackWebRoot)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (baseDir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") || 
                baseDir.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}") ||
                baseDir.EndsWith("bin") || 
                !Directory.Exists(Path.Combine(baseDir, "wwwroot")))
            {
                return Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\wwwroot"));
            }
            return fallbackWebRoot;
        }

        private static string GetDatabasePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Check if we are running in development (inside bin folder)
            if (baseDir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") || 
                baseDir.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}") ||
                baseDir.EndsWith("bin") || 
                !Directory.Exists(Path.Combine(baseDir, "wwwroot")))
            {
                string devPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\wwwroot\bd\DB_PETRO_BOT.db"));
                string devDir = Path.GetDirectoryName(devPath) ?? "";
                if (!Directory.Exists(devDir))
                {
                    Directory.CreateDirectory(devDir);
                }
                return devPath;
            }
            
            // Production path
            string prodPath = Path.Combine(baseDir, "wwwroot", "bd", "DB_PETRO_BOT.db");
            string prodDir = Path.GetDirectoryName(prodPath) ?? "";
            if (!Directory.Exists(prodDir))
            {
                Directory.CreateDirectory(prodDir);
            }
            return prodPath;
        }

        private static string GetConnectionString()
        {
            return $"Data Source={GetDatabasePath()};Pooling=False;";
        }

        public static void InicializarBaseDatos()
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();

            // Create tables
            string createGrifosTable = @"
                CREATE TABLE IF NOT EXISTS REGISTRO_VENTAS_GRIFOS (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT UNIQUE NOT NULL,
                    Plantilla TEXT
                );";

            string createConfiguracionTable = @"
                CREATE TABLE IF NOT EXISTS REGISTRO_VENTAS_CONFIGURACION (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GrifoId INTEGER NOT NULL,
                    EESS TEXT DEFAULT '',
                    ColumnaEESS TEXT DEFAULT '',
                    FilaEESS INTEGER NOT NULL DEFAULT -1,
                    ColumnaFecha INTEGER NOT NULL,
                    FilaFecha INTEGER NOT NULL DEFAULT 3,
                    ColumnaCreditoNombre INTEGER NOT NULL,
                    ColumnaCreditoMonto INTEGER NOT NULL,
                    ColumnaVariaCombusNombre INTEGER NOT NULL,
                    FilaVariaCombusNombre INTEGER NOT NULL DEFAULT -1,
                    ColumnaVariaCombusMonto INTEGER NOT NULL,
                    FilaVariaCombusMonto INTEGER NOT NULL DEFAULT -1,
                    VariaCombusNombre TEXT DEFAULT '',
                    ColumnaHermesMonto INTEGER NOT NULL DEFAULT 14,
                    FilaHermesMonto INTEGER NOT NULL DEFAULT -1,
                    ColumnaHermesBanco INTEGER NOT NULL DEFAULT -1,
                    FilaHermesBanco INTEGER NOT NULL DEFAULT -1,
                    ColumnaHermesTipo INTEGER NOT NULL DEFAULT -1,
                    FilaHermesTipo INTEGER NOT NULL DEFAULT -1,
                    HermesPalabraClaveMonto TEXT DEFAULT '',
                    FilaFinal INTEGER NOT NULL DEFAULT 129,
                    FilaCreditosNombre INTEGER NOT NULL DEFAULT 10,
                    FilaCreditosMonto INTEGER NOT NULL DEFAULT 10,
                    
                    -- Sequential column and row mapping pairs
                    Col_Venta_GPL TEXT,
                    Fila_Venta_GPL TEXT,
                    Col_Venta_GNV TEXT,
                    Fila_Venta_GNV TEXT,
                    Col_Total_venta_acumulada TEXT,
                    Fila_Total_venta_acumulada TEXT,
                    Col_Total_Tarjeta_de_Credito_Liquidos TEXT,
                    Fila_Total_Tarjeta_de_Credito_Liquidos TEXT,
                    Col_Total_Tarjeta_de_Credito_GLP TEXT,
                    Fila_Total_Tarjeta_de_Credito_GLP TEXT,
                    Col_Total_Tarjeta_de_Credito_GNV TEXT,
                    Fila_Total_Tarjeta_de_Credito_GNV TEXT,
                    Col_ErrorMaquina TEXT,
                    Fila_ErrorMaquina TEXT,
                    Col_Recaudo_Cofide_GNV TEXT,
                    Fila_Recaudo_Cofide_GNV TEXT,
                    Col_Gastos TEXT,
                    Fila_Gastos TEXT,
                    Col_Ventas_con_transferencia TEXT,
                    Fila_Ventas_con_transferencia TEXT,

                    -- Writing only columns
                    Col_DescuentoLiquidos TEXT,
                    Col_DescuentoGLP TEXT,
                    Col_Hermes_monto_liquido TEXT,
                    Col_Hermes_monto_GLP TEXT,
                    Col_Hermes_monto_GNV1 TEXT,
                    Col_Hermes_monto_GNV2 TEXT,

                    FOREIGN KEY (GrifoId) REFERENCES REGISTRO_VENTAS_GRIFOS (Id) ON DELETE CASCADE
                );";

            string createClientesTable = @"
                CREATE TABLE IF NOT EXISTS REGISTRO_VENTAS_CLIENTE_CREDITOS (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GrifoId INTEGER NOT NULL,
                    Columna TEXT NOT NULL,
                    ClienteNombre TEXT NOT NULL,
                    FOREIGN KEY (GrifoId) REFERENCES REGISTRO_VENTAS_GRIFOS (Id) ON DELETE CASCADE
                );";

            string createWriteConfigTable = @"
                CREATE TABLE IF NOT EXISTS REGISTRO_VENTAS_WRITE (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GrifoId INTEGER NOT NULL,
                    NombreHoja TEXT,
                    FilaSeleccion INTEGER NOT NULL DEFAULT 10,
                    Venta_GPL TEXT,
                    Venta_GNV TEXT,
                    Total_venta_acumulada TEXT,
                    Total_Tarjeta_de_Credito_Liquidos TEXT,
                    Total_Tarjeta_de_Credito_GLP TEXT,
                    Total_Tarjeta_de_Credito_GNV TEXT,
                    ErrorMaquina TEXT,
                    Recaudo_Cofide_GNV TEXT,
                    Gastos TEXT,
                    Ventas_con_transferencia TEXT,
                    DescuentoLiquidos TEXT,
                    DescuentoGLP TEXT,
                    Hermes_monto_liquido TEXT,
                    Hermes_monto_GLP TEXT,
                    Hermes_monto_GNV1 TEXT,
                    Hermes_monto_GNV2 TEXT,
                    FOREIGN KEY (GrifoId) REFERENCES REGISTRO_VENTAS_GRIFOS (Id) ON DELETE CASCADE
                );";

            string createDescuentosWriteConfigTable = @"
                CREATE TABLE IF NOT EXISTS REGISTRO_DESCUENTOS_WRITE (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GrifoId INTEGER NOT NULL,
                    NombreHoja TEXT,
                    Plantilla TEXT,
                    FilaSeleccion INTEGER NOT NULL DEFAULT 10,
                    ColumnaFecha TEXT,
                    TarjetaLiquidos TEXT,
                    TarjetaGLP TEXT,
                    DescLiquidos TEXT,
                    DescGLP TEXT,
                    FOREIGN KEY (GrifoId) REFERENCES REGISTRO_VENTAS_GRIFOS (Id) ON DELETE CASCADE
                );";

            using (var cmd = new SqliteCommand(createGrifosTable, connection)) cmd.ExecuteNonQuery();
            using (var cmd = new SqliteCommand(createConfiguracionTable, connection)) cmd.ExecuteNonQuery();
            using (var cmd = new SqliteCommand(createClientesTable, connection)) cmd.ExecuteNonQuery();
            using (var cmd = new SqliteCommand(createWriteConfigTable, connection)) cmd.ExecuteNonQuery();
            using (var cmd = new SqliteCommand(createDescuentosWriteConfigTable, connection)) cmd.ExecuteNonQuery();
        }

        private static void MigrarJsonSiEsNecesario()
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();

            long count = 0;
            using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM REGISTRO_VENTAS_GRIFOS ORDER BY 1 ASC", connection))
            {
                count = (long)(cmd.ExecuteScalar() ?? 0L);
            }

            if (count == 0)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string rutaJson = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Json\JsonRegistroVentas\", "config_grifos.json"));
                if (!File.Exists(rutaJson))
                {
                    rutaJson = Path.Combine(baseDir, "Json", "JsonRegistroVentas", "config_grifos.json");
                }

                if (File.Exists(rutaJson))
                {
                    try
                    {
                        string jsonTexto = File.ReadAllText(rutaJson);
                        var jsonConfig = JsonSerializer.Deserialize<ConfigRoot>(jsonTexto);
                        if (jsonConfig?.Grifos != null)
                        {
                            foreach (var kvp in jsonConfig.Grifos)
                            {
                                string nombre = kvp.Key.Trim().ToUpper();
                                var config = kvp.Value;
                                GuardarGrifoConfigInterno(nombre, config, connection);
                            }
                            Console.WriteLine("Migración de JSON a SQLite de columnas relacionales completada.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al migrar JSON a SQLite de columnas relacionales: {ex.Message}");
                    }
                }
            }
        }

        private static string GetRowNumberForProperty(Dictionary<string, string>? dict, string propName)
        {
            if (dict == null) return "";
            foreach (var kvp in dict)
            {
                // Unify Venta_GLP / Venta_GPL spelling
                if (string.Equals(kvp.Value, propName, StringComparison.OrdinalIgnoreCase) ||
                    (propName.Equals("Venta_GPL", StringComparison.OrdinalIgnoreCase) && string.Equals(kvp.Value, "Venta_GLP", StringComparison.OrdinalIgnoreCase)) ||
                    (propName.Equals("Venta_GLP", StringComparison.OrdinalIgnoreCase) && string.Equals(kvp.Value, "Venta_GPL", StringComparison.OrdinalIgnoreCase)))
                {
                    return kvp.Key;
                }
            }
            return "";
        }

        private static string GetColLetterForProperty(Dictionary<string, string>? dict, string propName)
        {
            if (dict == null) return "";
            if (dict.TryGetValue(propName, out var col))
            {
                return col ?? "";
            }
            // Fallback alias for Venta_GPL / Venta_GLP
            if (propName.Equals("Venta_GPL", StringComparison.OrdinalIgnoreCase) && dict.TryGetValue("Venta_GLP", out var colGlp))
            {
                return colGlp ?? "";
            }
            if (propName.Equals("Venta_GLP", StringComparison.OrdinalIgnoreCase) && dict.TryGetValue("Venta_GPL", out var colGpl))
            {
                return colGpl ?? "";
            }
            return "";
        }

        public static string GetExcelColumnName(int columnNumber)
        {
            if (columnNumber < 0) return "";
            int dividend = columnNumber + 1;
            string columnName = string.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        public static int GetExcelColumnIndex(string columnName)
        {
            if (string.IsNullOrEmpty(columnName)) return -1;
            columnName = columnName.Trim().ToUpper();
            
            if (int.TryParse(columnName, out int val))
            {
                return val;
            }

            int index = 0;
            for (int i = 0; i < columnName.Length; i++)
            {
                char c = columnName[i];
                if (c < 'A' || c > 'Z') return -1;
                index *= 26;
                index += (c - 'A' + 1);
            }
            return index - 1;
        }

        private static int ParseColumnDbValue(object dbValue, int defaultValue = -1)
        {
            if (dbValue == null || dbValue == DBNull.Value) return defaultValue;
            string strVal = dbValue.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(strVal)) return defaultValue;
            if (int.TryParse(strVal, out int val)) return val;
            return GetExcelColumnIndex(strVal);
        }

        private static void GuardarGrifoConfigInterno(string nombreGrifo, GrifoConfig config, SqliteConnection connection)
        {
            nombreGrifo = nombreGrifo.Trim().ToUpper();
            
            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. Insert or ignore Grifo to get or create it
                string insertGrifo = "INSERT OR IGNORE INTO REGISTRO_VENTAS_GRIFOS (Nombre) VALUES (@Nombre);";
                using (var cmd = new SqliteCommand(insertGrifo, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombreGrifo);
                    cmd.ExecuteNonQuery();
                }

                // Get Grifo ID
                long grifoId = 0;
                using (var cmd = new SqliteCommand("SELECT Id FROM REGISTRO_VENTAS_GRIFOS WHERE Nombre = @Nombre;", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombreGrifo);
                    grifoId = (long)(cmd.ExecuteScalar() ?? 0L);
                }

                if (grifoId == 0)
                {
                    throw new Exception($"No se pudo obtener o crear el Id para el grifo: {nombreGrifo}");
                }
                // 2. Insert or replace Configuration for this GrifoId
                string insertConfig = @"
                    INSERT INTO REGISTRO_VENTAS_CONFIGURACION (
                        GrifoId, EESS, ColumnaEESS, FilaEESS, ColumnaFecha, FilaFecha, ColumnaCreditoNombre, 
                        ColumnaCreditoMonto, ColumnaVariaCombusNombre, FilaVariaCombusNombre, ColumnaVariaCombusMonto, FilaVariaCombusMonto, VariaCombusNombre, ColumnaHermesMonto,
                        FilaHermesMonto, ColumnaHermesBanco, FilaHermesBanco, ColumnaHermesTipo, FilaHermesTipo, HermesPalabraClaveMonto,
                        FilaFinal, FilaCreditosNombre, FilaCreditosMonto,
                        
                        -- Sequential column and row mapping pairs
                        Col_Venta_GPL, Fila_Venta_GPL,
                        Col_Venta_GNV, Fila_Venta_GNV,
                        Col_Total_venta_acumulada, Fila_Total_venta_acumulada,
                        Col_Total_Tarjeta_de_Credito_Liquidos, Fila_Total_Tarjeta_de_Credito_Liquidos,
                        Col_Total_Tarjeta_de_Credito_GLP, Fila_Total_Tarjeta_de_Credito_GLP,
                        Col_Total_Tarjeta_de_Credito_GNV, Fila_Total_Tarjeta_de_Credito_GNV,
                        Col_ErrorMaquina, Fila_ErrorMaquina,
                        Col_Recaudo_Cofide_GNV, Fila_Recaudo_Cofide_GNV,
                        Col_Gastos, Fila_Gastos,
                        Col_Ventas_con_transferencia, Fila_Ventas_con_transferencia,
                        
                        -- Writing only columns
                        Col_DescuentoLiquidos, Col_DescuentoGLP,
                        Col_Hermes_monto_liquido, Col_Hermes_monto_GLP,
                        Col_Hermes_monto_GNV1, Col_Hermes_monto_GNV2
                    ) VALUES (
                        @GrifoId, @EESS, @ColumnaEESS, @FilaEESS, @ColumnaFecha, @FilaFecha, @ColumnaCreditoNombre, 
                        @ColumnaCreditoMonto, @ColumnaVariaCombusNombre, @FilaVariaCombusNombre, @ColumnaVariaCombusMonto, @FilaVariaCombusMonto, @VariaCombusNombre, @ColumnaHermesMonto,
                        @FilaHermesMonto, @ColumnaHermesBanco, @FilaHermesBanco, @ColumnaHermesTipo, @FilaHermesTipo, @HermesPalabraClaveMonto,
                        @FilaFinal, @FilaCreditosNombre, @FilaCreditosMonto,
                        
                        -- Pairs
                        @Col_Venta_GPL, @Fila_Venta_GPL,
                        @Col_Venta_GNV, @Fila_Venta_GNV,
                        @Col_Total_venta_acumulada, @Fila_Total_venta_acumulada,
                        @Col_Total_Tarjeta_de_Credito_Liquidos, @Fila_Total_Tarjeta_de_Credito_Liquidos,
                        @Col_Total_Tarjeta_de_Credito_GLP, @Fila_Total_Tarjeta_de_Credito_GLP,
                        @Col_Total_Tarjeta_de_Credito_GNV, @Fila_Total_Tarjeta_de_Credito_GNV,
                        @Col_ErrorMaquina, @Fila_ErrorMaquina,
                        @Col_Recaudo_Cofide_GNV, @Fila_Recaudo_Cofide_GNV,
                        @Col_Gastos, @Fila_Gastos,
                        @Col_Ventas_con_transferencia, @Fila_Ventas_con_transferencia,
                        
                        -- Writing only
                        @Col_DescuentoLiquidos, @Col_DescuentoGLP,
                        @Col_Hermes_monto_liquido, @Col_Hermes_monto_GLP,
                        @Col_Hermes_monto_GNV1, @Col_Hermes_monto_GNV2
                    );";
 
                // First delete existing configuration for this GrifoId to overwrite
                using (var cmd = new SqliteCommand("DELETE FROM REGISTRO_VENTAS_CONFIGURACION WHERE GrifoId = @GrifoId;", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.ExecuteNonQuery();
                }
 
                using (var cmd = new SqliteCommand(insertConfig, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.Parameters.AddWithValue("@EESS", config.Lectura?.EESS ?? "");
                    cmd.Parameters.AddWithValue("@ColumnaEESS", GetExcelColumnName(config.Lectura?.ColumnaEESS ?? -1));
                    cmd.Parameters.AddWithValue("@FilaEESS", config.Lectura?.FilaEESS ?? -1);
                    cmd.Parameters.AddWithValue("@ColumnaFecha", GetExcelColumnName(config.Lectura?.ColumnaFecha ?? 14));
                    cmd.Parameters.AddWithValue("@FilaFecha", config.Lectura?.FilaFecha ?? 3);
                    cmd.Parameters.AddWithValue("@ColumnaCreditoNombre", GetExcelColumnName(config.Lectura?.ColumnaCreditoNombre ?? 0));
                    cmd.Parameters.AddWithValue("@ColumnaCreditoMonto", GetExcelColumnName(config.Lectura?.ColumnaCreditoMonto ?? 6));
                    cmd.Parameters.AddWithValue("@ColumnaVariaCombusNombre", GetExcelColumnName(config.Lectura?.ColumnaVariaCombusNombre ?? 16));
                    cmd.Parameters.AddWithValue("@FilaVariaCombusNombre", config.Lectura?.FilaVariaCombusNombre ?? -1);
                    cmd.Parameters.AddWithValue("@ColumnaVariaCombusMonto", GetExcelColumnName(config.Lectura?.ColumnaVariaCombusMonto ?? 18));
                    cmd.Parameters.AddWithValue("@FilaVariaCombusMonto", config.Lectura?.FilaVariaCombusMonto ?? -1);
                    cmd.Parameters.AddWithValue("@VariaCombusNombre", config.Lectura?.VariaCombusNombre ?? "");
                    cmd.Parameters.AddWithValue("@ColumnaHermesMonto", GetExcelColumnName(config.Lectura?.ColumnaHermesMonto ?? 14));
                    cmd.Parameters.AddWithValue("@FilaHermesMonto", config.Lectura?.FilaHermesMonto ?? -1);
                    cmd.Parameters.AddWithValue("@ColumnaHermesBanco", GetExcelColumnName(config.Lectura?.ColumnaHermesBanco ?? -1));
                    cmd.Parameters.AddWithValue("@FilaHermesBanco", config.Lectura?.FilaHermesBanco ?? -1);
                    cmd.Parameters.AddWithValue("@ColumnaHermesTipo", GetExcelColumnName(config.Lectura?.ColumnaHermesTipo ?? -1));
                    cmd.Parameters.AddWithValue("@FilaHermesTipo", config.Lectura?.FilaHermesTipo ?? -1);
                    cmd.Parameters.AddWithValue("@HermesPalabraClaveMonto", config.Lectura?.HermesPalabraClaveMonto ?? "");
                    cmd.Parameters.AddWithValue("@FilaFinal", config.Lectura?.FilaFinal ?? 129);
                    cmd.Parameters.AddWithValue("@FilaCreditosNombre", config.Lectura?.FilaCreditosNombre ?? 10);
                    cmd.Parameters.AddWithValue("@FilaCreditosMonto", config.Lectura?.FilaCreditosMonto ?? 10);
                    
                    var m = config.Lectura?.MapeoFilas;
                    var c = config.Escritura?.Columnas;

                    cmd.Parameters.AddWithValue("@Col_Venta_GPL", GetColLetterForProperty(c, "Venta_GPL"));
                    cmd.Parameters.AddWithValue("@Fila_Venta_GPL", GetRowNumberForProperty(m, "Venta_GPL"));
                    cmd.Parameters.AddWithValue("@Col_Venta_GNV", GetColLetterForProperty(c, "Venta_GNV"));
                    cmd.Parameters.AddWithValue("@Fila_Venta_GNV", GetRowNumberForProperty(m, "Venta_GNV"));
                    cmd.Parameters.AddWithValue("@Col_Total_venta_acumulada", GetColLetterForProperty(c, "Total_venta_acumulada"));
                    cmd.Parameters.AddWithValue("@Fila_Total_venta_acumulada", GetRowNumberForProperty(m, "Total_venta_acumulada"));
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_Liquidos", GetColLetterForProperty(c, "Total_Tarjeta_de_Credito_Liquidos"));
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_Liquidos", GetRowNumberForProperty(m, "Total_Tarjeta_de_Credito_Liquidos"));
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_GLP", GetColLetterForProperty(c, "Total_Tarjeta_de_Credito_GLP"));
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_GLP", GetRowNumberForProperty(m, "Total_Tarjeta_de_Credito_GLP"));
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_GNV", GetColLetterForProperty(c, "Total_Tarjeta_de_Credito_GNV"));
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_GNV", GetRowNumberForProperty(m, "Total_Tarjeta_de_Credito_GNV"));
                    cmd.Parameters.AddWithValue("@Col_ErrorMaquina", GetColLetterForProperty(c, "ErrorMaquina"));
                    cmd.Parameters.AddWithValue("@Fila_ErrorMaquina", GetRowNumberForProperty(m, "ErrorMaquina"));
                    cmd.Parameters.AddWithValue("@Col_Recaudo_Cofide_GNV", GetColLetterForProperty(c, "Recaudo_Cofide_GNV"));
                    cmd.Parameters.AddWithValue("@Fila_Recaudo_Cofide_GNV", GetRowNumberForProperty(m, "Recaudo_Cofide_GNV"));
                    cmd.Parameters.AddWithValue("@Col_Gastos", GetColLetterForProperty(c, "Gastos"));
                    cmd.Parameters.AddWithValue("@Fila_Gastos", GetRowNumberForProperty(m, "Gastos"));
                    cmd.Parameters.AddWithValue("@Col_Ventas_con_transferencia", GetColLetterForProperty(c, "Ventas_con_transferencia"));
                    cmd.Parameters.AddWithValue("@Fila_Ventas_con_transferencia", GetRowNumberForProperty(m, "Ventas_con_transferencia"));

                    cmd.Parameters.AddWithValue("@Col_DescuentoLiquidos", GetColLetterForProperty(c, "DescuentoLiquidos"));
                    cmd.Parameters.AddWithValue("@Col_DescuentoGLP", GetColLetterForProperty(c, "DescuentoGLP"));
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_liquido", GetColLetterForProperty(c, "Hermes_monto_liquido"));
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GLP", GetColLetterForProperty(c, "Hermes_monto_GLP"));
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GNV1", GetColLetterForProperty(c, "Hermes_monto_GNV1"));
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GNV2", GetColLetterForProperty(c, "Hermes_monto_GNV2"));

                    cmd.ExecuteNonQuery();
                }

                // 3. Clear old Clientes for this GrifoId and insert new ones
                using (var cmd = new SqliteCommand("DELETE FROM REGISTRO_VENTAS_CLIENTE_CREDITOS WHERE GrifoId = @GrifoId;", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.ExecuteNonQuery();
                }

                if (config.FilasClientesCreditos != null)
                {
                    string insertCliente = "INSERT INTO REGISTRO_VENTAS_CLIENTE_CREDITOS (GrifoId, Columna, ClienteNombre) VALUES (@GrifoId, @Columna, @ClienteNombre);";
                    foreach (var kvp in config.FilasClientesCreditos)
                    {
                        using var cmd = new SqliteCommand(insertCliente, connection, transaction);
                        cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                        cmd.Parameters.AddWithValue("@Columna", kvp.Key);
                        cmd.Parameters.AddWithValue("@ClienteNombre", kvp.Value ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public static void GuardarGrifoConfig(string nombreGrifo, GrifoConfig config)
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();
            GuardarGrifoConfigInterno(nombreGrifo, config, connection);
        }

        public static void EliminarGrifo(string nombreGrifo)
        {
            nombreGrifo = nombreGrifo.Trim().ToUpper();
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                using (var cmd = new SqliteCommand("DELETE FROM REGISTRO_VENTAS_GRIFOS WHERE Nombre = @Nombre;", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombreGrifo);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public static void CargarConfiguracion()
        {
            try
            {
                string dbPath = GetDatabasePath();
                bool dbExists = File.Exists(dbPath);
                
                bool needsSchemaCreation = !dbExists;
                if (dbExists)
                {
                    using var connection = new SqliteConnection(GetConnectionString());
                    connection.Open();
                    // We check if the dedicated column schema exists by verifying REGISTRO_VENTAS_CONFIGURACION and a column like 'Fila_Venta_GPL'
                    using var cmd = new SqliteCommand("SELECT COUNT(*) FROM sqlite_schema WHERE type='table' AND name='REGISTRO_VENTAS_CONFIGURACION';", connection);
                    long tableCount = (long)(cmd.ExecuteScalar() ?? 0L);
                    if (tableCount == 0)
                    {
                        needsSchemaCreation = true;
                    }
                    else
                    {
                        // Check if the new sequential column schema exists by verifying table_info column names
                        using var colCmd = new SqliteCommand("PRAGMA table_info(REGISTRO_VENTAS_CONFIGURACION);", connection);
                        using var reader = colCmd.ExecuteReader();
                        bool hasNewSequentialColumns = false;
                        while (reader.Read())
                        {
                            string colName = reader.GetString(1);
                            if (colName.Equals("Col_Venta_GPL", StringComparison.OrdinalIgnoreCase))
                            {
                                hasNewSequentialColumns = true;
                                break;
                            }
                        }
                        if (!hasNewSequentialColumns)
                        {
                            needsSchemaCreation = true;
                        }
                        else
                        {
                            // Check if the Plantilla column exists in REGISTRO_VENTAS_GRIFOS
                            using var colCmd2 = new SqliteCommand("PRAGMA table_info(REGISTRO_VENTAS_GRIFOS);", connection);
                            using var reader2 = colCmd2.ExecuteReader();
                            bool hasPlantillaColumn = false;
                            while (reader2.Read())
                            {
                                string colName = reader2.GetString(1);
                                if (colName.Equals("Plantilla", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasPlantillaColumn = true;
                                    break;
                                }
                            }
                            if (!hasPlantillaColumn)
                            {
                                // Alter table to add Plantilla column dynamically
                                using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_GRIFOS ADD COLUMN Plantilla TEXT;", connection);
                                alterCmd.ExecuteNonQuery();
                                Console.WriteLine("Columna 'Plantilla' agregada a REGISTRO_VENTAS_GRIFOS exitosamente.");
                            }

                            // Check if the FilaFecha column exists in REGISTRO_VENTAS_CONFIGURACION
                            using var colCmd3 = new SqliteCommand("PRAGMA table_info(REGISTRO_VENTAS_CONFIGURACION);", connection);
                            using var reader3 = colCmd3.ExecuteReader();
                            bool hasFilaFecha = false;
                            while (reader3.Read())
                            {
                                string colName = reader3.GetString(1);
                                if (colName.Equals("FilaFecha", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasFilaFecha = true;
                                    break;
                                }
                            }
                            if (!hasFilaFecha)
                            {
                                // Alter table to add FilaFecha column dynamically
                                using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN FilaFecha INTEGER NOT NULL DEFAULT 3;", connection);
                                alterCmd.ExecuteNonQuery();
                                Console.WriteLine("Columna 'FilaFecha' agregada a REGISTRO_VENTAS_CONFIGURACION exitosamente.");
                            }

                            // Check if the EESS, ColumnaEESS and FilaEESS columns exist in REGISTRO_VENTAS_CONFIGURACION
                            using (var colCmdEESS = new SqliteCommand("PRAGMA table_info(REGISTRO_VENTAS_CONFIGURACION);", connection))
                            using (var readerEESS = colCmdEESS.ExecuteReader())
                            {
                                bool hasEESS = false;
                                bool hasColumnaEESS = false;
                                bool hasFilaEESS = false;
                                while (readerEESS.Read())
                                {
                                    string colName = readerEESS.GetString(1);
                                    if (colName.Equals("EESS", StringComparison.OrdinalIgnoreCase)) hasEESS = true;
                                    if (colName.Equals("ColumnaEESS", StringComparison.OrdinalIgnoreCase)) hasColumnaEESS = true;
                                    if (colName.Equals("FilaEESS", StringComparison.OrdinalIgnoreCase)) hasFilaEESS = true;
                                }
                                if (!hasEESS)
                                {
                                    // Alter table to add EESS column dynamically
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN EESS TEXT DEFAULT '';", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'EESS' agregada a REGISTRO_VENTAS_CONFIGURACION exitosamente.");
                                }
                                if (!hasColumnaEESS)
                                {
                                    // Alter table to add ColumnaEESS column dynamically
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN ColumnaEESS TEXT DEFAULT '';", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'ColumnaEESS' agregada a REGISTRO_VENTAS_CONFIGURACION exitosamente.");
                                }
                                if (!hasFilaEESS)
                                {
                                    // Alter table to add FilaEESS column dynamically
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN FilaEESS INTEGER NOT NULL DEFAULT -1;", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'FilaEESS' agregada a REGISTRO_VENTAS_CONFIGURACION exitosamente.");
                                }
                            }

                            // Dynamic check for FilaFinal, FilaCreditosNombre, and FilaCreditosMonto
                            using (var colCmd4 = new SqliteCommand("PRAGMA table_info(REGISTRO_VENTAS_CONFIGURACION);", connection))
                            using (var reader4 = colCmd4.ExecuteReader())
                            {
                                bool hasFilaFinal = false;
                                bool hasFilaCreditosNombre = false;
                                bool hasFilaCreditosMonto = false;
                                while (reader4.Read())
                                {
                                    string colName = reader4.GetString(1);
                                    if (colName.Equals("FilaFinal", StringComparison.OrdinalIgnoreCase)) hasFilaFinal = true;
                                    if (colName.Equals("FilaCreditosNombre", StringComparison.OrdinalIgnoreCase)) hasFilaCreditosNombre = true;
                                    if (colName.Equals("FilaCreditosMonto", StringComparison.OrdinalIgnoreCase)) hasFilaCreditosMonto = true;
                                }

                                if (!hasFilaFinal)
                                {
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN FilaFinal INTEGER NOT NULL DEFAULT 129;", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'FilaFinal' agregada exitosamente.");
                                }
                                if (!hasFilaCreditosNombre)
                                {
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN FilaCreditosNombre INTEGER NOT NULL DEFAULT 10;", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'FilaCreditosNombre' agregada exitosamente.");
                                }
                                if (!hasFilaCreditosMonto)
                                {
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN FilaCreditosMonto INTEGER NOT NULL DEFAULT 10;", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'FilaCreditosMonto' agregada exitosamente.");
                                }

                                // Check for FilaVariaCombusNombre, FilaVariaCombusMonto, VariaCombusNombre
                                bool hasFilaVariaCombusNombre = false;
                                bool hasFilaVariaCombusMonto = false;
                                bool hasVariaCombusNombre = false;

                                using (var colCmdVar = new SqliteCommand("PRAGMA table_info(REGISTRO_VENTAS_CONFIGURACION);", connection))
                                using (var readerVar = colCmdVar.ExecuteReader())
                                {
                                    while (readerVar.Read())
                                    {
                                        string colName = readerVar.GetString(1);
                                        if (colName.Equals("FilaVariaCombusNombre", StringComparison.OrdinalIgnoreCase)) hasFilaVariaCombusNombre = true;
                                        if (colName.Equals("FilaVariaCombusMonto", StringComparison.OrdinalIgnoreCase)) hasFilaVariaCombusMonto = true;
                                        if (colName.Equals("VariaCombusNombre", StringComparison.OrdinalIgnoreCase)) hasVariaCombusNombre = true;
                                    }
                                }

                                if (!hasFilaVariaCombusNombre)
                                {
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN FilaVariaCombusNombre INTEGER NOT NULL DEFAULT -1;", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'FilaVariaCombusNombre' agregada exitosamente.");
                                }
                                if (!hasFilaVariaCombusMonto)
                                {
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN FilaVariaCombusMonto INTEGER NOT NULL DEFAULT -1;", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'FilaVariaCombusMonto' agregada exitosamente.");
                                }
                                if (!hasVariaCombusNombre)
                                {
                                    using var alterCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION ADD COLUMN VariaCombusNombre TEXT DEFAULT '';", connection);
                                    alterCmd.ExecuteNonQuery();
                                    Console.WriteLine("Columna 'VariaCombusNombre' agregada exitosamente.");
                                }

                                // Check and migrate Hermes columns
                                bool hasColumnaTablaHermes = false;
                                var columnasExistentes = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

                                using (var colCmd5 = new SqliteCommand("PRAGMA table_info(REGISTRO_VENTAS_CONFIGURACION);", connection))
                                using (var reader5 = colCmd5.ExecuteReader())
                                {
                                    while (reader5.Read())
                                    {
                                        string colName = reader5.GetString(1);
                                        columnasExistentes.Add(colName);
                                        if (colName.Equals("ColumnaTablaHermes", StringComparison.OrdinalIgnoreCase))
                                        {
                                            hasColumnaTablaHermes = true;
                                        }
                                    }
                                }

                                if (hasColumnaTablaHermes)
                                {
                                    using (var transaction = connection.BeginTransaction())
                                    {
                                        try
                                        {
                                            using (var dropOldCmd = new SqliteCommand("DROP TABLE IF EXISTS REGISTRO_VENTAS_CONFIGURACION_OLD;", connection, transaction))
                                            {
                                                dropOldCmd.ExecuteNonQuery();
                                            }

                                            using (var renameCmd = new SqliteCommand("ALTER TABLE REGISTRO_VENTAS_CONFIGURACION RENAME TO REGISTRO_VENTAS_CONFIGURACION_OLD;", connection, transaction))
                                            {
                                                renameCmd.ExecuteNonQuery();
                                            }

                                            string createNewTable = @"
                                                CREATE TABLE REGISTRO_VENTAS_CONFIGURACION (
                                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                    GrifoId INTEGER NOT NULL,
                                                    EESS TEXT DEFAULT '',
                                                    ColumnaEESS TEXT DEFAULT '',
                                                    FilaEESS INTEGER NOT NULL DEFAULT -1,
                                                    ColumnaFecha INTEGER NOT NULL,
                                                    FilaFecha INTEGER NOT NULL DEFAULT 3,
                                                    ColumnaCreditoNombre INTEGER NOT NULL,
                                                    ColumnaCreditoMonto INTEGER NOT NULL,
                                                    ColumnaVariaCombusNombre INTEGER NOT NULL,
                                                    FilaVariaCombusNombre INTEGER NOT NULL DEFAULT -1,
                                                    ColumnaVariaCombusMonto INTEGER NOT NULL,
                                                    FilaVariaCombusMonto INTEGER NOT NULL DEFAULT -1,
                                                    VariaCombusNombre TEXT DEFAULT '',
                                                    ColumnaHermesMonto INTEGER NOT NULL DEFAULT 14,
                                                    FilaHermesMonto INTEGER NOT NULL DEFAULT -1,
                                                    ColumnaHermesBanco INTEGER NOT NULL DEFAULT -1,
                                                    FilaHermesBanco INTEGER NOT NULL DEFAULT -1,
                                                    ColumnaHermesTipo INTEGER NOT NULL DEFAULT -1,
                                                    FilaHermesTipo INTEGER NOT NULL DEFAULT -1,
                                                    HermesPalabraClaveMonto TEXT DEFAULT '',
                                                    FilaFinal INTEGER NOT NULL DEFAULT 129,
                                                    FilaCreditosNombre INTEGER NOT NULL DEFAULT 10,
                                                    FilaCreditosMonto INTEGER NOT NULL DEFAULT 10,
                                                    
                                                    Col_Venta_GPL TEXT,
                                                    Fila_Venta_GPL TEXT,
                                                    Col_Venta_GNV TEXT,
                                                    Fila_Venta_GNV TEXT,
                                                    Col_Total_venta_acumulada TEXT,
                                                    Fila_Total_venta_acumulada TEXT,
                                                    Col_Total_Tarjeta_de_Credito_Liquidos TEXT,
                                                    Fila_Total_Tarjeta_de_Credito_Liquidos TEXT,
                                                    Col_Total_Tarjeta_de_Credito_GLP TEXT,
                                                    Fila_Total_Tarjeta_de_Credito_GLP TEXT,
                                                    Col_Total_Tarjeta_de_Credito_GNV TEXT,
                                                    Fila_Total_Tarjeta_de_Credito_GNV TEXT,
                                                    Col_ErrorMaquina TEXT,
                                                    Fila_ErrorMaquina TEXT,
                                                    Col_Recaudo_Cofide_GNV TEXT,
                                                    Fila_Recaudo_Cofide_GNV TEXT,
                                                    Col_Gastos TEXT,
                                                    Fila_Gastos TEXT,
                                                    Col_Ventas_con_transferencia TEXT,
                                                    Fila_Ventas_con_transferencia TEXT,

                                                    Col_DescuentoLiquidos TEXT,
                                                    Col_DescuentoGLP TEXT,
                                                    Col_Hermes_monto_liquido TEXT,
                                                    Col_Hermes_monto_GLP TEXT,
                                                    Col_Hermes_monto_GNV1 TEXT,
                                                    Col_Hermes_monto_GNV2 TEXT,

                                                    FOREIGN KEY (GrifoId) REFERENCES REGISTRO_VENTAS_GRIFOS (Id) ON DELETE CASCADE
                                                );";
                                            using (var createCmd = new SqliteCommand(createNewTable, connection, transaction))
                                            {
                                                createCmd.ExecuteNonQuery();
                                            }

                                            var targetCols = new System.Collections.Generic.List<string>();
                                            var sourceCols = new System.Collections.Generic.List<string>();
                                            var columnsToMigrate = new System.Collections.Generic.List<string> {
                                                "Id", "GrifoId", "EESS", "ColumnaEESS", "FilaEESS", "ColumnaFecha", "FilaFecha", "ColumnaCreditoNombre", "ColumnaCreditoMonto",
                                                "ColumnaVariaCombusNombre", "FilaVariaCombusNombre", "ColumnaVariaCombusMonto", "FilaVariaCombusMonto", "VariaCombusNombre",
                                                "FilaFinal", "FilaCreditosNombre", "FilaCreditosMonto",
                                                "Col_Venta_GPL", "Fila_Venta_GPL", "Col_Venta_GNV", "Fila_Venta_GNV",
                                                "Col_Total_venta_acumulada", "Fila_Total_venta_acumulada",
                                                "Col_Total_Tarjeta_de_Credito_Liquidos", "Fila_Total_Tarjeta_de_Credito_Liquidos",
                                                "Col_Total_Tarjeta_de_Credito_GLP", "Fila_Total_Tarjeta_de_Credito_GLP",
                                                "Col_Total_Tarjeta_de_Credito_GNV", "Fila_Total_Tarjeta_de_Credito_GNV",
                                                "Col_ErrorMaquina", "Fila_ErrorMaquina", "Col_Recaudo_Cofide_GNV", "Fila_Recaudo_Cofide_GNV",
                                                "Col_Gastos", "Fila_Gastos", "Col_Ventas_con_transferencia", "Fila_Ventas_con_transferencia",
                                                "Col_DescuentoLiquidos", "Col_DescuentoGLP", "Col_Hermes_monto_liquido", "Col_Hermes_monto_GLP",
                                                "Col_Hermes_monto_GNV1", "Col_Hermes_monto_GNV2"
                                            };;

                                            foreach (var col in columnsToMigrate)
                                            {
                                                if (columnasExistentes.Contains(col))
                                                {
                                                    targetCols.Add(col);
                                                    sourceCols.Add(col);
                                                }
                                            }

                                            if (columnasExistentes.Contains("ColumnaTablaHermes"))
                                            {
                                                targetCols.Add("ColumnaHermesMonto");
                                                sourceCols.Add("ColumnaTablaHermes");
                                            }

                                            string copyData = $@"
                                                INSERT INTO REGISTRO_VENTAS_CONFIGURACION (
                                                    {string.Join(", ", targetCols)}
                                                )
                                                SELECT 
                                                    {string.Join(", ", sourceCols)}
                                                FROM REGISTRO_VENTAS_CONFIGURACION_OLD;";

                                            using (var copyCmd = new SqliteCommand(copyData, connection, transaction))
                                            {
                                                copyCmd.ExecuteNonQuery();
                                            }

                                            using (var dropCmd = new SqliteCommand("DROP TABLE REGISTRO_VENTAS_CONFIGURACION_OLD;", connection, transaction))
                                            {
                                                dropCmd.ExecuteNonQuery();
                                            }

                                            transaction.Commit();
                                            Console.WriteLine("Migración de tabla de configuración completada correctamente.");
                                        }
                                        catch (Exception ex)
                                        {
                                            transaction.Rollback();
                                            Console.WriteLine("Error crítico durante la recreación de la tabla de configuración: " + ex.Message);
                                            throw;
                                        }
                                    }
                                }
                            }

                            // Ensure REGISTRO_VENTAS_WRITE table is created dynamically
                            using (var cmdWrite = new SqliteCommand(@"
                                CREATE TABLE IF NOT EXISTS REGISTRO_VENTAS_WRITE (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    GrifoId INTEGER NOT NULL,
                                    NombreHoja TEXT,
                                    FilaSeleccion INTEGER NOT NULL DEFAULT 10,
                                    Venta_GPL TEXT,
                                    Venta_GNV TEXT,
                                    Total_venta_acumulada TEXT,
                                    Total_Tarjeta_de_Credito_Liquidos TEXT,
                                    Total_Tarjeta_de_Credito_GLP TEXT,
                                    Total_Tarjeta_de_Credito_GNV TEXT,
                                    ErrorMaquina TEXT,
                                    Recaudo_Cofide_GNV TEXT,
                                    Gastos TEXT,
                                    Ventas_con_transferencia TEXT,
                                    DescuentoLiquidos TEXT,
                                    DescuentoGLP TEXT,
                                    Hermes_monto_liquido TEXT,
                                    Hermes_monto_GLP TEXT,
                                    Hermes_monto_GNV1 TEXT,
                                    Hermes_monto_GNV2 TEXT,
                                    FOREIGN KEY (GrifoId) REFERENCES REGISTRO_VENTAS_GRIFOS (Id) ON DELETE CASCADE
                                );", connection))
                            {
                                cmdWrite.ExecuteNonQuery();
                            }

                            // Ensure REGISTRO_DESCUENTOS_WRITE table is created dynamically
                            using (var cmdDescWrite = new SqliteCommand(@"
                                CREATE TABLE IF NOT EXISTS REGISTRO_DESCUENTOS_WRITE (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    GrifoId INTEGER NOT NULL,
                                    NombreHoja TEXT,
                                    Plantilla TEXT,
                                    FilaSeleccion INTEGER NOT NULL DEFAULT 10,
                                    ColumnaFecha TEXT,
                                    TarjetaLiquidos TEXT,
                                    TarjetaGLP TEXT,
                                    DescLiquidos TEXT,
                                    DescGLP TEXT,
                                    FOREIGN KEY (GrifoId) REFERENCES REGISTRO_VENTAS_GRIFOS (Id) ON DELETE CASCADE
                                );", connection))
                            {
                                cmdDescWrite.ExecuteNonQuery();
                            }
                        }
                    }
                }

                if (needsSchemaCreation)
                {
                    // Clean recreate to apply dedicated column schema
                    if (dbExists)
                    {
                        try
                        {
                            SqliteConnection.ClearAllPools();
                            File.Delete(dbPath);
                        }
                        catch (Exception)
                        {
                            // If locked, we will just proceed, but clean file deletion is preferred
                        }
                    }
                    
                    InicializarBaseDatos();
                    MigrarJsonSiEsNecesario();
                }

                var newConfigRoot = new ConfigRoot();

                using (var connection = new SqliteConnection(GetConnectionString()))
                {
                    connection.Open();

                    using (var cmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 1. Read all Grifos and their configuration using an INNER JOIN
                    string queryGrifos = @"
                        SELECT g.Nombre, g.Id, c.ColumnaFecha, NULL AS ColumnaTotales, c.ColumnaCreditoNombre, 
                                c.ColumnaCreditoMonto, c.ColumnaVariaCombusNombre, c.ColumnaVariaCombusMonto, 
                                c.ColumnaHermesMonto, c.FilaFecha,
                                c.FilaHermesMonto, c.ColumnaHermesBanco, c.FilaHermesBanco, c.ColumnaHermesTipo, c.FilaHermesTipo, c.HermesPalabraClaveMonto,
                                -- Sequential column and row mapping pairs (16 to 35)
                                c.Col_Venta_GPL, c.Fila_Venta_GPL,
                                c.Col_Venta_GNV, c.Fila_Venta_GNV,
                                c.Col_Total_venta_acumulada, c.Fila_Total_venta_acumulada,
                                c.Col_Total_Tarjeta_de_Credito_Liquidos, c.Fila_Total_Tarjeta_de_Credito_Liquidos,
                                c.Col_Total_Tarjeta_de_Credito_GLP, c.Fila_Total_Tarjeta_de_Credito_GLP,
                                c.Col_Total_Tarjeta_de_Credito_GNV, c.Fila_Total_Tarjeta_de_Credito_GNV,
                                c.Col_ErrorMaquina, c.Fila_ErrorMaquina,
                                c.Col_Recaudo_Cofide_GNV, c.Fila_Recaudo_Cofide_GNV,
                                c.Col_Gastos, c.Fila_Gastos,
                                c.Col_Ventas_con_transferencia, c.Fila_Ventas_con_transferencia,
                                -- Writing only columns (36 to 41)
                                c.Col_DescuentoLiquidos, c.Col_DescuentoGLP,
                                c.Col_Hermes_monto_liquido, c.Col_Hermes_monto_GLP,
                                c.Col_Hermes_monto_GNV1, c.Col_Hermes_monto_GNV2,
                                c.FilaFinal, c.FilaCreditosNombre, c.FilaCreditosMonto,
                                c.FilaVariaCombusNombre, c.FilaVariaCombusMonto, c.VariaCombusNombre,
                                c.ColumnaEESS, c.FilaEESS, c.EESS
                        FROM REGISTRO_VENTAS_GRIFOS g
                        INNER JOIN REGISTRO_VENTAS_CONFIGURACION c ON g.Id = c.GrifoId
                        ORDER BY g.Nombre ASC;";

                    var grifoIdMap = new Dictionary<long, GrifoConfig>();

                    using (var cmd = new SqliteCommand(queryGrifos, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string nombre = reader.GetString(0);
                            long grifoId = reader.GetInt64(1);
                            
                            var grifo = new GrifoConfig
                            {
                                Lectura = new LecturaConfig
                                {
                                    ColumnaFecha = ParseColumnDbValue(reader.GetValue(2), 14),
                                    FilaFecha = reader.IsDBNull(9) ? 3 : reader.GetInt32(9),
                                    ColumnaCreditoNombre = ParseColumnDbValue(reader.GetValue(4), 0),
                                    ColumnaCreditoMonto = ParseColumnDbValue(reader.GetValue(5), 6),
                                    ColumnaVariaCombusNombre = ParseColumnDbValue(reader.GetValue(6), 16),
                                    ColumnaVariaCombusMonto = ParseColumnDbValue(reader.GetValue(7), 18),
                                    ColumnaHermesMonto = ParseColumnDbValue(reader.GetValue(8), 14),
                                    FilaHermesMonto = reader.IsDBNull(10) ? -1 : reader.GetInt32(10),
                                    ColumnaHermesBanco = ParseColumnDbValue(reader.GetValue(11), -1),
                                    FilaHermesBanco = reader.IsDBNull(12) ? -1 : reader.GetInt32(12),
                                    ColumnaHermesTipo = ParseColumnDbValue(reader.GetValue(13), -1),
                                    FilaHermesTipo = reader.IsDBNull(14) ? -1 : reader.GetInt32(14),
                                    HermesPalabraClaveMonto = reader.IsDBNull(15) ? "" : reader.GetString(15),
                                    FilaFinal = reader.IsDBNull(42) ? 129 : reader.GetInt32(42),
                                    FilaCreditosNombre = reader.IsDBNull(43) ? 10 : reader.GetInt32(43),
                                    FilaCreditosMonto = reader.IsDBNull(44) ? 10 : reader.GetInt32(44),
                                    FilaVariaCombusNombre = reader.IsDBNull(45) ? -1 : reader.GetInt32(45),
                                    FilaVariaCombusMonto = reader.IsDBNull(46) ? -1 : reader.GetInt32(46),
                                    VariaCombusNombre = reader.IsDBNull(47) ? "" : reader.GetString(47),
                                    ColumnaEESS = ParseColumnDbValue(reader.GetValue(48), -1),
                                    FilaEESS = reader.IsDBNull(49) ? -1 : reader.GetInt32(49),
                                    EESS = reader.IsDBNull(50) ? "" : reader.GetString(50),
                                    MapeoFilas = new Dictionary<string, string>()
                                },
                                Escritura = new EscrituraConfig
                                {
                                    Columnas = new Dictionary<string, string>()
                                },
                                FilasClientesCreditos = new Dictionary<string, string>()
                            };

                            // Populate MapeoFilas
                            void AddRow(string colVal, string propName)
                            {
                                if (!string.IsNullOrEmpty(colVal) && grifo.Lectura != null)
                                {
                                    grifo.Lectura.MapeoFilas[colVal] = propName;
                                }
                            }
                            
                            AddRow(reader.IsDBNull(17) ? "" : reader.GetString(17), "Venta_GPL");
                            AddRow(reader.IsDBNull(19) ? "" : reader.GetString(19), "Venta_GNV");
                            AddRow(reader.IsDBNull(21) ? "" : reader.GetString(21), "Total_venta_acumulada");
                            AddRow(reader.IsDBNull(23) ? "" : reader.GetString(23), "Total_Tarjeta_de_Credito_Liquidos");
                            AddRow(reader.IsDBNull(25) ? "" : reader.GetString(25), "Total_Tarjeta_de_Credito_GLP");
                            AddRow(reader.IsDBNull(27) ? "" : reader.GetString(27), "Total_Tarjeta_de_Credito_GNV");
                            AddRow(reader.IsDBNull(29) ? "" : reader.GetString(29), "ErrorMaquina");
                            AddRow(reader.IsDBNull(31) ? "" : reader.GetString(31), "Recaudo_Cofide_GNV");
                            AddRow(reader.IsDBNull(33) ? "" : reader.GetString(33), "Gastos");
                            AddRow(reader.IsDBNull(35) ? "" : reader.GetString(35), "Ventas_con_transferencia");

                            // Populate Escritura.Columnas
                            void AddCol(string colVal, string propName)
                            {
                                if (grifo.Escritura != null)
                                {
                                    grifo.Escritura.Columnas[propName] = colVal ?? "";
                                }
                            }

                            AddCol(reader.IsDBNull(16) ? "" : reader.GetString(16), "Venta_GPL");
                            AddCol(reader.IsDBNull(18) ? "" : reader.GetString(18), "Venta_GNV");
                            AddCol(reader.IsDBNull(20) ? "" : reader.GetString(20), "Total_venta_acumulada");
                            AddCol(reader.IsDBNull(22) ? "" : reader.GetString(22), "Total_Tarjeta_de_Credito_Liquidos");
                            AddCol(reader.IsDBNull(24) ? "" : reader.GetString(24), "Total_Tarjeta_de_Credito_GLP");
                            AddCol(reader.IsDBNull(26) ? "" : reader.GetString(26), "Total_Tarjeta_de_Credito_GNV");
                            AddCol(reader.IsDBNull(28) ? "" : reader.GetString(28), "ErrorMaquina");
                            AddCol(reader.IsDBNull(30) ? "" : reader.GetString(30), "Recaudo_Cofide_GNV");
                            AddCol(reader.IsDBNull(32) ? "" : reader.GetString(32), "Gastos");
                            AddCol(reader.IsDBNull(34) ? "" : reader.GetString(34), "Ventas_con_transferencia");
                            AddCol(reader.IsDBNull(36) ? "" : reader.GetString(36), "DescuentoLiquidos");
                            AddCol(reader.IsDBNull(37) ? "" : reader.GetString(37), "DescuentoGLP");
                            AddCol(reader.IsDBNull(38) ? "" : reader.GetString(38), "Hermes_monto_liquido");
                            AddCol(reader.IsDBNull(39) ? "" : reader.GetString(39), "Hermes_monto_GLP");
                            AddCol(reader.IsDBNull(40) ? "" : reader.GetString(40), "Hermes_monto_GNV1");
                            AddCol(reader.IsDBNull(41) ? "" : reader.GetString(41), "Hermes_monto_GNV2");;

                            newConfigRoot.Grifos[nombre] = grifo;
                            grifoIdMap[grifoId] = grifo;
                        }
                    }

                    // 2. Read ClientesCredito and populate FilasClientesCreditos
                    string queryClientes = "SELECT GrifoId, Columna, ClienteNombre FROM REGISTRO_VENTAS_CLIENTE_CREDITOS;";
                    using (var cmd = new SqliteCommand(queryClientes, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long grifoId = reader.GetInt64(0);
                            string columna = reader.GetString(1);
                            string clienteNombre = reader.GetString(2);

                            if (grifoIdMap.TryGetValue(grifoId, out var grifo))
                            {
                                if (grifo.FilasClientesCreditos != null)
                                {
                                    grifo.FilasClientesCreditos[columna] = clienteNombre;
                                }
                            }
                        }
                    }
                }

                _isLoading = true;
                _configGlobal = newConfigRoot;
                _isLoading = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error crítico al cargar configuración desde SQLite de columnas relacionales: {ex.Message}");
            }
        }

        public static List<RegistroVentasGrifo> ObtenerGrifosDB()
        {
            var grifos = new List<RegistroVentasGrifo>();
            try
            {
                using (var connection = new SqliteConnection(GetConnectionString()))
                {
                    connection.Open();

                    using (var cmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 1. Read all Grifos and their configuration using an INNER JOIN
                    string queryGrifos = @"
                        SELECT g.Id, g.Nombre, c.Id, c.ColumnaFecha, NULL AS ColumnaTotales, c.ColumnaCreditoNombre, 
                                c.ColumnaCreditoMonto, c.ColumnaVariaCombusNombre, c.ColumnaVariaCombusMonto, 
                                c.ColumnaHermesMonto, c.FilaFecha,
                                c.FilaHermesMonto, c.ColumnaHermesBanco, c.FilaHermesBanco, c.ColumnaHermesTipo, c.FilaHermesTipo, c.HermesPalabraClaveMonto,
                                -- Sequential column and row mapping pairs (17 to 36)
                                c.Col_Venta_GPL, c.Fila_Venta_GPL,
                                c.Col_Venta_GNV, c.Fila_Venta_GNV,
                                c.Col_Total_venta_acumulada, c.Fila_Total_venta_acumulada,
                                c.Col_Total_Tarjeta_de_Credito_Liquidos, c.Fila_Total_Tarjeta_de_Credito_Liquidos,
                                c.Col_Total_Tarjeta_de_Credito_GLP, c.Fila_Total_Tarjeta_de_Credito_GLP,
                                c.Col_Total_Tarjeta_de_Credito_GNV, c.Fila_Total_Tarjeta_de_Credito_GNV,
                                c.Col_ErrorMaquina, c.Fila_ErrorMaquina,
                                c.Col_Recaudo_Cofide_GNV, c.Fila_Recaudo_Cofide_GNV,
                                c.Col_Gastos, c.Fila_Gastos,
                                c.Col_Ventas_con_transferencia, c.Fila_Ventas_con_transferencia,
                                -- Writing only columns (37 to 42)
                                c.Col_DescuentoLiquidos, c.Col_DescuentoGLP,
                                c.Col_Hermes_monto_liquido, c.Col_Hermes_monto_GLP,
                                c.Col_Hermes_monto_GNV1, c.Col_Hermes_monto_GNV2,
                                c.FilaFinal, c.FilaCreditosNombre, c.FilaCreditosMonto,
                                g.Plantilla,
                                -- REGISTRO_VENTAS_WRITE columns (47 to 66)
                                w.NombreHoja, w.FilaSeleccion, w.Venta_GPL, w.Venta_GNV, w.Total_venta_acumulada, 
                                w.Total_Tarjeta_de_Credito_Liquidos, w.Total_Tarjeta_de_Credito_GLP, w.Total_Tarjeta_de_Credito_GNV, 
                                w.ErrorMaquina, w.Recaudo_Cofide_GNV, w.Gastos, w.Ventas_con_transferencia, 
                                w.DescuentoLiquidos, w.DescuentoGLP, w.Hermes_monto_liquido, w.Hermes_monto_GLP, 
                                w.Hermes_monto_GNV1, w.Hermes_monto_GNV2, w.Id,
                                c.FilaVariaCombusNombre, c.FilaVariaCombusMonto, c.VariaCombusNombre,
                                c.ColumnaEESS, c.FilaEESS, c.EESS,
                                -- REGISTRO_DESCUENTOS_WRITE columns (72 to 81)
                                dw.Id, dw.NombreHoja, dw.Plantilla, dw.FilaSeleccion, dw.ColumnaFecha,
                                dw.TarjetaLiquidos, dw.TarjetaGLP, dw.DescLiquidos, dw.DescGLP, dw.TarjetaGNV
                        FROM REGISTRO_VENTAS_GRIFOS g
                        INNER JOIN REGISTRO_VENTAS_CONFIGURACION c ON g.Id = c.GrifoId
                        LEFT JOIN REGISTRO_VENTAS_WRITE w ON g.Id = w.GrifoId
                        LEFT JOIN REGISTRO_DESCUENTOS_WRITE dw ON g.Id = dw.GrifoId
                        ORDER BY g.Nombre ASC;";

                    var grifoMap = new Dictionary<int, RegistroVentasGrifo>();

                    using (var cmd = new SqliteCommand(queryGrifos, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int gId = reader.GetInt32(0);
                            string gNombre = reader.GetString(1);
                            
                            var grifo = new RegistroVentasGrifo
                            {
                                Id = gId,
                                Nombre = gNombre,
                                Plantilla = reader.IsDBNull(46) ? "" : reader.GetString(46),
                                Configuracion = new RegistroVentasConfiguracion
                                {
                                    Id = reader.GetInt32(2),
                                    GrifoId = gId,
                                    ColumnaFecha = ParseColumnDbValue(reader.GetValue(3), 14),
                                    FilaFecha = reader.IsDBNull(10) ? 3 : reader.GetInt32(10),
                                    ColumnaCreditoNombre = ParseColumnDbValue(reader.GetValue(5), 0),
                                    ColumnaCreditoMonto = ParseColumnDbValue(reader.GetValue(6), 6),
                                    ColumnaVariaCombusNombre = ParseColumnDbValue(reader.GetValue(7), 16),
                                    ColumnaVariaCombusMonto = ParseColumnDbValue(reader.GetValue(8), 18),
                                    ColumnaHermesMonto = ParseColumnDbValue(reader.GetValue(9), 14),
                                    FilaHermesMonto = reader.IsDBNull(11) ? -1 : reader.GetInt32(11),
                                    ColumnaHermesBanco = ParseColumnDbValue(reader.GetValue(12), -1),
                                    FilaHermesBanco = reader.IsDBNull(13) ? -1 : reader.GetInt32(13),
                                    ColumnaHermesTipo = ParseColumnDbValue(reader.GetValue(14), -1),
                                    FilaHermesTipo = reader.IsDBNull(15) ? -1 : reader.GetInt32(15),
                                    HermesPalabraClaveMonto = reader.IsDBNull(16) ? "" : reader.GetString(16),
                                    FilaFinal = reader.IsDBNull(43) ? 129 : reader.GetInt32(43),
                                    FilaCreditosNombre = reader.IsDBNull(44) ? 10 : reader.GetInt32(44),
                                    FilaCreditosMonto = reader.IsDBNull(45) ? 10 : reader.GetInt32(45),
                                    FilaVariaCombusNombre = reader.IsDBNull(66) ? -1 : reader.GetInt32(66),
                                    FilaVariaCombusMonto = reader.IsDBNull(67) ? -1 : reader.GetInt32(67),
                                    VariaCombusNombre = reader.IsDBNull(68) ? "" : reader.GetString(68),
                                    ColumnaEESS = ParseColumnDbValue(reader.GetValue(69), -1),
                                    FilaEESS = reader.IsDBNull(70) ? -1 : reader.GetInt32(70),
                                    EESS = reader.IsDBNull(71) ? "" : reader.GetString(71),
                                    
                                    // Sequential column and row mapping pairs
                                    Col_Venta_GPL = reader.IsDBNull(17) ? "" : reader.GetString(17),
                                    Fila_Venta_GPL = reader.IsDBNull(18) ? "" : reader.GetString(18),
                                    Col_Venta_GNV = reader.IsDBNull(19) ? "" : reader.GetString(19),
                                    Fila_Venta_GNV = reader.IsDBNull(20) ? "" : reader.GetString(20),
                                    Col_Total_venta_acumulada = reader.IsDBNull(21) ? "" : reader.GetString(21),
                                    Fila_Total_venta_acumulada = reader.IsDBNull(22) ? "" : reader.GetString(22),
                                    Col_Total_Tarjeta_de_Credito_Liquidos = reader.IsDBNull(23) ? "" : reader.GetString(23),
                                    Fila_Total_Tarjeta_de_Credito_Liquidos = reader.IsDBNull(24) ? "" : reader.GetString(24),
                                    Col_Total_Tarjeta_de_Credito_GLP = reader.IsDBNull(25) ? "" : reader.GetString(25),
                                    Fila_Total_Tarjeta_de_Credito_GLP = reader.IsDBNull(26) ? "" : reader.GetString(26),
                                    Col_Total_Tarjeta_de_Credito_GNV = reader.IsDBNull(27) ? "" : reader.GetString(27),
                                    Fila_Total_Tarjeta_de_Credito_GNV = reader.IsDBNull(28) ? "" : reader.GetString(28),
                                    Col_ErrorMaquina = reader.IsDBNull(29) ? "" : reader.GetString(29),
                                    Fila_ErrorMaquina = reader.IsDBNull(30) ? "" : reader.GetString(30),
                                    Col_Recaudo_Cofide_GNV = reader.IsDBNull(31) ? "" : reader.GetString(31),
                                    Fila_Recaudo_Cofide_GNV = reader.IsDBNull(32) ? "" : reader.GetString(32),
                                    Col_Gastos = reader.IsDBNull(33) ? "" : reader.GetString(33),
                                    Fila_Gastos = reader.IsDBNull(34) ? "" : reader.GetString(34),
                                    Col_Ventas_con_transferencia = reader.IsDBNull(35) ? "" : reader.GetString(35),
                                    Fila_Ventas_con_transferencia = reader.IsDBNull(36) ? "" : reader.GetString(36),
                                    
                                    // Writing only columns
                                    Col_DescuentoLiquidos = reader.IsDBNull(37) ? "" : reader.GetString(37),
                                    Col_DescuentoGLP = reader.IsDBNull(38) ? "" : reader.GetString(38),
                                    Col_Hermes_monto_liquido = reader.IsDBNull(39) ? "" : reader.GetString(39),
                                    Col_Hermes_monto_GLP = reader.IsDBNull(40) ? "" : reader.GetString(40),
                                    Col_Hermes_monto_GNV1 = reader.IsDBNull(41) ? "" : reader.GetString(41),
                                    Col_Hermes_monto_GNV2 = reader.IsDBNull(42) ? "" : reader.GetString(42)
                                },
                                RegistroVentasWrite = new RegistroVentasWriteConfig
                                {
                                    GrifoId = gId,
                                    NombreHoja = reader.IsDBNull(47) ? "" : reader.GetString(47),
                                    FilaSeleccion = reader.IsDBNull(48) ? 10 : reader.GetInt32(48),
                                    Venta_GPL = reader.IsDBNull(49) ? "" : reader.GetString(49),
                                    Venta_GNV = reader.IsDBNull(50) ? "" : reader.GetString(50),
                                    Total_venta_acumulada = reader.IsDBNull(51) ? "" : reader.GetString(51),
                                    Total_Tarjeta_de_Credito_Liquidos = reader.IsDBNull(52) ? "" : reader.GetString(52),
                                    Total_Tarjeta_de_Credito_GLP = reader.IsDBNull(53) ? "" : reader.GetString(53),
                                    Total_Tarjeta_de_Credito_GNV = reader.IsDBNull(54) ? "" : reader.GetString(54),
                                    ErrorMaquina = reader.IsDBNull(55) ? "" : reader.GetString(55),
                                    Recaudo_Cofide_GNV = reader.IsDBNull(56) ? "" : reader.GetString(56),
                                    Gastos = reader.IsDBNull(57) ? "" : reader.GetString(57),
                                    Ventas_con_transferencia = reader.IsDBNull(58) ? "" : reader.GetString(58),
                                    DescuentoLiquidos = reader.IsDBNull(59) ? "" : reader.GetString(59),
                                    DescuentoGLP = reader.IsDBNull(60) ? "" : reader.GetString(60),
                                    Hermes_monto_liquido = reader.IsDBNull(61) ? "" : reader.GetString(61),
                                    Hermes_monto_GLP = reader.IsDBNull(62) ? "" : reader.GetString(62),
                                    Hermes_monto_GNV1 = reader.IsDBNull(63) ? "" : reader.GetString(63),
                                    Hermes_monto_GNV2 = reader.IsDBNull(64) ? "" : reader.GetString(64),
                                    Id = reader.IsDBNull(65) ? 0 : reader.GetInt32(65)
                                },
                                RegistroDescuentosWrite = new RegistroDescuentosWriteConfig
                                {
                                    Id = reader.IsDBNull(72) ? 0 : reader.GetInt32(72),
                                    GrifoId = gId,
                                    NombreHoja = reader.IsDBNull(73) ? "" : reader.GetString(73),
                                    Plantilla = reader.IsDBNull(74) ? "" : reader.GetString(74),
                                    FilaSeleccion = reader.IsDBNull(75) ? 10 : reader.GetInt32(75),
                                    ColumnaFecha = reader.IsDBNull(76) ? "" : reader.GetString(76),
                                    TarjetaLiquidos = reader.IsDBNull(77) ? "" : reader.GetString(77),
                                    TarjetaGLP = reader.IsDBNull(78) ? "" : reader.GetString(78),
                                    DescLiquidos = reader.IsDBNull(79) ? "" : reader.GetString(79),
                                    DescGLP = reader.IsDBNull(80) ? "" : reader.GetString(80),
                                    TarjetaGNV = reader.IsDBNull(81) ? "" : reader.GetString(81)
                                }
                            };
                            grifos.Add(grifo);
                            grifoMap[gId] = grifo;
                        }
                    }

                    // 2. Read ClientesCredito
                    string queryClientes = "SELECT Id, GrifoId, Columna, ClienteNombre FROM REGISTRO_VENTAS_CLIENTE_CREDITOS;";
                    using (var cmd = new SqliteCommand(queryClientes, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int cId = reader.GetInt32(0);
                            int grifoId = reader.GetInt32(1);
                            string columna = reader.GetString(2);
                            string clienteNombre = reader.GetString(3);

                            if (grifoMap.TryGetValue(grifoId, out var grifo))
                            {
                                grifo.ClientesCredito.Add(new RegistroVentasClienteCredito
                                {
                                    Id = cId,
                                    GrifoId = grifoId,
                                    Columna = columna,
                                    ClienteNombre = clienteNombre
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener grifos de la BD: {ex.Message}");
            }
            return grifos;
        }

        public static void GuardarGrifoCompletoDB(RegistroVentasGrifo grifo)
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. If Id exists, update name and Plantilla. Else, insert new grifo.
                long grifoId = grifo.Id;
                if (grifo.Id > 0)
                {
                    string updateGrifo = "UPDATE REGISTRO_VENTAS_GRIFOS SET Nombre = @Nombre, Plantilla = @Plantilla WHERE Id = @Id;";
                    using (var cmd = new SqliteCommand(updateGrifo, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", grifo.Nombre.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@Plantilla", grifo.Plantilla ?? "");
                        cmd.Parameters.AddWithValue("@Id", grifo.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    string insertGrifo = "INSERT INTO REGISTRO_VENTAS_GRIFOS (Nombre, Plantilla) VALUES (@Nombre, @Plantilla);";
                    using (var cmd = new SqliteCommand(insertGrifo, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", grifo.Nombre.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@Plantilla", grifo.Plantilla ?? "");
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SqliteCommand("SELECT last_insert_rowid();", connection, transaction))
                    {
                        grifoId = (long)(cmd.ExecuteScalar() ?? 0L);
                    }
                    grifo.Id = (int)grifoId;
                }

                // 2. Insert or replace Configuration for this GrifoId
                string insertConfig = @"
                    INSERT INTO REGISTRO_VENTAS_CONFIGURACION (
                        GrifoId, EESS, ColumnaEESS, FilaEESS, ColumnaFecha, FilaFecha, ColumnaCreditoNombre, 
                        ColumnaCreditoMonto, ColumnaVariaCombusNombre, FilaVariaCombusNombre, ColumnaVariaCombusMonto, FilaVariaCombusMonto, VariaCombusNombre, ColumnaHermesMonto,
                        FilaHermesMonto, ColumnaHermesBanco, FilaHermesBanco, ColumnaHermesTipo, FilaHermesTipo, HermesPalabraClaveMonto,
                        FilaFinal, FilaCreditosNombre, FilaCreditosMonto,
                        
                        -- Sequential column and row mapping pairs
                        Col_Venta_GPL, Fila_Venta_GPL,
                        Col_Venta_GNV, Fila_Venta_GNV,
                        Col_Total_venta_acumulada, Fila_Total_venta_acumulada,
                        Col_Total_Tarjeta_de_Credito_Liquidos, Fila_Total_Tarjeta_de_Credito_Liquidos,
                        Col_Total_Tarjeta_de_Credito_GLP, Fila_Total_Tarjeta_de_Credito_GLP,
                        Col_Total_Tarjeta_de_Credito_GNV, Fila_Total_Tarjeta_de_Credito_GNV,
                        Col_ErrorMaquina, Fila_ErrorMaquina,
                        Col_Recaudo_Cofide_GNV, Fila_Recaudo_Cofide_GNV,
                        Col_Gastos, Fila_Gastos,
                        Col_Ventas_con_transferencia, Fila_Ventas_con_transferencia,
                        
                        -- Writing only columns
                        Col_DescuentoLiquidos, Col_DescuentoGLP,
                        Col_Hermes_monto_liquido, Col_Hermes_monto_GLP,
                        Col_Hermes_monto_GNV1, Col_Hermes_monto_GNV2
                    ) VALUES (
                        @GrifoId, @EESS, @ColumnaEESS, @FilaEESS, @ColumnaFecha, @FilaFecha, @ColumnaCreditoNombre, 
                        @ColumnaCreditoMonto, @ColumnaVariaCombusNombre, @FilaVariaCombusNombre, @ColumnaVariaCombusMonto, @FilaVariaCombusMonto, @VariaCombusNombre, @ColumnaHermesMonto,
                        @FilaHermesMonto, @ColumnaHermesBanco, @FilaHermesBanco, @ColumnaHermesTipo, @FilaHermesTipo, @HermesPalabraClaveMonto,
                        @FilaFinal, @FilaCreditosNombre, @FilaCreditosMonto,
                        
                        -- Pairs
                        @Col_Venta_GPL, @Fila_Venta_GPL,
                        @Col_Venta_GNV, @Fila_Venta_GNV,
                        @Col_Total_venta_acumulada, @Fila_Total_venta_acumulada,
                        @Col_Total_Tarjeta_de_Credito_Liquidos, @Fila_Total_Tarjeta_de_Credito_Liquidos,
                        @Col_Total_Tarjeta_de_Credito_GLP, @Fila_Total_Tarjeta_de_Credito_GLP,
                        @Col_Total_Tarjeta_de_Credito_GNV, @Fila_Total_Tarjeta_de_Credito_GNV,
                        @Col_ErrorMaquina, @Fila_ErrorMaquina,
                        @Col_Recaudo_Cofide_GNV, @Fila_Recaudo_Cofide_GNV,
                        @Col_Gastos, @Fila_Gastos,
                        @Col_Ventas_con_transferencia, @Fila_Ventas_con_transferencia,
                        
                        -- Writing only
                        @Col_DescuentoLiquidos, @Col_DescuentoGLP,
                        @Col_Hermes_monto_liquido, @Col_Hermes_monto_GLP,
                        @Col_Hermes_monto_GNV1, @Col_Hermes_monto_GNV2
                    );";

                using (var cmd = new SqliteCommand("DELETE FROM REGISTRO_VENTAS_CONFIGURACION WHERE GrifoId = @GrifoId;", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.ExecuteNonQuery();
                }

                var c = grifo.Configuracion;
                using (var cmd = new SqliteCommand(insertConfig, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.Parameters.AddWithValue("@EESS", c.EESS ?? "");
                    cmd.Parameters.AddWithValue("@ColumnaEESS", GetExcelColumnName(c.ColumnaEESS));
                    cmd.Parameters.AddWithValue("@FilaEESS", c.FilaEESS);
                    cmd.Parameters.AddWithValue("@ColumnaFecha", GetExcelColumnName(c.ColumnaFecha));
                    cmd.Parameters.AddWithValue("@FilaFecha", c.FilaFecha);
                    cmd.Parameters.AddWithValue("@ColumnaCreditoNombre", GetExcelColumnName(c.ColumnaCreditoNombre));
                    cmd.Parameters.AddWithValue("@ColumnaCreditoMonto", GetExcelColumnName(c.ColumnaCreditoMonto));
                    cmd.Parameters.AddWithValue("@ColumnaVariaCombusNombre", GetExcelColumnName(c.ColumnaVariaCombusNombre));
                    cmd.Parameters.AddWithValue("@FilaVariaCombusNombre", c.FilaVariaCombusNombre);
                    cmd.Parameters.AddWithValue("@ColumnaVariaCombusMonto", GetExcelColumnName(c.ColumnaVariaCombusMonto));
                    cmd.Parameters.AddWithValue("@FilaVariaCombusMonto", c.FilaVariaCombusMonto);
                    cmd.Parameters.AddWithValue("@VariaCombusNombre", c.VariaCombusNombre ?? "");
                    cmd.Parameters.AddWithValue("@ColumnaHermesMonto", GetExcelColumnName(c.ColumnaHermesMonto));
                    cmd.Parameters.AddWithValue("@FilaHermesMonto", c.FilaHermesMonto);
                    cmd.Parameters.AddWithValue("@ColumnaHermesBanco", GetExcelColumnName(c.ColumnaHermesBanco));
                    cmd.Parameters.AddWithValue("@FilaHermesBanco", c.FilaHermesBanco);
                    cmd.Parameters.AddWithValue("@ColumnaHermesTipo", GetExcelColumnName(c.ColumnaHermesTipo));
                    cmd.Parameters.AddWithValue("@FilaHermesTipo", c.FilaHermesTipo);
                    cmd.Parameters.AddWithValue("@HermesPalabraClaveMonto", c.HermesPalabraClaveMonto ?? "");
                    cmd.Parameters.AddWithValue("@FilaFinal", c.FilaFinal);
                    cmd.Parameters.AddWithValue("@FilaCreditosNombre", c.FilaCreditosNombre);
                    cmd.Parameters.AddWithValue("@FilaCreditosMonto", c.FilaCreditosMonto);
                    
                    cmd.Parameters.AddWithValue("@Col_Venta_GPL", c.Col_Venta_GPL ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Venta_GPL", c.Fila_Venta_GPL ?? "");
                    cmd.Parameters.AddWithValue("@Col_Venta_GNV", c.Col_Venta_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Venta_GNV", c.Fila_Venta_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Col_Total_venta_acumulada", c.Col_Total_venta_acumulada ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Total_venta_acumulada", c.Fila_Total_venta_acumulada ?? "");
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_Liquidos", c.Col_Total_Tarjeta_de_Credito_Liquidos ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_Liquidos", c.Fila_Total_Tarjeta_de_Credito_Liquidos ?? "");
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_GLP", c.Col_Total_Tarjeta_de_Credito_GLP ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_GLP", c.Fila_Total_Tarjeta_de_Credito_GLP ?? "");
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_GNV", c.Col_Total_Tarjeta_de_Credito_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_GNV", c.Fila_Total_Tarjeta_de_Credito_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Col_ErrorMaquina", c.Col_ErrorMaquina ?? "");
                    cmd.Parameters.AddWithValue("@Fila_ErrorMaquina", c.Fila_ErrorMaquina ?? "");
                    cmd.Parameters.AddWithValue("@Col_Recaudo_Cofide_GNV", c.Col_Recaudo_Cofide_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Recaudo_Cofide_GNV", c.Fila_Recaudo_Cofide_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Col_Gastos", c.Col_Gastos ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Gastos", c.Fila_Gastos ?? "");
                    cmd.Parameters.AddWithValue("@Col_Ventas_con_transferencia", c.Col_Ventas_con_transferencia ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Ventas_con_transferencia", c.Fila_Ventas_con_transferencia ?? "");

                    cmd.Parameters.AddWithValue("@Col_DescuentoLiquidos", c.Col_DescuentoLiquidos ?? "");
                    cmd.Parameters.AddWithValue("@Col_DescuentoGLP", c.Col_DescuentoGLP ?? "");
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_liquido", c.Col_Hermes_monto_liquido ?? "");
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GLP", c.Col_Hermes_monto_GLP ?? "");
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GNV1", c.Col_Hermes_monto_GNV1 ?? "");
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GNV2", c.Col_Hermes_monto_GNV2 ?? "");

                    cmd.ExecuteNonQuery();
                }

                // 2.5 Save REGISTRO_VENTAS_WRITE config
                string insertWriteConfig = @"
                    INSERT INTO REGISTRO_VENTAS_WRITE (
                        GrifoId, NombreHoja, FilaSeleccion,
                        Venta_GPL, Venta_GNV, Total_venta_acumulada,
                        Total_Tarjeta_de_Credito_Liquidos, Total_Tarjeta_de_Credito_GLP, Total_Tarjeta_de_Credito_GNV,
                        ErrorMaquina, Recaudo_Cofide_GNV, Gastos, Ventas_con_transferencia,
                        DescuentoLiquidos, DescuentoGLP, Hermes_monto_liquido, Hermes_monto_GLP,
                        Hermes_monto_GNV1, Hermes_monto_GNV2
                    ) VALUES (
                        @GrifoId, @NombreHoja, @FilaSeleccion,
                        @Venta_GPL, @Venta_GNV, @Total_venta_acumulada,
                        @Total_Tarjeta_de_Credito_Liquidos, @Total_Tarjeta_de_Credito_GLP, @Total_Tarjeta_de_Credito_GNV,
                        @ErrorMaquina, @Recaudo_Cofide_GNV, @Gastos, @Ventas_con_transferencia,
                        @DescuentoLiquidos, @DescuentoGLP, @Hermes_monto_liquido, @Hermes_monto_GLP,
                        @Hermes_monto_GNV1, @Hermes_monto_GNV2
                    );";

                using (var cmd = new SqliteCommand("DELETE FROM REGISTRO_VENTAS_WRITE WHERE GrifoId = @GrifoId;", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.ExecuteNonQuery();
                }

                var w = grifo.RegistroVentasWrite ?? new RegistroVentasWriteConfig();
                using (var cmd = new SqliteCommand(insertWriteConfig, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.Parameters.AddWithValue("@NombreHoja", w.NombreHoja ?? "");
                    cmd.Parameters.AddWithValue("@FilaSeleccion", w.FilaSeleccion);
                    cmd.Parameters.AddWithValue("@Venta_GPL", w.Venta_GPL ?? "");
                    cmd.Parameters.AddWithValue("@Venta_GNV", w.Venta_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Total_venta_acumulada", w.Total_venta_acumulada ?? "");
                    cmd.Parameters.AddWithValue("@Total_Tarjeta_de_Credito_Liquidos", w.Total_Tarjeta_de_Credito_Liquidos ?? "");
                    cmd.Parameters.AddWithValue("@Total_Tarjeta_de_Credito_GLP", w.Total_Tarjeta_de_Credito_GLP ?? "");
                    cmd.Parameters.AddWithValue("@Total_Tarjeta_de_Credito_GNV", w.Total_Tarjeta_de_Credito_GNV ?? "");
                    cmd.Parameters.AddWithValue("@ErrorMaquina", w.ErrorMaquina ?? "");
                    cmd.Parameters.AddWithValue("@Recaudo_Cofide_GNV", w.Recaudo_Cofide_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Gastos", w.Gastos ?? "");
                    cmd.Parameters.AddWithValue("@Ventas_con_transferencia", w.Ventas_con_transferencia ?? "");
                    cmd.Parameters.AddWithValue("@DescuentoLiquidos", w.DescuentoLiquidos ?? "");
                    cmd.Parameters.AddWithValue("@DescuentoGLP", w.DescuentoGLP ?? "");
                    cmd.Parameters.AddWithValue("@Hermes_monto_liquido", w.Hermes_monto_liquido ?? "");
                    cmd.Parameters.AddWithValue("@Hermes_monto_GLP", w.Hermes_monto_GLP ?? "");
                    cmd.Parameters.AddWithValue("@Hermes_monto_GNV1", w.Hermes_monto_GNV1 ?? "");
                    cmd.Parameters.AddWithValue("@Hermes_monto_GNV2", w.Hermes_monto_GNV2 ?? "");
                    cmd.ExecuteNonQuery();
                }

                // 2.6 Save REGISTRO_DESCUENTOS_WRITE config
                string insertDescWriteConfig = @"
                    INSERT INTO REGISTRO_DESCUENTOS_WRITE (
                        GrifoId, NombreHoja, Plantilla, FilaSeleccion,
                        ColumnaFecha, TarjetaLiquidos, TarjetaGLP, DescLiquidos, DescGLP, TarjetaGNV
                    ) VALUES (
                        @GrifoId, @NombreHoja, @Plantilla, @FilaSeleccion,
                        @ColumnaFecha, @TarjetaLiquidos, @TarjetaGLP, @DescLiquidos, @DescGLP, @TarjetaGNV
                    );";

                using (var cmd = new SqliteCommand("DELETE FROM REGISTRO_DESCUENTOS_WRITE WHERE GrifoId = @GrifoId;", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.ExecuteNonQuery();
                }

                var dw = grifo.RegistroDescuentosWrite ?? new RegistroDescuentosWriteConfig();
                using (var cmd = new SqliteCommand(insertDescWriteConfig, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.Parameters.AddWithValue("@NombreHoja", dw.NombreHoja ?? "");
                    cmd.Parameters.AddWithValue("@Plantilla", dw.Plantilla ?? "");
                    cmd.Parameters.AddWithValue("@FilaSeleccion", dw.FilaSeleccion);
                    cmd.Parameters.AddWithValue("@ColumnaFecha", dw.ColumnaFecha ?? "");
                    cmd.Parameters.AddWithValue("@TarjetaLiquidos", dw.TarjetaLiquidos ?? "");
                    cmd.Parameters.AddWithValue("@TarjetaGLP", dw.TarjetaGLP ?? "");
                    cmd.Parameters.AddWithValue("@DescLiquidos", dw.DescLiquidos ?? "");
                    cmd.Parameters.AddWithValue("@DescGLP", dw.DescGLP ?? "");
                    cmd.Parameters.AddWithValue("@TarjetaGNV", dw.TarjetaGNV ?? "");
                    cmd.ExecuteNonQuery();
                }

                // 3. Clear old Clientes and insert new ones
                using (var cmd = new SqliteCommand("DELETE FROM REGISTRO_VENTAS_CLIENTE_CREDITOS WHERE GrifoId = @GrifoId;", connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                    cmd.ExecuteNonQuery();
                }

                if (grifo.ClientesCredito != null)
                {
                    string insertCliente = "INSERT INTO REGISTRO_VENTAS_CLIENTE_CREDITOS (GrifoId, Columna, ClienteNombre) VALUES (@GrifoId, @Columna, @ClienteNombre);";
                    foreach (var cli in grifo.ClientesCredito)
                    {
                        using var cmd = new SqliteCommand(insertCliente, connection, transaction);
                        cmd.Parameters.AddWithValue("@GrifoId", grifoId);
                        cmd.Parameters.AddWithValue("@Columna", cli.Columna);
                        cmd.Parameters.AddWithValue("@ClienteNombre", cli.ClienteNombre ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public static void EliminarGrifoPorIdDB(int id)
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();
            using var cmd = new SqliteCommand("DELETE FROM REGISTRO_VENTAS_GRIFOS WHERE Id = @Id;", connection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }
    }
}
