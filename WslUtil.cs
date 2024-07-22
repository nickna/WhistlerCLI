using Microsoft.Win32;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace WhistlerCLI
{
    public class WslUtil : IWslUtil
    {

        public List<WslDistro> WslDistros
        {
            get
            {
                List<WslDistro> distros = new List<WslDistro>();
                const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";

                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                    using (var key = baseKey.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        if (key == null)
                        {
                            AnsiConsole.MarkupLine($"Error: The registry key {keyPath} does not exist or cannot be accessed.");
                            return distros;
                        }

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey == null) continue;

                                var distro = new WslDistro
                                {
                                    Id = distros.Count + 1,
                                    WslId = Guid.Parse(subKeyName),
                                    DistroName = subKey.GetValue("DistributionName", string.Empty) as string,
                                    PackageFamilyName = subKey.GetValue("PackageFamilyName", string.Empty) as string,
                                    BasePath = subKey.GetValue("BasePath", string.Empty) as string,
                                    TotalBytes = GetDiskSpace(subKey.GetValue("BasePath", string.Empty) as string),
                                    LastAccess = GetLastAccess(subKey.GetValue("BasePath", string.Empty) as string)
                                };
                                if (distro.WslId == GetDefaultWSLDistro())
                                {
                                    distro.Default = true;
                                }
                                distros.Add(distro);
                            }
                        }
                    }
                }
                catch (SecurityException)
                {
                    Console.WriteLine("SecurityException: Insufficient permissions to access the registry key.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An exception occurred: {ex.Message}");
                }

                return distros;
            }
        }

        public long GetDiskSpace(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return 0;
            }

            // Check if the path exists
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Directory does not exist.");
                return 0;
            }

            long totalBytes = 0;

            // Get all file names in the directory, including subdirectories
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                try
                {
                    // FileInfo provides properties and instance methods for files
                    FileInfo fileInfo = new FileInfo(file);
                    totalBytes += fileInfo.Length;
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., file is being used by another process)
                    Console.WriteLine($"Could not access {file}: {ex.Message}");
                }
            }

            return totalBytes;
        }

        public bool UpdateWSLDistroName(string distroName, string newName)
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";

            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (key == null)
                    {
                        AnsiConsole.MarkupLine($"Error: The registry key {keyPath} does not exist or cannot be accessed.");
                        return false;
                    }

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName, true))
                        {
                            if (subKey == null) continue;

                            if (subKey.GetValue("DistributionName", string.Empty) as string == distroName)
                            {
                                subKey.SetValue("DistributionName", newName);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (SecurityException)
            {
                AnsiConsole.MarkupLine("SecurityException: Insufficient permissions to access the registry key.");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"An exception occurred: {ex.Message}");
            }
            return false;
        
        }
        public Guid GetDefaultWSLDistro()
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";

            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(keyPath, RegistryKeyPermissionCheck.Default))
                {
                    if (key == null)
                    {
                        Console.WriteLine($"The registry key {keyPath} does not exist or cannot be accessed.");
                        return Guid.Empty;
                    }

                    string value = key.GetValue("DefaultDistribution", string.Empty).ToString();
                    if (Guid.TryParse(value, out Guid result))
                    {
                        return result;
                    }
                }
            }
            catch (SecurityException)
            {
                Console.WriteLine("SecurityException: Insufficient permissions to access the registry key.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred: {ex.Message}");
            }

            return Guid.Empty;
        }

        public DateTime GetLastAccess(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return DateTime.MinValue;
            }

            // Check if the path exists
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Directory does not exist.");
                return DateTime.MinValue;
            }

            // Look for VHD or VHDx files in the directory
            string[] files = Directory.GetFiles(path, "*.vhdx", SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                files = Directory.GetFiles(path, "*.vhd", SearchOption.AllDirectories);
            }

            // Get the last access time of the first file found
            if (files.Length > 0)
            {
                return File.GetLastAccessTime(files[0]);
            }

            return DateTime.MinValue;

        }
    }
}
