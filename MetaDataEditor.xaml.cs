using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using iTextSharp.text.pdf;

namespace DarkHub
{
    public partial class MetaDataEditor : Page
    {
        private string? selectedFilePath;
        private List<MetadataItem> metadataItems = new();
        private readonly Dictionary<int, string> imageMetadataProperties = new();
        private readonly Dictionary<string, string> documentMetadata = new();

        public MetaDataEditor()
        {
            try
            {
                InitializeComponent();
                InitializeImageMetadataProperties();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar MetaDataEditor: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeImageMetadataProperties()
        {
            try
            {
                imageMetadataProperties.Add(0x00FE, "New Subfile Type");
                imageMetadataProperties.Add(0x00FF, "Subfile Type");
                imageMetadataProperties.Add(0x0100, "Image Width");
                imageMetadataProperties.Add(0x0101, "Image Height");
                imageMetadataProperties.Add(0x0102, "Bits Per Sample");
                imageMetadataProperties.Add(0x0103, "Compression");
                imageMetadataProperties.Add(0x0106, "Photometric Interpretation");
                imageMetadataProperties.Add(0x0107, "Thresholding");
                imageMetadataProperties.Add(0x0108, "Cell Width");
                imageMetadataProperties.Add(0x0109, "Cell Length");
                imageMetadataProperties.Add(0x010A, "Fill Order");
                imageMetadataProperties.Add(0x010D, "Document Name");
                imageMetadataProperties.Add(0x010E, "Image Description");
                imageMetadataProperties.Add(0x010F, "Make");
                imageMetadataProperties.Add(0x0110, "Model");
                imageMetadataProperties.Add(0x0111, "Strip Offsets");
                imageMetadataProperties.Add(0x0112, "Orientation");
                imageMetadataProperties.Add(0x0115, "Samples Per Pixel");
                imageMetadataProperties.Add(0x0116, "Rows Per Strip");
                imageMetadataProperties.Add(0x0117, "Strip Byte Counts");
                imageMetadataProperties.Add(0x0118, "Min Sample Value");
                imageMetadataProperties.Add(0x0119, "Max Sample Value");
                imageMetadataProperties.Add(0x011A, "X Resolution");
                imageMetadataProperties.Add(0x011B, "Y Resolution");
                imageMetadataProperties.Add(0x011C, "Planar Configuration");
                imageMetadataProperties.Add(0x011D, "Page Name");
                imageMetadataProperties.Add(0x0128, "Resolution Unit");
                imageMetadataProperties.Add(0x0129, "Page Number");
                imageMetadataProperties.Add(0x012D, "Transfer Function");
                imageMetadataProperties.Add(0x0131, "Software");
                imageMetadataProperties.Add(0x0132, "Date Time");
                imageMetadataProperties.Add(0x013B, "Artist");
                imageMetadataProperties.Add(0x013D, "Predictor");
                imageMetadataProperties.Add(0x013E, "White Point");
                imageMetadataProperties.Add(0x013F, "Primary Chromaticities");
                imageMetadataProperties.Add(0x0140, "Color Map");
                imageMetadataProperties.Add(0x014C, "Ink Set");
                imageMetadataProperties.Add(0x0150, "Number Of Inks");
                imageMetadataProperties.Add(0x0152, "Dot Range");
                imageMetadataProperties.Add(0x0153, "Sample Format");
                imageMetadataProperties.Add(0x015F, "JPEG Lossless Predictors");
                imageMetadataProperties.Add(0x0201, "JPEG Interop Index");
                imageMetadataProperties.Add(0x0202, "JPEG Interop Length");
                imageMetadataProperties.Add(0x0211, "YCbCr Coefficients");
                imageMetadataProperties.Add(0x0212, "YCbCr Sub Sampling");
                imageMetadataProperties.Add(0x0213, "YCbCr Positioning");
                imageMetadataProperties.Add(0x0214, "Reference Black White");
                imageMetadataProperties.Add(0x4746, "Rating");
                imageMetadataProperties.Add(0x5039, "Proprietary Tag 0x5039");
                imageMetadataProperties.Add(0x5090, "Luminance Table");
                imageMetadataProperties.Add(0x5091, "Chrominance Table");
                imageMetadataProperties.Add(0x828D, "CFA Repeat Pattern Dim");
                imageMetadataProperties.Add(0x828E, "CFA Pattern");
                imageMetadataProperties.Add(0x8298, "Copyright");
                imageMetadataProperties.Add(0x829A, "Exposure Time");
                imageMetadataProperties.Add(0x829D, "F Number");
                imageMetadataProperties.Add(0x8769, "Exif IFD Pointer");
                imageMetadataProperties.Add(0x8773, "Inter Color Profile");
                imageMetadataProperties.Add(0x8822, "Exposure Program");
                imageMetadataProperties.Add(0x8824, "Spectral Sensitivity");
                imageMetadataProperties.Add(0x8825, "GPS Info IFD Pointer");
                imageMetadataProperties.Add(0x8827, "ISO Speed Ratings");
                imageMetadataProperties.Add(0x8828, "OECF");
                imageMetadataProperties.Add(0x8830, "Sensitivity Type");
                imageMetadataProperties.Add(0x8831, "Standard Output Sensitivity");
                imageMetadataProperties.Add(0x8832, "Recommended Exposure Index");
                imageMetadataProperties.Add(0x8833, "ISO Speed");
                imageMetadataProperties.Add(0x8834, "ISO Speed Latitude yyy");
                imageMetadataProperties.Add(0x8835, "ISO Speed Latitude zzz");
                imageMetadataProperties.Add(0x9000, "Exif Version");
                imageMetadataProperties.Add(0x9003, "Date Time Original");
                imageMetadataProperties.Add(0x9004, "Date Time Digitized");
                imageMetadataProperties.Add(0x9010, "Offset Time");
                imageMetadataProperties.Add(0x9011, "Offset Time Original");
                imageMetadataProperties.Add(0x9012, "Offset Time Digitized");
                imageMetadataProperties.Add(0x9101, "Components Configuration");
                imageMetadataProperties.Add(0x9102, "Compressed Bits Per Pixel");
                imageMetadataProperties.Add(0x9201, "Shutter Speed Value");
                imageMetadataProperties.Add(0x9202, "Aperture Value");
                imageMetadataProperties.Add(0x9203, "Brightness Value");
                imageMetadataProperties.Add(0x9204, "Exposure Bias Value");
                imageMetadataProperties.Add(0x9205, "Max Aperture Value");
                imageMetadataProperties.Add(0x9206, "Subject Distance");
                imageMetadataProperties.Add(0x9207, "Metering Mode");
                imageMetadataProperties.Add(0x9208, "Light Source");
                imageMetadataProperties.Add(0x9209, "Flash");
                imageMetadataProperties.Add(0x920A, "Focal Length");
                imageMetadataProperties.Add(0x9214, "Subject Area");
                imageMetadataProperties.Add(0x927C, "Maker Note");
                imageMetadataProperties.Add(0x9286, "User Comment");
                imageMetadataProperties.Add(0x9290, "Sub Sec Time");
                imageMetadataProperties.Add(0x9291, "Sub Sec Time Original");
                imageMetadataProperties.Add(0x9292, "Sub Sec Time Digitized");
                imageMetadataProperties.Add(0xA000, "Flashpix Version");
                imageMetadataProperties.Add(0xA001, "Color Space");
                imageMetadataProperties.Add(0xA002, "Pixel X Dimension");
                imageMetadataProperties.Add(0xA003, "Pixel Y Dimension");
                imageMetadataProperties.Add(0xA004, "Related Sound File");
                imageMetadataProperties.Add(0xA005, "Interoperability IFD Pointer");
                imageMetadataProperties.Add(0xA20B, "Flash Energy");
                imageMetadataProperties.Add(0xA20E, "Focal Plane X Resolution");
                imageMetadataProperties.Add(0xA20F, "Focal Plane Y Resolution");
                imageMetadataProperties.Add(0xA210, "Focal Plane Resolution Unit");
                imageMetadataProperties.Add(0xA214, "Subject Location");
                imageMetadataProperties.Add(0xA217, "Sensing Method");
                imageMetadataProperties.Add(0xA300, "File Source");
                imageMetadataProperties.Add(0xA301, "Scene Type");
                imageMetadataProperties.Add(0xA302, "CFA Pattern Format");
                imageMetadataProperties.Add(0xA401, "Custom Rendered");
                imageMetadataProperties.Add(0xA402, "Exposure Mode");
                imageMetadataProperties.Add(0xA403, "White Balance");
                imageMetadataProperties.Add(0xA404, "Digital Zoom Ratio");
                imageMetadataProperties.Add(0xA405, "Focal Length In 35mm Film");
                imageMetadataProperties.Add(0xA406, "Scene Capture Type");
                imageMetadataProperties.Add(0xA407, "Gain Control");
                imageMetadataProperties.Add(0xA408, "Contrast");
                imageMetadataProperties.Add(0xA409, "Saturation");
                imageMetadataProperties.Add(0xA40A, "Sharpness");
                imageMetadataProperties.Add(0xA40C, "Subject Distance Range");
                imageMetadataProperties.Add(0xA420, "Image Unique ID");
                imageMetadataProperties.Add(0xA430, "Camera Owner Name");
                imageMetadataProperties.Add(0xA431, "Body Serial Number");
                imageMetadataProperties.Add(0xA432, "Lens Specification");
                imageMetadataProperties.Add(0xA433, "Lens Make");
                imageMetadataProperties.Add(0xA434, "Lens Model");
                imageMetadataProperties.Add(0xA435, "Lens Serial Number");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em InitializeImageMetadataProperties: {ex.Message}");
            }
        }

        private async void SelectFile_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "All Files|*.jpg;*.jpeg;*.png;*.bmp;*.pdf;*.docx;*.exe|Images|*.jpg;*.jpeg;*.png;*.bmp|Documents|*.pdf;*.docx|Executables|*.exe",
                    Title = "Selecione um arquivo"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    selectedFilePath = openFileDialog.FileName;
                    await LoadMetadataAsync();
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Erro ao selecionar arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private async Task LoadMetadataAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(selectedFilePath))
                {
                    Dispatcher.Invoke(() => MessageBox.Show("Nenhum arquivo selecionado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                metadataItems.Clear();
                string? extension = Path.GetExtension(selectedFilePath)?.ToLower();

                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                        await Task.Run(LoadImageMetadata);
                        break;
                    case ".pdf":
                        await Task.Run(LoadPdfMetadata);
                        break;
                    case ".docx":
                        await Task.Run(LoadDocxMetadata);
                        break;
                    case ".exe":
                        await Task.Run(LoadExeMetadata);
                        break;
                    default:
                        Dispatcher.Invoke(() => MessageBox.Show("Formato de arquivo não suportado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error));
                        return;
                }

                Dispatcher.Invoke(() =>
                {
                    listViewMetadata.ItemsSource = null;
                    listViewMetadata.ItemsSource = metadataItems;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Erro ao carregar os metadados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void LoadImageMetadata()
        {
            try
            {
                using var stream = new FileStream(selectedFilePath!, FileMode.Open, FileAccess.Read);
                using var image = System.Drawing.Image.FromStream(stream);
                PropertyItem[] propItems = image.PropertyItems;

                foreach (PropertyItem propItem in propItems)
                {
                    string propName = imageMetadataProperties.TryGetValue(propItem.Id, out string? name)
                        ? name
                        : $"Unknown (ID: 0x{propItem.Id:X4})";
                    string value = GetPropertyValue(propItem);

                    metadataItems.Add(new MetadataItem
                    {
                        PropertyName = propName,
                        Value = value,
                        PropertyItem = propItem,
                        FileType = "Image"
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em LoadImageMetadata {ex.Message}");
                throw;
            }
        }

        private void LoadPdfMetadata()
        {
            try
            {
                using var reader = new PdfReader(selectedFilePath!);
                var info = reader.Info;
                foreach (var key in info.Keys)
                {
                    metadataItems.Add(new MetadataItem
                    {
                        PropertyName = key,
                        Value = info[key] ?? "N/A",
                        FileType = "PDF"
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em LoadPdfMetadata {ex.Message}");
                throw;
            }
        }

        private void LoadDocxMetadata()
        {
            try
            {
                using var package = Package.Open(selectedFilePath!, FileMode.Open, FileAccess.Read);
                var props = package.PackageProperties;
                metadataItems.AddRange(new[]
                {
                    new MetadataItem { PropertyName = "Title", Value = props.Title ?? "N/A", FileType = "DOCX" },
                    new MetadataItem { PropertyName = "Subject", Value = props.Subject ?? "N/A", FileType = "DOCX" },
                    new MetadataItem { PropertyName = "Creator", Value = props.Creator ?? "N/A", FileType = "DOCX" },
                    new MetadataItem { PropertyName = "Keywords", Value = props.Keywords ?? "N/A", FileType = "DOCX" },
                    new MetadataItem { PropertyName = "Description", Value = props.Description ?? "N/A", FileType = "DOCX" },
                    new MetadataItem { PropertyName = "LastModifiedBy", Value = props.LastModifiedBy ?? "N/A", FileType = "DOCX" },
                    new MetadataItem { PropertyName = "Revision", Value = props.Revision ?? "N/A", FileType = "DOCX" },
                    new MetadataItem { PropertyName = "Created", Value = props.Created?.ToString() ?? "N/A", FileType = "DOCX" },
                    new MetadataItem { PropertyName = "Modified", Value = props.Modified?.ToString() ?? "N/A", FileType = "DOCX" }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em LoadDocxMetadata {ex.Message}");
                throw;
            }
        }

        private void LoadExeMetadata()
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(selectedFilePath!);
                metadataItems.AddRange(new[]
                {
                    new MetadataItem { PropertyName = "File Description", Value = versionInfo.FileDescription ?? "N/A", FileType = "EXE" },
                    new MetadataItem { PropertyName = "Product Name", Value = versionInfo.ProductName ?? "N/A", FileType = "EXE" },
                    new MetadataItem { PropertyName = "Company Name", Value = versionInfo.CompanyName ?? "N/A", FileType = "EXE" },
                    new MetadataItem { PropertyName = "File Version", Value = versionInfo.FileVersion ?? "N/A", FileType = "EXE" },
                    new MetadataItem { PropertyName = "Product Version", Value = versionInfo.ProductVersion ?? "N/A", FileType = "EXE" },
                    new MetadataItem { PropertyName = "Copyright", Value = versionInfo.LegalCopyright ?? "N/A", FileType = "EXE" },
                    new MetadataItem { PropertyName = "Comments", Value = versionInfo.Comments ?? "N/A", FileType = "EXE" }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em LoadExeMetadata {ex.Message}");
                throw;
            }
        }

        private string GetPropertyValue(PropertyItem propItem)
        {
            try
            {
                switch (propItem.Type)
                {
                    case 1:  
                        return propItem.Value[0].ToString();
                    case 2:  
                        return Encoding.ASCII.GetString(propItem.Value).TrimEnd('\0');
                    case 3:  
                        ushort shortValue = BitConverter.ToUInt16(propItem.Value, 0);
                        return propItem.Id == 0x0112 ? GetOrientationDescription(shortValue) :
                               propItem.Id == 0x8822 ? GetExposureProgramDescription(shortValue) :
                               propItem.Id == 0x9207 ? GetMeteringModeDescription(shortValue) :
                               propItem.Id == 0x9208 ? GetLightSourceDescription(shortValue) :
                               propItem.Id == 0x9209 ? GetFlashDescription(shortValue) : shortValue.ToString();
                    case 4:  
                        return BitConverter.ToUInt32(propItem.Value, 0).ToString();
                    case 5:  
                        uint num = BitConverter.ToUInt32(propItem.Value, 0);
                        uint den = BitConverter.ToUInt32(propItem.Value, 4);
                        return den == 0 ? "Invalid Rational" : $"{num}/{den} ({(double)num / den})";
                    case 7:  
                        if (propItem.Id == 0xA000)   
                            return Encoding.ASCII.GetString(propItem.Value).TrimEnd('\0');
                        else if (propItem.Id == 0x927C)   
                            return BitConverter.ToString(propItem.Value);
                        return BitConverter.ToString(propItem.Value);
                    case 10:  
                        int sNum = BitConverter.ToInt32(propItem.Value, 0);
                        int sDen = BitConverter.ToInt32(propItem.Value, 4);
                        return sDen == 0 ? "Invalid SRational" : $"{sNum}/{sDen} ({(double)sNum / sDen})";
                    default:
                        return $"Type {propItem.Type} Not Supported";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em GetPropertyValue: {ex.Message}");
                return "Error reading value";
            }
        }

        private static string GetOrientationDescription(ushort orientation)
        {
            return orientation switch
            {
                1 => "Top-left",
                2 => "Top-right",
                3 => "Bottom-right",
                4 => "Bottom-left",
                5 => "Left-top",
                6 => "Right-top",
                7 => "Right-bottom",
                8 => "Left-bottom",
                _ => $"Unknown Orientation ({orientation})"
            };
        }

        private static string GetExposureProgramDescription(ushort program)
        {
            return program switch
            {
                0 => "Not defined",
                1 => "Manual",
                2 => "Normal program",
                3 => "Aperture priority",
                4 => "Shutter priority",
                5 => "Creative program",
                6 => "Action program",
                7 => "Portrait mode",
                8 => "Landscape mode",
                _ => $"Unknown Exposure Program ({program})"
            };
        }

        private static string GetMeteringModeDescription(ushort mode)
        {
            return mode switch
            {
                0 => "Unknown",
                1 => "Average",
                2 => "Center weighted average",
                3 => "Spot",
                4 => "Multi-spot",
                5 => "Pattern",
                6 => "Partial",
                255 => "Other",
                _ => $"Unknown Metering Mode ({mode})"
            };
        }

        private static string GetLightSourceDescription(ushort source)
        {
            return source switch
            {
                0 => "Unknown",
                1 => "Daylight",
                2 => "Fluorescent",
                3 => "Tungsten",
                4 => "Flash",
                9 => "Fine weather",
                10 => "Cloudy weather",
                11 => "Shade",
                12 => "Daylight fluorescent",
                13 => "Day white fluorescent",
                14 => "Cool white fluorescent",
                15 => "White fluorescent",
                17 => "Standard light A",
                18 => "Standard light B",
                19 => "Standard light C",
                20 => "D55",
                21 => "D65",
                22 => "D75",
                23 => "D50",
                24 => "ISO studio tungsten",
                255 => "Other light source",
                _ => $"Unknown Light Source ({source})"
            };
        }

        private static string GetFlashDescription(ushort flash)
        {
            return flash switch
            {
                0x0000 => "Flash did not fire",
                0x0001 => "Flash fired",
                0x0005 => "Flash fired, strobe return light not detected",
                0x0007 => "Flash fired, strobe return light detected",
                0x0009 => "Flash fired, compulsory flash mode",
                0x000D => "Flash fired, compulsory flash mode, return light not detected",
                0x000F => "Flash fired, compulsory flash mode, return light detected",
                0x0010 => "Flash did not fire, compulsory flash mode",
                0x0018 => "Flash did not fire, auto mode",
                0x0019 => "Flash fired, auto mode",
                0x001D => "Flash fired, auto mode, return light not detected",
                0x001F => "Flash fired, auto mode, return light detected",
                0x0020 => "No flash function",
                0x0041 => "Flash fired, red-eye reduction mode",
                0x0045 => "Flash fired, red-eye reduction mode, return light not detected",
                0x0047 => "Flash fired, red-eye reduction mode, return light detected",
                0x0049 => "Flash fired, compulsory flash mode, red-eye reduction mode",
                0x004D => "Flash fired, compulsory flash mode, red-eye reduction mode, return light not detected",
                0x004F => "Flash fired, compulsory flash mode, red-eye reduction mode, return light detected",
                0x0059 => "Flash fired, auto mode, red-eye reduction mode",
                0x005D => "Flash fired, auto mode, return light not detected, red-eye reduction mode",
                0x005F => "Flash fired, auto mode, return light detected, red-eye reduction mode",
                _ => $"Unknown Flash ({flash})"
            };
        }

        private async void SaveMetadata_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedFilePath))
                {
                    Dispatcher.Invoke(() => MessageBox.Show("Nenhum arquivo selecionado para salvar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                string? extension = Path.GetExtension(selectedFilePath)?.ToLower();
                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".bmp":
                        await Task.Run(SaveImageMetadata);
                        break;
                    case ".pdf":
                        await Task.Run(SavePdfMetadata);
                        break;
                    case ".docx":
                        await Task.Run(SaveDocxMetadata);
                        break;
                    case ".exe":
                        await Task.Run(SaveExeMetadata);
                        break;
                    default:
                        Dispatcher.Invoke(() => MessageBox.Show("Formato de arquivo não suportado para salvamento.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error));
                        return;
                }
                Dispatcher.Invoke(() => MessageBox.Show("Metadados salvos com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (UnauthorizedAccessException ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Erro de permissão ao salvar os metadados. Execute o programa como administrador.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error));
                Debug.WriteLine($"Erro em SaveMetadata_Click (UnauthorizedAccessException) {ex.Message}");
            }
            catch (IOException ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Erro de I/O ao salvar os metadados: {ex.Message}. Verifique se o arquivo não está aberto em outro programa.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error));
                Debug.WriteLine($"Erro em SaveMetadata_Click (IOException) {ex.Message}");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Erro ao salvar os metadados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error));
                Debug.WriteLine($"Erro em SaveMetadata_Click (Exception) {ex.Message}");
            }
        }

        private void SaveImageMetadata()
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(selectedFilePath!);
                using var stream = new MemoryStream(imageBytes);
                using var image = System.Drawing.Image.FromStream(stream);
                foreach (MetadataItem item in metadataItems)
                {
                    if (item.FileType != "Image" || item.PropertyItem == null) continue;
                    PropertyItem propItem = item.PropertyItem;

                    switch (propItem.Type)
                    {
                        case 2:  
                            byte[] asciiBytes = Encoding.ASCII.GetBytes(item.Value + '\0');
                            propItem.Value = asciiBytes;
                            break;
                        case 3:  
                            if (ushort.TryParse(item.Value, out ushort shortValue))
                                propItem.Value = BitConverter.GetBytes(shortValue);
                            break;
                        case 4:  
                            if (uint.TryParse(item.Value, out uint longValue))
                                propItem.Value = BitConverter.GetBytes(longValue);
                            break;
                        case 5:  
                            string[] rationalParts = item.Value.Split('/');
                            if (rationalParts.Length == 2 &&
                                uint.TryParse(rationalParts[0], out uint num) &&
                                uint.TryParse(rationalParts[1], out uint den))
                            {
                                byte[] rationalBytes = new byte[8];
                                Array.Copy(BitConverter.GetBytes(num), 0, rationalBytes, 0, 4);
                                Array.Copy(BitConverter.GetBytes(den), 0, rationalBytes, 4, 4);
                                propItem.Value = rationalBytes;
                            }
                            break;
                        case 7:      
                            continue;
                        default:
                            continue;
                    }
                    image.SetPropertyItem(propItem);
                }

                string tempFilePath = Path.Combine(Path.GetDirectoryName(selectedFilePath)!, "temp_" + Path.GetFileName(selectedFilePath));
                image.Save(tempFilePath);
                File.Copy(tempFilePath, selectedFilePath!, true);
                File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em SaveImageMetadata {ex.Message}");
                throw;
            }
        }

        private void SavePdfMetadata()
        {
            try
            {
                using var reader = new PdfReader(selectedFilePath!);
                using var stream = new FileStream(selectedFilePath + ".temp", FileMode.Create, FileAccess.Write);
                using var stamper = new PdfStamper(reader, stream);
                var info = reader.Info;
                foreach (MetadataItem item in metadataItems)
                {
                    if (item.FileType != "PDF") continue;
                    info[item.PropertyName] = item.Value;
                }
                stamper.MoreInfo = info;
                stamper.Close();
                File.Copy(selectedFilePath + ".temp", selectedFilePath!, true);
                File.Delete(selectedFilePath + ".temp");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em SavePdfMetadata {ex.Message}");
                throw;
            }
        }

        private void SaveDocxMetadata()
        {
            try
            {
                using var package = Package.Open(selectedFilePath!, FileMode.Open, FileAccess.ReadWrite);
                var props = package.PackageProperties;
                foreach (MetadataItem item in metadataItems)
                {
                    if (item.FileType != "DOCX") continue;
                    switch (item.PropertyName)
                    {
                        case "Title":
                            props.Title = item.Value;
                            break;
                        case "Subject":
                            props.Subject = item.Value;
                            break;
                        case "Creator":
                            props.Creator = item.Value;
                            break;
                        case "Keywords":
                            props.Keywords = item.Value;
                            break;
                        case "Description":
                            props.Description = item.Value;
                            break;
                        case "LastModifiedBy":
                            props.LastModifiedBy = item.Value;
                            break;
                        case "Revision":
                            props.Revision = item.Value;
                            break;
                        case "Created":
                            if (DateTime.TryParse(item.Value, out DateTime created))
                                props.Created = created;
                            break;
                        case "Modified":
                            if (DateTime.TryParse(item.Value, out DateTime modified))
                                props.Modified = modified;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em SaveDocxMetadata: {ex.Message}");
                throw;
            }
        }

        private void SaveExeMetadata()
        {
            try
            {
                Dispatcher.Invoke(() => MessageBox.Show("Edição de metadados de executáveis não é totalmente suportada nesta versão devido a limitações do GDI+. Apenas leitura disponível.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
            catch
            {
            }
        }
    }

    public class MetadataItem
    {
        public string? PropertyName { get; set; }
        public string? Value { get; set; }
        public PropertyItem? PropertyItem { get; set; }
        public string? FileType { get; set; }
    }
}