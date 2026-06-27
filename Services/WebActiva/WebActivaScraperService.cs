using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PETRO_BOT.Models.WebActiva;
using PETRO_BOT.Services.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PETRO_BOT.Services.WebActiva
{
    public class WebActivaScraperService
    {
        private readonly IConfiguration _configuration;

        public WebActivaScraperService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ObtenerCarpetaDescarga()
        {
            string rutaBase = ConfiguracionService.ObtenerRutaBase();
            string carpeta = Path.Combine(rutaBase, "wwwroot", "uploads", "WebActivaExcel");
            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }
            return carpeta;
        }

        public async Task EjecutarScrapingAsync(string fechaInicio, string fechaFin, Action<ScraperProgress>? onProgress)
        {
            string downloadPath = ObtenerCarpetaDescarga();

            string loginUrl = _configuration["WebActiva:LoginUrl"] ?? "http://190.223.24.11:9010/account/login";
            string usuario = _configuration["WebActiva:Usuario"] ?? "ECHAVEZ";
            string contrasena = _configuration["WebActiva:Contrasena"] ?? "HOLI";
            bool headless = bool.TryParse(_configuration["WebActiva:Headless"], out bool h) ? h : true;

            Uri uri = new Uri(loginUrl);
            string origin = $"{uri.Scheme}://{uri.Authority}";

            string tempProfileDir = Path.Combine(Path.GetTempPath(), $"ChromeProfile_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempProfileDir);

            ChromeOptions options = new ChromeOptions();
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("profile.default_content_settings.popups", 0);
            options.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
            options.AddUserProfilePreference("safebrowsing.enabled", true);
            options.AddUserProfilePreference("safebrowsing.disable_download_protection", true);
            options.AddUserProfilePreference("download_restrictions", 0);
            options.AddUserProfilePreference("profile.default_content_setting_values.insecure_private_network", 1);

            options.AddArgument("--start-maximized");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-web-security");
            options.AddArgument("--allow-running-insecure-content");
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-features=InsecureDownloadWarnings,BlockInsecureDownloads,InsecureDownloadBlocking");
            options.AddArgument($"--unsafely-treat-insecure-origin-as-secure={origin}");
            options.AddArgument($"--user-data-dir={tempProfileDir}");

            if (headless)
            {
                options.AddArgument("--headless=new");
            }

            IWebDriver? driver = null;

            try
            {
                onProgress?.Invoke(new ScraperProgress { PasoActual = "-ingresando a web activa" });

                driver = await Task.Run(() => new ChromeDriver(options));
                driver.Navigate().GoToUrl(loginUrl);

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                await Task.Delay(1000);

                onProgress?.Invoke(new ScraperProgress { PasoActual = "-ingresando credenciales" });

                IWebElement usernameInput = wait.Until(d => d.FindElement(By.Name("userNameOrEmailAddress")));
                IWebElement passwordInput = wait.Until(d => d.FindElement(By.Name("password")));
                usernameInput.SendKeys(usuario);
                passwordInput.SendKeys(contrasena);

                IWebElement loginButton = wait.Until(d => d.FindElement(By.XPath("//button[@type='submit' and contains(text(), 'Iniciar sesión')]")));
                loginButton.Click();
                await Task.Delay(5000);

                IWebElement reportesSagitarius = wait.Until(d => d.FindElement(By.XPath("//span[contains(text(), 'Reportes Sagitarius')]")));
                reportesSagitarius.Click();
                await Task.Delay(2000);

                onProgress?.Invoke(new ScraperProgress { PasoActual = "-ingresando a modulo de reporteria" });

                IWebElement reportesVentas = wait.Until(d => d.FindElement(By.XPath("//span[contains(text(), 'Reportes Ventas')]")));
                reportesVentas.Click();
                await Task.Delay(5000);

                string searchXpath = "//tr[.//td[normalize-space(text())='Rp69']]//button[.//mat-icon[text()='search']]";
                IWebElement searchButton = wait.Until(d => d.FindElement(By.XPath(searchXpath)));
                searchButton.Click();
                await Task.Delay(5000);

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript($@"
                    var el1 = document.getElementsByName('initialDateModel')[0];
                    if(el1) {{
                        el1.value = '{fechaInicio}';
                        el1.dispatchEvent(new Event('input', {{ bubbles: true }}));
                        el1.dispatchEvent(new Event('change', {{ bubbles: true }}));
                    }}

                    var el2 = document.getElementsByName('finalDateModel')[0];
                    if(el2) {{
                        el2.value = '{fechaFin}';
                        el2.dispatchEvent(new Event('input', {{ bubbles: true }}));
                        el2.dispatchEvent(new Event('change', {{ bubbles: true }}));
                    }}
                ");
                await Task.Delay(1000);

                IWebElement selectFormat = wait.Until(d => d.FindElement(By.Name("selectedReportType")));
                selectFormat.Click();
                IWebElement excelOption = selectFormat.FindElement(By.XPath(".//option[@value='1']"));
                excelOption.Click();
                await Task.Delay(1000);

                IWebElement dropdownLabel = wait.Until(d => d.FindElement(By.XPath("//label[contains(@class, 'ui-dropdown-label')]")));
                dropdownLabel.Click();
                await Task.Delay(1500);

                IWebElement dropdownList = wait.Until(d => d.FindElement(By.XPath("//ul[contains(@class, 'ui-dropdown-items')]")));
                var optionsList = dropdownList.FindElements(By.XPath(".//li[@role='option']"));

                List<string> listaGrifos = new List<string>();
                foreach (var option in optionsList)
                {
                    string? ariaLabel = option.GetAttribute("aria-label");
                    if (!string.IsNullOrEmpty(ariaLabel) && ariaLabel.Trim() != "Todos los locales")
                    {
                        listaGrifos.Add(ariaLabel.Trim());
                    }
                }

                dropdownLabel.Click();
                await Task.Delay(1000);

                onProgress?.Invoke(new ScraperProgress
                {
                    PasoActual = "-descargando archivos",
                    DescargaIniciada = true,
                    TotalGrifos = listaGrifos.Count,
                    GrifosProcesados = 0
                });

                string periodoStr = "";
                if (!string.IsNullOrEmpty(fechaInicio) && fechaInicio.Length >= 7)
                {
                    periodoStr = fechaInicio.Substring(0, 4) + fechaInicio.Substring(5, 2);
                }

                for (int i = 0; i < listaGrifos.Count; i++)
                {
                    string grifo = listaGrifos[i];

                    IWebElement currentDropdown = wait.Until(d => d.FindElement(By.XPath("//label[contains(@class, 'ui-dropdown-label')]")));
                    currentDropdown.Click();
                    await Task.Delay(1000);

                    string optionItemXpath = $"//li[@role='option' and @aria-label='{grifo}']";
                    IWebElement itemToClick = wait.Until(d => d.FindElement(By.XPath(optionItemXpath)));
                    itemToClick.Click();
                    await Task.Delay(1000);

                    IWebElement lupaBtn = wait.Until(d => d.FindElement(By.XPath("//div[p[text()='Formato de exportación']]//button[.//i[contains(@class, 'fa-search')]]")));
                    lupaBtn.Click();

                    IWebElement? btnDescargar = null;
                    while (btnDescargar == null)
                    {
                        try
                        {
                            WebDriverWait waitReporte = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                            btnDescargar = waitReporte.Until(d =>
                            {
                                try
                                {
                                    var element = d.FindElement(By.XPath("//button[contains(text(), 'Descargar')]"));
                                    if (element.Displayed && element.Enabled)
                                    {
                                        return element;
                                    }
                                    return null;
                                }
                                catch (NoSuchElementException)
                                {
                                    return null;
                                }
                            });
                        }
                        catch (WebDriverTimeoutException)
                        {
                            // Continúa esperando
                        }
                    }

                    await Task.Delay(1000);

                    var filesBefore = Directory.GetFiles(downloadPath).ToList();

                    btnDescargar.Click();

                    string downloadedFile = "";
                    int attempts = 0;
                    while (attempts < 60)
                    {
                        var currentFiles = Directory.GetFiles(downloadPath).ToList();
                        var newFiles = currentFiles.Except(filesBefore).ToList();

                        var readyFile = newFiles.FirstOrDefault(f => !f.EndsWith(".crdownload") && !f.EndsWith(".tmp"));

                        if (readyFile != null)
                        {
                            await Task.Delay(1000);
                            downloadedFile = readyFile;
                            break;
                        }
                        else
                        {
                            var crdownloadFile = newFiles.FirstOrDefault(f => f.EndsWith(".crdownload"));
                            if (crdownloadFile != null)
                            {
                                long size1 = new FileInfo(crdownloadFile).Length;
                                await Task.Delay(2000);
                                long size2 = new FileInfo(crdownloadFile).Length;

                                if (size1 > 0 && size1 == size2)
                                {
                                    downloadedFile = crdownloadFile;
                                    break;
                                }
                            }
                        }

                        await Task.Delay(1000);
                        attempts++;
                    }

                    string newFileName = "";
                    if (!string.IsNullOrEmpty(downloadedFile))
                    {
                        string cleanGrifoName = grifo.Replace(" ", "").ToUpper();
                        newFileName = !string.IsNullOrEmpty(periodoStr) ? $"{cleanGrifoName}_{periodoStr}.xlsx" : $"{cleanGrifoName}.xlsx";
                        string newPath = Path.Combine(downloadPath, newFileName);

                        if (File.Exists(newPath)) File.Delete(newPath);
                        File.Move(downloadedFile, newPath);
                    }
                    else
                    {
                        newFileName = "Error_Descarga.xlsx";
                    }

                    int procesadosCount = i + 1;
                    string logTexto = $"grifo {grifo} + {newFileName} procesado correctamente";

                    onProgress?.Invoke(new ScraperProgress
                    {
                        PasoActual = "-descargando archivos",
                        DescargaIniciada = true,
                        TotalGrifos = listaGrifos.Count,
                        GrifosProcesados = procesadosCount,
                        UltimoMensajeLog = logTexto
                    });

                    await Task.Delay(2000);
                }

                onProgress?.Invoke(new ScraperProgress { Finalizado = true, DescargaIniciada = true, TotalGrifos = listaGrifos.Count, GrifosProcesados = listaGrifos.Count });
            }
            catch (Exception ex)
            {
                onProgress?.Invoke(new ScraperProgress
                {
                    ConError = true,
                    MensajeError = ex.Message,
                    Finalizado = true
                });
            }
            finally
            {
                if (driver != null)
                {
                    try { driver.Quit(); } catch { }
                }
                try
                {
                    if (Directory.Exists(tempProfileDir)) Directory.Delete(tempProfileDir, true);
                }
                catch { }
            }
        }
    }
}
