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
                    ColumnaFecha INTEGER NOT NULL,
                    ColumnaTotales INTEGER NOT NULL,
                    ColumnaCreditoNombre INTEGER NOT NULL,
                    ColumnaCreditoMonto INTEGER NOT NULL,
                    ColumnaVariaCombusNombre INTEGER NOT NULL,
                    ColumnaVariaCombusMonto INTEGER NOT NULL,
                    ColumnaTablaHermes INTEGER NOT NULL,
                    
                    -- Fila mappings as dedicated columns
                    Fila_Venta_GPL TEXT,
                    Fila_Venta_GNV TEXT,
                    Fila_Total_venta_acumulada TEXT,
                    Fila_Total_Tarjeta_de_Credito_Liquidos TEXT,
                    Fila_Total_Tarjeta_de_Credito_GLP TEXT,
                    Fila_Total_Tarjeta_de_Credito_GNV TEXT,
                    Fila_ErrorMaquina TEXT,
                    Fila_Recaudo_Cofide_GNV TEXT,
                    Fila_Gastos TEXT,
                    Fila_Ventas_con_transferencia TEXT,

                    -- Col writing mappings as dedicated columns
                    Col_Venta_GPL TEXT,
                    Col_Venta_GNV TEXT,
                    Col_Total_venta_acumulada TEXT,
                    Col_Total_Tarjeta_de_Credito_Liquidos TEXT,
                    Col_Total_Tarjeta_de_Credito_GLP TEXT,
                    Col_Total_Tarjeta_de_Credito_GNV TEXT,
                    Col_ErrorMaquina TEXT,
                    Col_Recaudo_Cofide_GNV TEXT,
                    Col_Gastos TEXT,
                    Col_Ventas_con_transferencia TEXT,
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

            using (var cmd = new SqliteCommand(createGrifosTable, connection)) cmd.ExecuteNonQuery();
            using (var cmd = new SqliteCommand(createConfiguracionTable, connection)) cmd.ExecuteNonQuery();
            using (var cmd = new SqliteCommand(createClientesTable, connection)) cmd.ExecuteNonQuery();
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
                        GrifoId, ColumnaFecha, ColumnaTotales, ColumnaCreditoNombre, 
                        ColumnaCreditoMonto, ColumnaVariaCombusNombre, ColumnaVariaCombusMonto, ColumnaTablaHermes,
                        -- Rows
                        Fila_Venta_GPL, Fila_Venta_GNV, Fila_Total_venta_acumulada, Fila_Total_Tarjeta_de_Credito_Liquidos,
                        Fila_Total_Tarjeta_de_Credito_GLP, Fila_Total_Tarjeta_de_Credito_GNV, Fila_ErrorMaquina,
                        Fila_Recaudo_Cofide_GNV, Fila_Gastos, Fila_Ventas_con_transferencia,
                        -- Cols
                        Col_Venta_GPL, Col_Venta_GNV, Col_Total_venta_acumulada, Col_Total_Tarjeta_de_Credito_Liquidos,
                        Col_Total_Tarjeta_de_Credito_GLP, Col_Total_Tarjeta_de_Credito_GNV, Col_ErrorMaquina,
                        Col_Recaudo_Cofide_GNV, Col_Gastos, Col_Ventas_con_transferencia, Col_DescuentoLiquidos,
                        Col_DescuentoGLP, Col_Hermes_monto_liquido, Col_Hermes_monto_GLP, Col_Hermes_monto_GNV1,
                        Col_Hermes_monto_GNV2
                    ) VALUES (
                        @GrifoId, @ColumnaFecha, @ColumnaTotales, @ColumnaCreditoNombre, 
                        @ColumnaCreditoMonto, @ColumnaVariaCombusNombre, @ColumnaVariaCombusMonto, @ColumnaTablaHermes,
                        -- Rows
                        @Fila_Venta_GPL, @Fila_Venta_GNV, @Fila_Total_venta_acumulada, @Fila_Total_Tarjeta_de_Credito_Liquidos,
                        @Fila_Total_Tarjeta_de_Credito_GLP, @Fila_Total_Tarjeta_de_Credito_GNV, @Fila_ErrorMaquina,
                        @Fila_Recaudo_Cofide_GNV, @Fila_Gastos, @Fila_Ventas_con_transferencia,
                        -- Cols
                        @Col_Venta_GPL, @Col_Venta_GNV, @Col_Total_venta_acumulada, @Col_Total_Tarjeta_de_Credito_Liquidos,
                        @Col_Total_Tarjeta_de_Credito_GLP, @Col_Total_Tarjeta_de_Credito_GNV, @Col_ErrorMaquina,
                        @Col_Recaudo_Cofide_GNV, @Col_Gastos, @Col_Ventas_con_transferencia, @Col_DescuentoLiquidos,
                        @Col_DescuentoGLP, @Col_Hermes_monto_liquido, @Col_Hermes_monto_GLP, @Col_Hermes_monto_GNV1,
                        @Col_Hermes_monto_GNV2
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
                    cmd.Parameters.AddWithValue("@ColumnaFecha", config.Lectura?.ColumnaFecha ?? 14);
                    cmd.Parameters.AddWithValue("@ColumnaTotales", config.Lectura?.ColumnaTotales ?? 15);
                    cmd.Parameters.AddWithValue("@ColumnaCreditoNombre", config.Lectura?.ColumnaCreditoNombre ?? 0);
                    cmd.Parameters.AddWithValue("@ColumnaCreditoMonto", config.Lectura?.ColumnaCreditoMonto ?? 6);
                    cmd.Parameters.AddWithValue("@ColumnaVariaCombusNombre", config.Lectura?.ColumnaVariaCombusNombre ?? 16);
                    cmd.Parameters.AddWithValue("@ColumnaVariaCombusMonto", config.Lectura?.ColumnaVariaCombusMonto ?? 18);
                    cmd.Parameters.AddWithValue("@ColumnaTablaHermes", config.Lectura?.ColumnaTablaHermes ?? 14);
                    
                    // Row mappings
                    var m = config.Lectura?.MapeoFilas;
                    cmd.Parameters.AddWithValue("@Fila_Venta_GPL", GetRowNumberForProperty(m, "Venta_GPL"));
                    cmd.Parameters.AddWithValue("@Fila_Venta_GNV", GetRowNumberForProperty(m, "Venta_GNV"));
                    cmd.Parameters.AddWithValue("@Fila_Total_venta_acumulada", GetRowNumberForProperty(m, "Total_venta_acumulada"));
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_Liquidos", GetRowNumberForProperty(m, "Total_Tarjeta_de_Credito_Liquidos"));
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_GLP", GetRowNumberForProperty(m, "Total_Tarjeta_de_Credito_GLP"));
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_GNV", GetRowNumberForProperty(m, "Total_Tarjeta_de_Credito_GNV"));
                    cmd.Parameters.AddWithValue("@Fila_ErrorMaquina", GetRowNumberForProperty(m, "ErrorMaquina"));
                    cmd.Parameters.AddWithValue("@Fila_Recaudo_Cofide_GNV", GetRowNumberForProperty(m, "Recaudo_Cofide_GNV"));
                    cmd.Parameters.AddWithValue("@Fila_Gastos", GetRowNumberForProperty(m, "Gastos"));
                    cmd.Parameters.AddWithValue("@Fila_Ventas_con_transferencia", GetRowNumberForProperty(m, "Ventas_con_transferencia"));

                    // Col mappings
                    var c = config.Escritura?.Columnas;
                    cmd.Parameters.AddWithValue("@Col_Venta_GPL", GetColLetterForProperty(c, "Venta_GPL"));
                    cmd.Parameters.AddWithValue("@Col_Venta_GNV", GetColLetterForProperty(c, "Venta_GNV"));
                    cmd.Parameters.AddWithValue("@Col_Total_venta_acumulada", GetColLetterForProperty(c, "Total_venta_acumulada"));
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_Liquidos", GetColLetterForProperty(c, "Total_Tarjeta_de_Credito_Liquidos"));
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_GLP", GetColLetterForProperty(c, "Total_Tarjeta_de_Credito_GLP"));
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_GNV", GetColLetterForProperty(c, "Total_Tarjeta_de_Credito_GNV"));
                    cmd.Parameters.AddWithValue("@Col_ErrorMaquina", GetColLetterForProperty(c, "ErrorMaquina"));
                    cmd.Parameters.AddWithValue("@Col_Recaudo_Cofide_GNV", GetColLetterForProperty(c, "Recaudo_Cofide_GNV"));
                    cmd.Parameters.AddWithValue("@Col_Gastos", GetColLetterForProperty(c, "Gastos"));
                    cmd.Parameters.AddWithValue("@Col_Ventas_con_transferencia", GetColLetterForProperty(c, "Ventas_con_transferencia"));
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
                        // Check if the Fila_Venta_GPL column exists to distinguish from the older JSON schema
                        using var colCmd = new SqliteCommand("PRAGMA table_info(REGISTRO_VENTAS_CONFIGURACION);", connection);
                        using var reader = colCmd.ExecuteReader();
                        bool hasRelationalColumns = false;
                        while (reader.Read())
                        {
                            string colName = reader.GetString(1);
                            if (colName.Equals("Fila_Venta_GPL", StringComparison.OrdinalIgnoreCase))
                            {
                                hasRelationalColumns = true;
                                break;
                            }
                        }
                        if (!hasRelationalColumns)
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
                        SELECT g.Nombre, g.Id, c.ColumnaFecha, c.ColumnaTotales, c.ColumnaCreditoNombre, 
                               c.ColumnaCreditoMonto, c.ColumnaVariaCombusNombre, c.ColumnaVariaCombusMonto, 
                               c.ColumnaTablaHermes,
                               -- Rows (9 to 18)
                               c.Fila_Venta_GPL, c.Fila_Venta_GNV, c.Fila_Total_venta_acumulada, c.Fila_Total_Tarjeta_de_Credito_Liquidos,
                               c.Fila_Total_Tarjeta_de_Credito_GLP, c.Fila_Total_Tarjeta_de_Credito_GNV, c.Fila_ErrorMaquina,
                               c.Fila_Recaudo_Cofide_GNV, c.Fila_Gastos, c.Fila_Ventas_con_transferencia,
                               -- Cols (19 to 34)
                               c.Col_Venta_GPL, c.Col_Venta_GNV, c.Col_Total_venta_acumulada, c.Col_Total_Tarjeta_de_Credito_Liquidos,
                               c.Col_Total_Tarjeta_de_Credito_GLP, c.Col_Total_Tarjeta_de_Credito_GNV, c.Col_ErrorMaquina,
                               c.Col_Recaudo_Cofide_GNV, c.Col_Gastos, c.Col_Ventas_con_transferencia, c.Col_DescuentoLiquidos,
                               c.Col_DescuentoGLP, c.Col_Hermes_monto_liquido, c.Col_Hermes_monto_GLP, c.Col_Hermes_monto_GNV1,
                               c.Col_Hermes_monto_GNV2
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
                                    ColumnaFecha = reader.GetInt32(2),
                                    ColumnaTotales = reader.GetInt32(3),
                                    ColumnaCreditoNombre = reader.GetInt32(4),
                                    ColumnaCreditoMonto = reader.GetInt32(5),
                                    ColumnaVariaCombusNombre = reader.GetInt32(6),
                                    ColumnaVariaCombusMonto = reader.GetInt32(7),
                                    ColumnaTablaHermes = reader.GetInt32(8),
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
                            
                            AddRow(reader.IsDBNull(9) ? "" : reader.GetString(9), "Venta_GPL");
                            AddRow(reader.IsDBNull(10) ? "" : reader.GetString(10), "Venta_GNV");
                            AddRow(reader.IsDBNull(11) ? "" : reader.GetString(11), "Total_venta_acumulada");
                            AddRow(reader.IsDBNull(12) ? "" : reader.GetString(12), "Total_Tarjeta_de_Credito_Liquidos");
                            AddRow(reader.IsDBNull(13) ? "" : reader.GetString(13), "Total_Tarjeta_de_Credito_GLP");
                            AddRow(reader.IsDBNull(14) ? "" : reader.GetString(14), "Total_Tarjeta_de_Credito_GNV");
                            AddRow(reader.IsDBNull(15) ? "" : reader.GetString(15), "ErrorMaquina");
                            AddRow(reader.IsDBNull(16) ? "" : reader.GetString(16), "Recaudo_Cofide_GNV");
                            AddRow(reader.IsDBNull(17) ? "" : reader.GetString(17), "Gastos");
                            AddRow(reader.IsDBNull(18) ? "" : reader.GetString(18), "Ventas_con_transferencia");

                            // Populate Escritura.Columnas
                            void AddCol(string colVal, string propName)
                            {
                                if (grifo.Escritura != null)
                                {
                                    grifo.Escritura.Columnas[propName] = colVal ?? "";
                                }
                            }

                            AddCol(reader.IsDBNull(19) ? "" : reader.GetString(19), "Venta_GPL");
                            AddCol(reader.IsDBNull(20) ? "" : reader.GetString(20), "Venta_GNV");
                            AddCol(reader.IsDBNull(21) ? "" : reader.GetString(21), "Total_venta_acumulada");
                            AddCol(reader.IsDBNull(22) ? "" : reader.GetString(22), "Total_Tarjeta_de_Credito_Liquidos");
                            AddCol(reader.IsDBNull(23) ? "" : reader.GetString(23), "Total_Tarjeta_de_Credito_GLP");
                            AddCol(reader.IsDBNull(24) ? "" : reader.GetString(24), "Total_Tarjeta_de_Credito_GNV");
                            AddCol(reader.IsDBNull(25) ? "" : reader.GetString(25), "ErrorMaquina");
                            AddCol(reader.IsDBNull(26) ? "" : reader.GetString(26), "Recaudo_Cofide_GNV");
                            AddCol(reader.IsDBNull(27) ? "" : reader.GetString(27), "Gastos");
                            AddCol(reader.IsDBNull(28) ? "" : reader.GetString(28), "Ventas_con_transferencia");
                            AddCol(reader.IsDBNull(29) ? "" : reader.GetString(29), "DescuentoLiquidos");
                            AddCol(reader.IsDBNull(30) ? "" : reader.GetString(30), "DescuentoGLP");
                            AddCol(reader.IsDBNull(31) ? "" : reader.GetString(31), "Hermes_monto_liquido");
                            AddCol(reader.IsDBNull(32) ? "" : reader.GetString(32), "Hermes_monto_GLP");
                            AddCol(reader.IsDBNull(33) ? "" : reader.GetString(33), "Hermes_monto_GNV1");
                            AddCol(reader.IsDBNull(34) ? "" : reader.GetString(34), "Hermes_monto_GNV2");

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
                        SELECT g.Id, g.Nombre, c.Id, c.ColumnaFecha, c.ColumnaTotales, c.ColumnaCreditoNombre, 
                               c.ColumnaCreditoMonto, c.ColumnaVariaCombusNombre, c.ColumnaVariaCombusMonto, 
                               c.ColumnaTablaHermes,
                               -- Rows
                               c.Fila_Venta_GPL, c.Fila_Venta_GNV, c.Fila_Total_venta_acumulada, c.Fila_Total_Tarjeta_de_Credito_Liquidos,
                               c.Fila_Total_Tarjeta_de_Credito_GLP, c.Fila_Total_Tarjeta_de_Credito_GNV, c.Fila_ErrorMaquina,
                               c.Fila_Recaudo_Cofide_GNV, c.Fila_Gastos, c.Fila_Ventas_con_transferencia,
                               -- Cols
                               c.Col_Venta_GPL, c.Col_Venta_GNV, c.Col_Total_venta_acumulada, c.Col_Total_Tarjeta_de_Credito_Liquidos,
                               c.Col_Total_Tarjeta_de_Credito_GLP, c.Col_Total_Tarjeta_de_Credito_GNV, c.Col_ErrorMaquina,
                               c.Col_Recaudo_Cofide_GNV, c.Col_Gastos, c.Col_Ventas_con_transferencia, c.Col_DescuentoLiquidos,
                               c.Col_DescuentoGLP, c.Col_Hermes_monto_liquido, c.Col_Hermes_monto_GLP, c.Col_Hermes_monto_GNV1,
                               c.Col_Hermes_monto_GNV2,
                               g.Plantilla
                        FROM REGISTRO_VENTAS_GRIFOS g
                        INNER JOIN REGISTRO_VENTAS_CONFIGURACION c ON g.Id = c.GrifoId
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
                                Plantilla = reader.IsDBNull(36) ? "" : reader.GetString(36),
                                Configuracion = new RegistroVentasConfiguracion
                                {
                                    Id = reader.GetInt32(2),
                                    GrifoId = gId,
                                    ColumnaFecha = reader.GetInt32(3),
                                    ColumnaTotales = reader.GetInt32(4),
                                    ColumnaCreditoNombre = reader.GetInt32(5),
                                    ColumnaCreditoMonto = reader.GetInt32(6),
                                    ColumnaVariaCombusNombre = reader.GetInt32(7),
                                    ColumnaVariaCombusMonto = reader.GetInt32(8),
                                    ColumnaTablaHermes = reader.GetInt32(9),
                                    
                                    // Rows
                                    Fila_Venta_GPL = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                    Fila_Venta_GNV = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                    Fila_Total_venta_acumulada = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                    Fila_Total_Tarjeta_de_Credito_Liquidos = reader.IsDBNull(13) ? "" : reader.GetString(13),
                                    Fila_Total_Tarjeta_de_Credito_GLP = reader.IsDBNull(14) ? "" : reader.GetString(14),
                                    Fila_Total_Tarjeta_de_Credito_GNV = reader.IsDBNull(15) ? "" : reader.GetString(15),
                                    Fila_ErrorMaquina = reader.IsDBNull(16) ? "" : reader.GetString(16),
                                    Fila_Recaudo_Cofide_GNV = reader.IsDBNull(17) ? "" : reader.GetString(17),
                                    Fila_Gastos = reader.IsDBNull(18) ? "" : reader.GetString(18),
                                    Fila_Ventas_con_transferencia = reader.IsDBNull(19) ? "" : reader.GetString(19),

                                    // Cols
                                    Col_Venta_GPL = reader.IsDBNull(20) ? "" : reader.GetString(20),
                                    Col_Venta_GNV = reader.IsDBNull(21) ? "" : reader.GetString(21),
                                    Col_Total_venta_acumulada = reader.IsDBNull(22) ? "" : reader.GetString(22),
                                    Col_Total_Tarjeta_de_Credito_Liquidos = reader.IsDBNull(23) ? "" : reader.GetString(23),
                                    Col_Total_Tarjeta_de_Credito_GLP = reader.IsDBNull(24) ? "" : reader.GetString(24),
                                    Col_Total_Tarjeta_de_Credito_GNV = reader.IsDBNull(25) ? "" : reader.GetString(25),
                                    Col_ErrorMaquina = reader.IsDBNull(26) ? "" : reader.GetString(26),
                                    Col_Recaudo_Cofide_GNV = reader.IsDBNull(27) ? "" : reader.GetString(27),
                                    Col_Gastos = reader.IsDBNull(28) ? "" : reader.GetString(28),
                                    Col_Ventas_con_transferencia = reader.IsDBNull(29) ? "" : reader.GetString(29),
                                    Col_DescuentoLiquidos = reader.IsDBNull(30) ? "" : reader.GetString(30),
                                    Col_DescuentoGLP = reader.IsDBNull(31) ? "" : reader.GetString(31),
                                    Col_Hermes_monto_liquido = reader.IsDBNull(32) ? "" : reader.GetString(32),
                                    Col_Hermes_monto_GLP = reader.IsDBNull(33) ? "" : reader.GetString(33),
                                    Col_Hermes_monto_GNV1 = reader.IsDBNull(34) ? "" : reader.GetString(34),
                                    Col_Hermes_monto_GNV2 = reader.IsDBNull(35) ? "" : reader.GetString(35)
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
                        GrifoId, ColumnaFecha, ColumnaTotales, ColumnaCreditoNombre, 
                        ColumnaCreditoMonto, ColumnaVariaCombusNombre, ColumnaVariaCombusMonto, ColumnaTablaHermes,
                        -- Rows
                        Fila_Venta_GPL, Fila_Venta_GNV, Fila_Total_venta_acumulada, Fila_Total_Tarjeta_de_Credito_Liquidos,
                        Fila_Total_Tarjeta_de_Credito_GLP, Fila_Total_Tarjeta_de_Credito_GNV, Fila_ErrorMaquina,
                        Fila_Recaudo_Cofide_GNV, Fila_Gastos, Fila_Ventas_con_transferencia,
                        -- Cols
                        Col_Venta_GPL, Col_Venta_GNV, Col_Total_venta_acumulada, Col_Total_Tarjeta_de_Credito_Liquidos,
                        Col_Total_Tarjeta_de_Credito_GLP, Col_Total_Tarjeta_de_Credito_GNV, Col_ErrorMaquina,
                        Col_Recaudo_Cofide_GNV, Col_Gastos, Col_Ventas_con_transferencia, Col_DescuentoLiquidos,
                        Col_DescuentoGLP, Col_Hermes_monto_liquido, Col_Hermes_monto_GLP, Col_Hermes_monto_GNV1,
                        Col_Hermes_monto_GNV2
                    ) VALUES (
                        @GrifoId, @ColumnaFecha, @ColumnaTotales, @ColumnaCreditoNombre, 
                        @ColumnaCreditoMonto, @ColumnaVariaCombusNombre, @ColumnaVariaCombusMonto, @ColumnaTablaHermes,
                        -- Rows
                        @Fila_Venta_GPL, @Fila_Venta_GNV, @Fila_Total_venta_acumulada, @Fila_Total_Tarjeta_de_Credito_Liquidos,
                        @Fila_Total_Tarjeta_de_Credito_GLP, @Fila_Total_Tarjeta_de_Credito_GNV, @Fila_ErrorMaquina,
                        @Fila_Recaudo_Cofide_GNV, @Fila_Gastos, @Fila_Ventas_con_transferencia,
                        -- Cols
                        @Col_Venta_GPL, @Col_Venta_GNV, @Col_Total_venta_acumulada, @Col_Total_Tarjeta_de_Credito_Liquidos,
                        @Col_Total_Tarjeta_de_Credito_GLP, @Col_Total_Tarjeta_de_Credito_GNV, @Col_ErrorMaquina,
                        @Col_Recaudo_Cofide_GNV, @Col_Gastos, @Col_Ventas_con_transferencia, @Col_DescuentoLiquidos,
                        @Col_DescuentoGLP, @Col_Hermes_monto_liquido, @Col_Hermes_monto_GLP, @Col_Hermes_monto_GNV1,
                        @Col_Hermes_monto_GNV2
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
                    cmd.Parameters.AddWithValue("@ColumnaFecha", c.ColumnaFecha);
                    cmd.Parameters.AddWithValue("@ColumnaTotales", c.ColumnaTotales);
                    cmd.Parameters.AddWithValue("@ColumnaCreditoNombre", c.ColumnaCreditoNombre);
                    cmd.Parameters.AddWithValue("@ColumnaCreditoMonto", c.ColumnaCreditoMonto);
                    cmd.Parameters.AddWithValue("@ColumnaVariaCombusNombre", c.ColumnaVariaCombusNombre);
                    cmd.Parameters.AddWithValue("@ColumnaVariaCombusMonto", c.ColumnaVariaCombusMonto);
                    cmd.Parameters.AddWithValue("@ColumnaTablaHermes", c.ColumnaTablaHermes);
                    
                    // Row mappings
                    cmd.Parameters.AddWithValue("@Fila_Venta_GPL", c.Fila_Venta_GPL ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Venta_GNV", c.Fila_Venta_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Total_venta_acumulada", c.Fila_Total_venta_acumulada ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_Liquidos", c.Fila_Total_Tarjeta_de_Credito_Liquidos ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_GLP", c.Fila_Total_Tarjeta_de_Credito_GLP ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Total_Tarjeta_de_Credito_GNV", c.Fila_Total_Tarjeta_de_Credito_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Fila_ErrorMaquina", c.Fila_ErrorMaquina ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Recaudo_Cofide_GNV", c.Fila_Recaudo_Cofide_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Gastos", c.Fila_Gastos ?? "");
                    cmd.Parameters.AddWithValue("@Fila_Ventas_con_transferencia", c.Fila_Ventas_con_transferencia ?? "");

                    // Col mappings
                    cmd.Parameters.AddWithValue("@Col_Venta_GPL", c.Col_Venta_GPL ?? "");
                    cmd.Parameters.AddWithValue("@Col_Venta_GNV", c.Col_Venta_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Col_Total_venta_acumulada", c.Col_Total_venta_acumulada ?? "");
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_Liquidos", c.Col_Total_Tarjeta_de_Credito_Liquidos ?? "");
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_GLP", c.Col_Total_Tarjeta_de_Credito_GLP ?? "");
                    cmd.Parameters.AddWithValue("@Col_Total_Tarjeta_de_Credito_GNV", c.Col_Total_Tarjeta_de_Credito_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Col_ErrorMaquina", c.Col_ErrorMaquina ?? "");
                    cmd.Parameters.AddWithValue("@Col_Recaudo_Cofide_GNV", c.Col_Recaudo_Cofide_GNV ?? "");
                    cmd.Parameters.AddWithValue("@Col_Gastos", c.Col_Gastos ?? "");
                    cmd.Parameters.AddWithValue("@Col_Ventas_con_transferencia", c.Col_Ventas_con_transferencia ?? "");
                    cmd.Parameters.AddWithValue("@Col_DescuentoLiquidos", c.Col_DescuentoLiquidos ?? "");
                    cmd.Parameters.AddWithValue("@Col_DescuentoGLP", c.Col_DescuentoGLP ?? "");
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_liquido", c.Col_Hermes_monto_liquido ?? "");
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GLP", c.Col_Hermes_monto_GLP ?? "");
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GNV1", c.Col_Hermes_monto_GNV1 ?? "");
                    cmd.Parameters.AddWithValue("@Col_Hermes_monto_GNV2", c.Col_Hermes_monto_GNV2 ?? "");

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
