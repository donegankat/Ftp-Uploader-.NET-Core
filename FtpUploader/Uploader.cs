using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

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
                string ftpPrefix = "ftp://";

                if (_settings.FtpIsSSL) // This is an SFTP protocol transfer
                    ftpPrefix = "sftp://";

                string fullDestination = ftpPrefix + _settings.DestinationFtpSite + _settings.DestinationFileDirectory + _settings.LocalFileName;

                // Set the port, if applicable
                if (_settings.FtpPort.HasValue)
                    fullDestination += $":{_settings.FtpPort}";

                // Get the object used to communicate with the server.
                // Example reference: https://docs.microsoft.com/en-us/dotnet/framework/network-programming/how-to-upload-files-with-ftp
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fullDestination);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_settings.FtpUserName, _settings.FtpPassword);

                // Set the SSH key, if applicable
                if (_settings.FtpIsSSL && string.IsNullOrWhiteSpace(_settings.FtpSSHKey)) // If this is an SFTP transfer, make sure we have a value for the SSH Key
                {
                    // If this should be an SFTP transfer but we DON'T have the SSH key, we can't continue.
                    Log(LogTypes.Error, $"UploadSFTP failed - SSH protocol is missing the server SSH key for FTP site: {_settings.DestinationFtpSite}");
                    success = false;
                }
                else
                {
                    // TODO: Need to actually figure this out
                    //request.ClientCertificates.Add(new System.Security.Cryptography.X509Certificates.X509Certificate(_settings.FtpSSHKey)); // SSH key has value
                }

                // Copy the contents of the file to the request stream.  
                StreamReader sourceStream = new StreamReader(_settings.LocalFileDirectory + _settings.LocalFileName);
                byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
                sourceStream.Close();
                request.ContentLength = fileContents.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(fileContents, 0, fileContents.Length);
                requestStream.Close();

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);

                response.Close();
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
        /// Calls the _formatDirectory helper function on both the local directory path and remote destination directory
        /// </summary>
        private void _formatDirectory()
        {
            _settings.LocalFileDirectory = _formatDirectory(_settings.LocalFileDirectory);
            _settings.DestinationFileDirectory = _formatDirectory(_settings.DestinationFileDirectory, true);
        }

        /// <summary>
        /// Ensure proper string format for the destination directory.
        /// Directory should be in format: /DirectoryPath/SubDirectory/ (subdirectory is optional).
        /// Success isn't dependent upon the directory, so this returns void rather than bool.
        /// </summary>
        private string _formatDirectory(string directoryPath, bool isRemote = false)
        {
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                // Make sure we weren't given a bad path with the wrong folder separators
                directoryPath = directoryPath.Replace(@"\", "/");

                // Make sure the REMOTE (NOT local) directory doesn't start with ftp://, sftp://, or ftps:// (we'll add it in later)
                // Also make sure the REMOTE directory begins with /
                if (isRemote)
                {
                    if (Regex.IsMatch(directoryPath, @"^(ftp|sftp|ftps):\/\/.*", RegexOptions.IgnoreCase)) // Don't begin the remote directory path with ftp/ftps/sftp
                        directoryPath = Regex.Replace(directoryPath, @"^(ftp|sftp|ftps):\/\/", "", RegexOptions.IgnoreCase);

                    if (!directoryPath.StartsWith("/")) // Ensure the directory starts with /
                        directoryPath = "/" + directoryPath;
                }

                // Make sure the directory ends with '/'
                if (!directoryPath.EndsWith("/"))
                {
                    directoryPath += "/";
                }
            }

            return directoryPath;
        }
        #endregion
    }
}
