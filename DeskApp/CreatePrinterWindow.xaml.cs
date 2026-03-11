using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DeskApp.Models;
using System.Threading;
using System.Management;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Printing;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using System.Drawing;
using System.Drawing.Printing;
using DeskApp.Services;

namespace DeskApp
{
    public partial class CreatePrinterWindow : Window
    {
        private ObservableCollection<PrinterData> _printers = new();

        public CreatePrinterWindow()
        {
            InitializeComponent();
            PrintersComboBox.ItemsSource = _printers;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try { 
                try { PrintersComboBox.ClearValue(System.Windows.Controls.Primitives.Selector.TemplateProperty); } catch { }
                try { PrintersComboBox.ClearValue(System.Windows.Controls.ComboBox.TemplateProperty); } catch { }
            }
            catch { }

            try
            {
                ScanningPanel.Visibility = Visibility.Visible;
                await ScanNetworkAsync();
                await ScanLocalPrintersAsync();

                try
                {
                    var token = SessionService.Instance.Token;
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        var apiResult = await ApiService.Instance.GetPrintersAsync(token);
                        if (apiResult.Success)
                        {
                            var remoteList = apiResult.Data ?? new System.Collections.Generic.List<PrinterData>();
                            foreach (var remote in remoteList)
                            {
                                if (!_printers.Any(x => (!string.IsNullOrWhiteSpace(remote.IP) && x.IP == remote.IP && x.Port == remote.Port) || (x.Name == remote.Name && x.PortName == remote.PortName)))
                                {
                                    _printers.Add(remote);
                                }
                            }
                            if (remoteList.Count == 0)
                            {
                                try { ToastNotification.Show("No se encontraron impresoras en el servidor.", ToastType.Info, 3); } catch { }
                            }
                        }
                        else
                        {
                            var msg = apiResult.ErrorMessage ?? $"Error cargando impresoras: HTTP {apiResult.StatusCode}";
                            try { System.Diagnostics.Debug.WriteLine($"[CreatePrinterWindow] GetPrinters failed: {msg}"); } catch { }
                            try { ToastNotification.Show(msg, ToastType.Warning, 5); } catch { }
                        }
                     }
                 }
                 catch { }
            }
            finally
            {
                ScanningPanel.Visibility = Visibility.Collapsed;
            }

            if (_printers.Count > 0)
            {
                PrintersComboBox.SelectedIndex = 0;
            }
        }

