using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string dbPath = @"c:\ProyectNet\PETRO_BOT\wwwroot\bd\DB_PETRO_BOT.db";
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        
        var command = connection.CreateCommand();
        // Check if column exists first to avoid errors on rerun
        command.CommandText = "PRAGMA table_info(REGISTRO_DESCUENTOS_WRITE);";
        bool hasColumn = false;
        using (var reader = command.ExecuteReader()) {
            while (reader.Read()) {
                if (reader["name"].ToString() == "TarjetaGNV") {
                    hasColumn = true;
                    break;
                }
            }
        }
        
        if (!hasColumn) {
            command.CommandText = "ALTER TABLE REGISTRO_DESCUENTOS_WRITE ADD COLUMN TarjetaGNV TEXT;";
            command.ExecuteNonQuery();
            Console.WriteLine("Column TarjetaGNV added.");
        } else {
            Console.WriteLine("Column TarjetaGNV already exists.");
        }
    }
}
