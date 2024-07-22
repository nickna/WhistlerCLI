using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistlerCLI
{
    public struct WslDistro
    {
        public int Id { get; set; }
        public Guid WslId { get; set; }
        public string DistroName { get; set; }
        public string PackageFamilyName { get; set; }
        public double TotalBytes { get; set; }

        public DateTime LastAccess { get; set; }

        public string LastAccessStr
        {
            get
            {
                if (LastAccess == DateTime.MinValue)
                    return "never";

                var elapsed = DateTime.Now - LastAccess;

                if (elapsed < TimeSpan.FromSeconds(60))
                    return "just now";

                if (elapsed < TimeSpan.FromMinutes(60))
                    return string.Format("{0:F0} minutes ago", elapsed.TotalMinutes);

                if (elapsed < TimeSpan.FromHours(24))
                    return string.Format("{0:F0} hours ago", elapsed.TotalHours);

                if (elapsed < TimeSpan.FromDays(2))
                    return "1 day ago";

                if (elapsed < TimeSpan.FromDays(30))
                    return string.Format("{0:D} days ago", elapsed.Days);

                if (elapsed < TimeSpan.FromDays(365))
                    return string.Format("{0:F0} months ago", Math.Floor((double)elapsed.Days / 30));

                return string.Format("{0:F1} years ago", Math.Floor((double)elapsed.Days / 365.25));
            }
        }


        public bool Default { get; set; }

        public string TotalSpace
        {
            get
            {
                if (TotalBytes < 1024L)
                    return $"{TotalBytes:F0} bytes";
                else if (TotalBytes < 1024L * 1024L)
                    return $"{(double)TotalBytes / 1024:F2} KiB";
                else if (TotalBytes < 1024L * 1024L * 1024L)
                    return $"{(double)TotalBytes / (1024L * 1024L):F2} MiB";
                else if (TotalBytes < 1024L * 1024L * 1024L * 1024L)
                    return $"{(double)TotalBytes / (1024L * 1024L * 1024L):F2} GiB";
                else
                    return $"{(double)TotalBytes / (1024L * 1024L * 1024L * 1024L):F2} TiB";
            }
        }


        public string BasePath { get; set; }
    }
}