        private async Task ScanNetworkAsync()
        {
            var localIps = GetLocalIPv4Addresses();
            var endpoints = new System.Collections.Generic.List<(string ip, int port)>();
            var portsToTry = new[] { 9100, 631, 515 };
            foreach (var ip in localIps)
            {
                var parts = ip.Split('.');
                if (parts.Length < 4) continue;
                var baseIp = string.Join('.', parts[0..3]);
                for (int i = 1; i < 255; i++)
                {
                    var target = baseIp + "." + i;
                    foreach (var p in portsToTry) endpoints.Add((target, p));
                }
            }

            var maxConcurrency = 200;
            using var sem = new SemaphoreSlim(maxConcurrency);
            var tasks = new System.Collections.Generic.List<Task>();
            var timeoutMs = 120;

            foreach (var (target, port) in endpoints)
            {
                await sem.WaitAsync();
                var t = Task.Run(async () =>
                {
                    try
                    {
                        using var tcp = new System.Net.Sockets.TcpClient();
                        var connectTask = tcp.ConnectAsync(target, port);
                        var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));
                        if (completed == connectTask && tcp.Connected)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    var pd = new PrinterData { Name = $"Printer {target}:{port}", ConnectionType = PrinterConnectionType.Network, IP = target, Port = port };
                                    if (!_printers.Any(x => x.IP == pd.IP && x.Port == pd.Port)) _printers.Add(pd);
                                }
                                catch { }
                            });
                        }
                    }
                    catch { }
                    finally { sem.Release(); }
                });
                tasks.Add(t);
            }
            await Task.WhenAll(tasks);
        }

        private async Task ScanLocalPrintersAsync()
        {
            try
            {
                try
                {
                    var query = new SelectQuery("SELECT Name, PortName, DriverName, DeviceID, Local, Network FROM Win32_Printer");
                    using var searcher = new ManagementObjectSearcher(query);
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        try
                        {
                            var name = (mo["Name"] as string) ?? string.Empty;
                            var portName = (mo["PortName"] as string) ?? string.Empty;
                            var driver = (mo["DriverName"] as string) ?? string.Empty;
                            var isNetwork = mo["Network"] is bool nb && nb;
                            var isLocal = mo["Local"] is bool lb && lb;

                            var connType = PrinterConnectionType.Unknown;
                            if (!string.IsNullOrWhiteSpace(portName) && portName.StartsWith("USB", StringComparison.OrdinalIgnoreCase))
                            {
                                connType = PrinterConnectionType.Usb;
                            }
                            else if (isNetwork || (!string.IsNullOrWhiteSpace(portName) && portName.StartsWith("IP_", StringComparison.OrdinalIgnoreCase)))
                            {
                                connType = PrinterConnectionType.Network;
                            }
                            else if (isLocal)
                            {
                                connType = PrinterConnectionType.Installed;
                            }

                            int? numericPort = null;
                            var m = Regex.Match(portName ?? string.Empty, "(\\d+)$");
                            if (m.Success && int.TryParse(m.Groups[1].Value, out var parsed)) numericPort = parsed;

                            var pd = new PrinterData
                            {
                                Name = name,
                                ConnectionType = connType,
                                IP = null,
                                Port = numericPort,
                                PortName = string.IsNullOrWhiteSpace(portName) ? null : portName,
                                Driver = string.IsNullOrWhiteSpace(driver) ? null : driver,
                                Model = string.IsNullOrWhiteSpace(driver) ? portName : $"{driver} ({portName})",
                                Status = DeskApp.Models.PrinterStatus.Available,
                                SerialNumber = null,
                                MacAddress = null
                            };

                            Dispatcher.Invoke(() =>
                            {
                                if (!_printers.Any(x => x.Name == pd.Name && x.Model == pd.Model && x.PortName == pd.PortName))
                                {
                                    _printers.Add(pd);
                                }
                            });
                        }
                        catch { }
                    }

                    return;
                }
                catch
                {
                }

                var server = new System.Printing.LocalPrintServer();
                var queues = server.GetPrintQueues(new[] { System.Printing.EnumeratedPrintQueueTypes.Local, System.Printing.EnumeratedPrintQueueTypes.Connections });
                foreach (var q in queues)
                {
                    try
                    {
                        var name = q.Name ?? string.Empty;
                        if (!_printers.Any(x => x.Name == name))
                        {
                            Dispatcher.Invoke(() => _printers.Add(new PrinterData { Name = name, ConnectionType = PrinterConnectionType.Installed, Model = q.FullName, Status = DeskApp.Models.PrinterStatus.Available }));
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static string[] GetLocalIPv4Addresses()
        {
            return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .Distinct()
                .ToArray();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            try
            {
                if (Owner is IndexWindow iw)
                {
                    _ = iw.LoadPrintersAsync();
                }
            }
            catch { }
        }

        private void PrintersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrintersComboBox.SelectedItem is PrinterData pd)
            {
                NameTextBox.Text = pd.Name;
                IpTextBox.Text = pd.IP ?? string.Empty;
                PortTextBox.Text = pd.Port?.ToString() ?? string.Empty;
                PortNameTextBox.Text = pd.PortName ?? string.Empty;
                DriverTextBox.Text = pd.Driver ?? string.Empty;
                ModelTextBox.Text = pd.Model ?? string.Empty;
                SerialTextBox.Text = pd.SerialNumber ?? string.Empty;
                MacTextBox.Text = pd.MacAddress ?? string.Empty;
                StatusComboBox.SelectedIndex = pd.Status switch
                {
                    DeskApp.Models.PrinterStatus.Available => 0,
                    DeskApp.Models.PrinterStatus.Busy => 1,
                    DeskApp.Models.PrinterStatus.Offline => 2,
                    _ => 0
                };
                TypeComboBox.SelectedIndex = pd.ConnectionType switch
                {
                    PrinterConnectionType.Usb => 1,
                    PrinterConnectionType.Network => 2,
                    PrinterConnectionType.Installed => 3,
                    _ => 0
                };
            }
        }

        private async void SavePrinter_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim() ?? string.Empty;
            var ip = string.IsNullOrWhiteSpace(IpTextBox.Text) ? null : IpTextBox.Text.Trim();
            int? port = null;
            if (int.TryParse(PortTextBox.Text, out var portVal)) port = portVal;
            var portName = string.IsNullOrWhiteSpace(PortNameTextBox.Text) ? null : PortNameTextBox.Text.Trim();
            var model = string.IsNullOrWhiteSpace(ModelTextBox.Text) ? null : ModelTextBox.Text.Trim();
            var driver = string.IsNullOrWhiteSpace(DriverTextBox.Text) ? null : DriverTextBox.Text.Trim();
            var serial = string.IsNullOrWhiteSpace(SerialTextBox.Text) ? null : SerialTextBox.Text.Trim();
            var mac = string.IsNullOrWhiteSpace(MacTextBox.Text) ? null : MacTextBox.Text.Trim();
            var status = StatusComboBox.SelectedIndex switch
            {
                0 => DeskApp.Models.PrinterStatus.Available,
                1 => DeskApp.Models.PrinterStatus.Busy,
                2 => DeskApp.Models.PrinterStatus.Offline,
                _ => DeskApp.Models.PrinterStatus.Available
            };
            var connectionType = (PrinterConnectionType)TypeComboBox.SelectedIndex;

            PrinterData? target = null;
            if (PrintersComboBox.SelectedItem is PrinterData selected)
            {
                selected.Name = name == string.Empty ? selected.Name : name;
                selected.IP = ip ?? selected.IP;
                if (port.HasValue) selected.Port = port.Value;
                if (!string.IsNullOrWhiteSpace(portName)) selected.PortName = portName;
                if (!string.IsNullOrWhiteSpace(driver)) selected.Driver = driver;
                selected.Model = model ?? selected.Model;
                selected.ConnectionType = connectionType;
                selected.SerialNumber = serial ?? selected.SerialNumber;
                selected.MacAddress = mac ?? selected.MacAddress;
                selected.Status = status;
                selected.UpdatedAt = DateTime.UtcNow;
                target = selected;
            }

            var toSave = new PrinterData
            {
                Name = name,
                IP = ip,
                Port = port,
                PortName = portName,
                Driver = driver,
                Model = model,
                ConnectionType = connectionType,
                SerialNumber = serial,
                MacAddress = mac,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                var token = SessionService.Instance.Token;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var apiResult = await ApiService.Instance.CreatePrinterAsync(toSave, token);
                    if (apiResult.Success && apiResult.Data != null)
                    {
                        var saved = apiResult.Data;
                        if (Owner is IndexWindow owner)
                        {
                            _ = owner.LoadPrintersAsync();
                        }

                        ToastNotification.Show("Impresora guardada en el servidor", ToastType.Success, 3);
                    }
                    else
                    {
                        ToastNotification.Show(apiResult.ErrorMessage ?? "No se pudo guardar la impresora en el servidor", ToastType.Warning, 4);
                        return;
                    }
                }
                else
                {
                    ToastNotification.Show("No hay sesión activa para guardar la impresora", ToastType.Warning, 4);
                    return;
                }
            }
            catch (Exception ex)
            {
                try { ToastNotification.Show($"Error al guardar en servidor: {ex.Message}", ToastType.Error, 4); } catch { }
                return;
            }

            if (target != null)
            {
                PrintersComboBox.SelectedItem = target;
            }

            this.DialogResult = true;
            this.Close();
        }

        private string? FindGhostscriptExecutable()
        {
            var candidates = new[] { "gswin64c.exe", "gswin32c.exe", "gs.exe" };

            foreach (var c in candidates)
            {
                try
                {
                    var where = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = c,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using var p = Process.Start(where);
                    if (p != null)
                    {
                        var outp = p.StandardOutput.ReadLine();
                        p.WaitForExit(1000);
                        if (!string.IsNullOrWhiteSpace(outp) && File.Exists(outp)) return outp;
                    }
                }
                catch { }
            }

            var programDirs = new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) };
            foreach (var baseDir in programDirs)
            {
                if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir)) continue;
                try
                {
                    var gsRoot = Path.Combine(baseDir, "gs");
                    if (!Directory.Exists(gsRoot)) continue;
                    var versions = Directory.GetDirectories(gsRoot);
                    Array.Sort(versions);
                    Array.Reverse(versions);
                    foreach (var ver in versions)
                    {
                        foreach (var c in candidates)
                        {
                            var candidate = Path.Combine(ver, "bin", c);
                            if (File.Exists(candidate)) return candidate;
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private void PrintTest_Click(object sender, RoutedEventArgs e)
        {
            if (PrintersComboBox.SelectedItem is not PrinterData pd)
            {
                ToastNotification.Show("No hay impresora seleccionada", ToastType.Warning, 3);
                return;
            }

            var ofd = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Seleccionar PDF",
                Filter = "PDF Files (*.pdf)|*.pdf"
            };

            if (ofd.ShowDialog() != true)
                return;

            var filePath = ofd.FileName;

            if (!File.Exists(filePath))
            {
                ToastNotification.Show("El archivo seleccionado no existe", ToastType.Error, 4);
                return;
            }

            var gsExe = FindGhostscriptExecutable();

            if (string.IsNullOrWhiteSpace(gsExe))
            {
                ToastNotification.Show("Ghostscript no está instalado o no se encontró", ToastType.Error, 5);
                return;
            }

            try
            {
                string gsArgs =
                    "-q " +
                    "-dNOPAUSE " +
                    "-dBATCH " +
                    "-dNOSAFER " +
                    "-sDEVICE=mswinpr2 " +
                    $"-sOutputFile=\"%printer%{pd.Name}\" " +
                    $"\"{filePath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = gsExe,
                    Arguments = gsArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using var proc = Process.Start(psi);

                if (proc == null)
                {
                    ToastNotification.Show("No se pudo iniciar Ghostscript", ToastType.Error, 4);
                    return;
                }

                proc.WaitForExit();

                string stderr = proc.StandardError.ReadToEnd();
                string stdout = proc.StandardOutput.ReadToEnd();

                if (proc.ExitCode != 0)
                {
                    ToastNotification.Show($"Error de impresión (código {proc.ExitCode})", ToastType.Error, 5);
                    return;
                }

                ToastNotification.Show("PDF enviado correctamente a la impresora", ToastType.Success, 3);
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al ejecutar impresión: {ex.Message}", ToastType.Error, 5);
            }
        }
    }
}
