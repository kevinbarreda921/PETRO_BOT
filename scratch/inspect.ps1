Add-Type -Path "C:\ProyectNet\PETRO_BOT\bin\Debug\net10.0\Microsoft.Data.Sqlite.dll"
$conn = [Microsoft.Data.Sqlite.SqliteConnection]::new("Data Source=C:\ProyectNet\PETRO_BOT\wwwroot\bd\DB_PETRO_BOT.db;Pooling=False;")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "PRAGMA table_info(REGISTRO_VENTAS_CONFIGURACION);"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Output $reader.GetString(1)
}
$conn.Close()
