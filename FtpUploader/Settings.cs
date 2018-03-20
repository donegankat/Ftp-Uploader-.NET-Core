using System;
using System.Collections.Generic;
using System.Text;

namespace FtpUploader
{
    public class Settings
    {
        public bool UseAppSettingsFtp { get; set; } // True or false - whether we should read the rest of the settings from appSettings.json

        public string LocalFileName { get; set; } // The local file we're uploading
        public string LocalFileDirectory { get; set; } // The local directory containing the file to be uploaded
        public string DestinationFtpSite { get; set; } // The destination host address
        public string DestinationFileDirectory { get; set; } // The destination folder. This should not begin with '/', but it can end with '/'
        public string FtpUserName { get; set; }
        public string FtpPassword { get; set; }
        public int? FtpPort { get; set; } // Not typically needed
        public bool FtpIsSSL { get; set; } // Determines whether to use SFTP or FTP protocol
        public string FtpSSHKey { get; set; } // Only used if we're doing an SFTP transfer
    }
}
