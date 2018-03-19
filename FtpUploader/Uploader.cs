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

            if (type == LogTypes.Error)
            {
                throw new Exception("ERROR ENCOUNTERED");
            }
        }

        #endregion

        public Uploader(Settings appSettings)
        {
            _settings = appSettings;

            // Ensure proper string format for the destination host site.
            _formatSite();

            // Ensure proper string format for the destination directory.
            // Directory should be in format: /DirectoryPath/SubDirectory/ (subdirectory is optional).
            // Success isn't dependent upon the directory, so don't set success to equal the result.
            _formatDirectory();
        }


        #region Upload to FTP Site

        /// <summary>
        /// Upload the file to the designated FTP location
        /// </summary>
        /// <returns></returns>
        public bool UploadSFTP()
        {
            bool success = true;

            // Begin upload process
            Log(LogTypes.Log, "Uploading file to FTP");

            try
            {
                // Setup WinSCP session options
                SessionOptions sessionOptions = new SessionOptions
                {
                    HostName = _settings.DestinationFtpSite,
                    UserName = _settings.FtpUserName,
                    Password = _settings.FtpPassword,
                };

                // Set the port, if applicable
                if (_settings.FtpPort.HasValue)
                {
                    sessionOptions.PortNumber = _settings.FtpPort.Value;
                }

                // Set the SSH key, if applicable
                if (_settings.FtpIsSSL) // This is an SFTP protocol transfer
                {
                    sessionOptions.Protocol = Protocol.Sftp;

                    if (string.IsNullOrWhiteSpace(_settings.FtpSSHKey)) // Make sure we have a value
                    {
                        // If this should be an SFTP transfer but we DON'T have the SSH key, we can't continue.
                        Log(LogTypes.Error, $"UploadSFTP failed - SSH protocol is missing the server SSH key for FTP site: {_settings.DestinationFtpSite}");
                        success = false;
                    }
                    else
                    {
                        sessionOptions.SshHostKeyFingerprint = _settings.FtpSSHKey; // SSH key has value
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
                    if (!string.IsNullOrWhiteSpace(_settings.DestinationFileDirectory))
                    {
                        destination = _settings.DestinationFileDirectory;
                    }

                    destination += _settings.LocalFileName;

                    using (Session session = new Session())
                    {
                        // Connect
                        session.Open(sessionOptions);

                        // Upload files
                        TransferOptions transferOptions = new TransferOptions();
                        transferOptions.TransferMode = TransferMode.Binary;

                        TransferOperationResult transferResult;
                        transferResult = session.PutFiles(_settings.LocalFileDirectory + @"\" + _settings.LocalFileName, destination, false, transferOptions);

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
        private int? _parseInt(string val)
        {
            int i;
            return int.TryParse(val, out i) ? (int?)i : null;
        }

        /// <summary>
        /// Ensure proper string format for the destination host site.
        /// </summary>
        /// <returns></returns>
        private void _formatSite()
        {
            // Make sure we successfully retrieved data
            if (!string.IsNullOrWhiteSpace(_settings.DestinationFtpSite))
            {
                // The host can't end with '/'
                if (_settings.DestinationFtpSite.EndsWith("/"))
                {
                    _settings.DestinationFtpSite = _settings.DestinationFtpSite.Remove(_settings.DestinationFtpSite.LastIndexOf("/"));
                }

                // The host doesn't need to start with ftp:// or sftp://
                if (_settings.DestinationFtpSite.StartsWith("ftp://") || _settings.DestinationFtpSite.StartsWith("sftp://"))
                {
                    _settings.DestinationFtpSite = _settings.DestinationFtpSite.Replace("ftp://", "").Replace("sftp://", "");
                }
            }
            else
            {
                Log(LogTypes.Error, $"FormatSite - FTP host name is empty");                
            }
        }

        /// <summary>
        /// Ensure proper string format for the destination directory.
        /// Directory should be in format: /DirectoryPath/SubDirectory/ (subdirectory is optional).
        /// Success isn't dependent upon the directory, so this returns void rather than bool.
        /// </summary>
        private void _formatDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_settings.DestinationFileDirectory))
            {
                // Make sure we weren't given a bad path with the wrong folder separators
                _settings.DestinationFileDirectory = _settings.DestinationFileDirectory.Replace(@"\", "/");

                // Make sure the directory starts with '/'
                if (!_settings.DestinationFileDirectory.StartsWith("/"))
                {
                    _settings.DestinationFileDirectory = "/" + _settings.DestinationFileDirectory;
                }

                // Make sure the directory ends with '/'
                if (!_settings.DestinationFileDirectory.EndsWith("/"))
                {
                    _settings.DestinationFileDirectory += "/";
                }
            }
        }
        #endregion
    }
}
