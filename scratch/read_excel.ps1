Add-Type -Path "c:\ProyectNet\PETRO_BOT\bin\Debug\net10.0\EPPlus.dll"
[OfficeOpenXml.ExcelPackage]::LicenseContext = [OfficeOpenXml.LicenseContext]::NonCommercial

$excelFile = "c:\ProyectNet\PETRO_BOT\wwwroot\bd\RegistroVentas\Plantillas\PARTE DIARIO 3 ABRIL---BRASIL.xlsx"
$pkg = [OfficeOpenXml.ExcelPackage]::new($excelFile)
$sheet = $pkg.Workbook.Worksheets[0]

Write-Output "Sheet Name: $($sheet.Name)"

for ($r = 1; $r -le 25; $r++) {
    for ($c = 1; $c -le 10; $c++) {
        $cell = $sheet.Cells[$r, $c]
        if ($cell.Value -ne $null) {
            $val = $cell.Value
            $type = $val.GetType().Name
            $text = $cell.Text
            Write-Output "Row $r Col $c : Val='$val' Type='$type' Text='$text'"
        }
    }
}
$pkg.Dispose()
