Add-Type -Path "c:\ProyectNet\PETRO_BOT\bin\Debug\net10.0\EPPlus.dll"
[OfficeOpenXml.ExcelPackage]::LicenseContext = [OfficeOpenXml.LicenseContext]::NonCommercial

$excelFile = "c:\ProyectNet\PETRO_BOT\wwwroot\bd\RegistroVentas\Plantillas\PARTE DIARIO 3 ABRIL---BRASIL.xlsx"
$pkg = [OfficeOpenXml.ExcelPackage]::new($excelFile)
$sheet = $pkg.Workbook.Worksheets[0]

Write-Output "Sheet Name: $($sheet.Name)"
Write-Output "Dimension: $($sheet.Dimension.Address)"

for ($r = 1; $r -le 20; $r++) {
    for ($c = 1; $c -le 10; $c++) {
        $cell = $sheet.Cells[$r, $c]
        $val = $cell.Value
        if ($val -ne $null -or $cell.Style.Fill.PatternType -ne [OfficeOpenXml.Style.ExcelFillStyle]::None) {
            $bg = $cell.Style.Fill.BackgroundColor.Rgb
            $fg = $cell.Style.Font.Color.Rgb
            $fontColor = $cell.Style.Font.Color.LookupColor()
            Write-Output "Row $r Col $c : Val='$val' Text='$($cell.Text)' BG='$bg' FG='$fg' LookupFG='$fontColor'"
        }
    }
}
$pkg.Dispose()
