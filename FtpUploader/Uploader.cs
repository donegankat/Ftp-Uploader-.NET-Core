using System;
using System.Collections.Generic;
using System.Text;
using WinSCP;
using Microsoft.Extensions.Options;

namespace FtpUploader
{
    public class Uploader
    {
        #region General Variables
        private Settings _settings;
        #endregion

        #region FTP Site Variables

        // TODO: Figure out how I'm handling all of these variables and actually set them properly both in the declaration and all throughout the rest of the file
        private string localFileName; // The local file we're uploading
        private string localFileDirectory; // The local directory containing the file to be uploaded
        private string ftpSite; // The destination host address
        private string ftpDirectory; // The destination folder. This should not begin with '/', but it can end with '/'
        private string ftpUserName;
        private string ftpPassword;
        private int? ftpPort = null; // Not typically needed
        private bool ftpIsSSL = false; // Determines whether to use SFTP or FTP protocol
        private string ftpSSH_Key; // Only used if we're doing an SFTP transfer

        #endregion

        #region Console Logging

        private void Log(LogTypes type, string text)
        {
            switch(type)
            {
                case LogTypes.Success:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogTypes.Log:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogTypes.Debug:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogTypes.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default: // Shouldn't actually hit this
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

            Console.WriteLine(text);
        }

        #endregion

        public Uploader(Settings appSettings)
        {
            _settings = appSettings;

            // This is all handled in Program.cs now
            //_settings.UseAppSettingsFtp = appSettings.Value.UseAppSettingsFtp;

            //if (_settings.UseAppSettingsFtp) // If the config says that we should use the appSettings.json, set those settings according to what's in that file
            //{
                
            //}
            //else // If we're not using appSettings.json, use the variables provided by the user
            //{
            //    _settings.LocalFileName = localFileName;
            //    _settings.LocalFileDirectory = localFileDirectory;
            //    _settings.DestinationFtpSite = ftpSite;
            //    _settings.DestinationFileDirectory = ftpDirectory;
            //    _settings.FtpUserName = ftpUserName;
            //    _settings.FtpPassword = ftpPassword;
            //    _settings.FtpPort = ftpPort;
            //}
        }


        #region Upload to FTP Site

        /// <summary>
        /// Upload the file to the designated FTP location
        /// </summary>
        /// <returns></returns>
        public bool UploadSFTP(string sourcePath, string fileName)
        {
            bool success = true;

            // Begin upload process
            Log(LogTypes.Log, "Uploading file to FTP");

            try
            {
                // Setup WinSCP session options
                SessionOptions sessionOptions = new SessionOptions
                {
                    HostName = ftpSite,
                    UserName = ftpUserName,
                    Password = ftpPassword,
                };

                // Set the port, if applicable
                if (ftpPort != null)
                {
                    sessionOptions.PortNumber = (int)ftpPort;
                }

                // Set the SSH key, if applicable
                if (ftpIsSSL) // This is an SFTP protocol transfer
                {
                    sessionOptions.Protocol = Protocol.Sftp;

                    if (string.IsNullOrWhiteSpace(ftpSSH_Key)) // Make sure we have a value
                    {
                        // If this should be an SFTP transfer but we DON'T have the SSH key, we can't continue.
                        Log(LogTypes.Error, $"UploadSFTP failed - SSH protocol is missing the server SSH key for FTP site: {ftpSite}");
                        success = false;
                    }
                    else
                    {
                        sessionOptions.SshHostKeyFingerprint = ftpSSH_Key; // SSH key has value
                    }
                }
                else // This is a normal FTP protocol transfer
                {
                    sessionOptions.Protocol = Protocol.Ftp;
                }

                if (success)
                {
                    string destination = "";

                    // If we have a directory, set the destination to be [directory]/[file].
                    // If we don't have a directory, set the destination to just the file name.
                    if (!string.IsNullOrWhiteSpace(ftpDirectory))
                    {
                        destination = ftpDirectory;
                    }

                    destination += fileName;

                    using (Session session = new Session())
                    {
                        // Connect
                        session.Open(sessionOptions);

                        // Upload files
                        TransferOptions transferOptions = new TransferOptions();
                        transferOptions.TransferMode = TransferMode.Binary;

                        TransferOperationResult transferResult;
                        transferResult = session.PutFiles(sourcePath + @"\" + fileName, destination, false, transferOptions);

                        // Throw on any error
                        transferResult.Check();

                        // Print and log results
                        foreach (TransferEventArgs transfer in transferResult.Transfers)
                        {
                            Log(LogTypes.Success, $"UploadSFTP - Upload of {transfer.FileName} succeeded");
                        }
                    }
                }
            }
            catch (Exception ex) // Catch, show, and log any errors
            {
                Log(LogTypes.Error, $"UploadSFTP failed to upload file. Message: {ex.Message} Stack Trace: {ex.StackTrace}");
                success = false;
            }

            return success;
        }

        #endregion


        #region Helpers
        /// <summary>
        /// Parses a nullable int from a string. Returns null or an int value.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private int? ParseInt(string val)
        {
            int i;
            return int.TryParse(val, out i) ? (int?)i : null;
        }

        /// <summary>
        /// Ensure proper string format for the destination host site.
        /// </summary>
        /// <returns></returns>
        private bool FormatSite()
        {
            bool success = true;

            // Make sure we successfully retrieved data
            if (!string.IsNullOrWhiteSpace(ftpSite))
            {
                // The host can't end with '/'
                if (ftpSite.EndsWith("/"))
                {
                    ftpSite = ftpSite.Remove(ftpSite.LastIndexOf("/"));
                }

                // The host doesn't need to start with ftp:// or sftp://
                if (ftpSite.StartsWith("ftp://") || ftpSite.StartsWith("sftp://"))
                {
                    ftpSite = ftpSite.Replace("ftp://", "").Replace("sftp://", "");
                }
            }
            else
            {
                Log(LogTypes.Error, $"FormatSite - FTP host name is empty");
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Ensure proper string format for the destination directory.
        /// Directory should be in format: /DirectoryPath/SubDirectory/ (subdirectory is optional).
        /// Success isn't dependent upon the directory, so this returns void rather than bool.
        /// </summary>
        private void FormatDirectory()
        {
            if (!string.IsNullOrWhiteSpace(ftpDirectory))
            {
                // Make sure we weren't given a bad path with the wrong folder separators
                ftpDirectory = ftpDirectory.Replace(@"\", "/");

                // Make sure the directory starts with '/'
                if (!ftpDirectory.StartsWith("/"))
                {
                    ftpDirectory = "/" + ftpDirectory;
                }

                // Make sure the directory ends with '/'
                if (!ftpDirectory.EndsWith("/"))
                {
                    ftpDirectory += "/";
                }
            }
        }
        #endregion
    }
}
