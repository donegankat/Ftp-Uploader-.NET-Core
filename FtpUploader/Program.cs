using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using System;
using System.IO;

namespace FtpUploader
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }

        #region FTP Variables
        #endregion

        private static Settings _loadAppSettings(IConfiguration config)
        {
            var settings = new Settings();

            // Read configuration
            var ftpDefaults = config.GetSection("ftpDefaults");
            settings.LocalFileName = ftpDefaults["localFileName"];
            settings.LocalFileDirectory = ftpDefaults["localFileDirectory"];
            settings.DestinationFtpSite = ftpDefaults["destinationFtpSite"];
            settings.DestinationFileDirectory = ftpDefaults["destinationFileDirectory"];
            settings.FtpUserName = ftpDefaults["ftpUserName"];
            settings.FtpPassword = ftpDefaults["ftpPassword"];

            if (!string.IsNullOrWhiteSpace(ftpDefaults["ftpPort"]))
            {
                bool validPort;
                validPort = int.TryParse(ftpDefaults["ftpPort"], out int parsedPort); // TODO: Do something with this validation
                if (validPort)
                    settings.FtpPort = parsedPort;
            }
            else
            {
                settings.FtpPort = null;
            }

            return settings;
        }

        static void Main(string[] args)
        {
            // Adding JSON file into IConfiguration.
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                ;

            var config = builder.Build();

            
            Settings _settings = new Settings();

            // TODO: Decide whether I want to prompt for whether the appSettings.json should be used or if I just want to specify the useAppSettings bool in that file.

            while (true) // Loop until we get valid input
            {
                // Prompt user for whether they want to manually provide the FTP info via the console or if they want to read from appSettings.json
                Console.WriteLine("Do you want to load the file and FTP information from appSettings.json? Y/N (Default is Y)");
                var input = Console.ReadLine().ToUpper();

                if (input == "Y" || string.IsNullOrWhiteSpace(input)) // If the user entered Y or just pressed enter without input, load from appSettings
                {
                    _settings = _loadAppSettings(config);
                    _settings.UseAppSettingsFtp = true;
                    
                    break; // Don't continue to prompt for proper input
                }
                else if (input == "N")
                {
                    // TODO: Make sure the user provides valid input for all of these. I'm being lazy right now, though.
                    // TODO: For the file directory and path, make sure that the user provides valid info.
                    Console.WriteLine();

                    Console.WriteLine("Enter the local filename (NOT INCLUDING THE DIRECTORY PATH) for the file you wish to upload:");
                    _settings.LocalFileName = Console.ReadLine();

                    Console.WriteLine("Enter the local directory (NOT INCLUDING THE FILENAME) for the file you wish to upload:");
                    _settings.LocalFileDirectory = Console.ReadLine();

                    Console.WriteLine("Enter the host address for the destination FTP site:");
                    _settings.DestinationFtpSite = Console.ReadLine();

                    Console.WriteLine("Enter the file directory for the destination FTP site:");
                    _settings.DestinationFileDirectory = Console.ReadLine();

                    Console.WriteLine("Enter the user name for the destination FTP site:");
                    _settings.FtpUserName = Console.ReadLine();

                    Console.WriteLine("Enter the password for the destination FTP site:");
                    _settings.FtpPassword = Console.ReadLine();

                    Console.WriteLine("(OPTIONAL) Enter the port for the destination FTP site:");
                    input = Console.ReadLine();

                    if (!string.IsNullOrWhiteSpace(input)) // If the user entered a value for the port, try and parse it to make sure it's a valid integer
                    {
                        bool validPort;
                        validPort = int.TryParse(input, out int parsedPort); // TODO: Do something with this validation
                        if (validPort)
                            _settings.FtpPort = parsedPort;
                    }
                    else
                    {
                        _settings.FtpPort = null;
                    }

                    // Do a little bit of cleaning up
                    if (_settings.LocalFileName.EndsWith("/")) // Ensure the filename does NOT end with /
                    {
                        _settings.LocalFileName = _settings.LocalFileName.Remove(_settings.LocalFileName.LastIndexOf("/"));
                    }

                    break; // Don't continue to prompt for proper input
                }

                // If we reached this point then the user did not provide valid input. Continue the while() loop until we get something valid.
            }

            // TODO: Figure out how I'm handling all of these variables and actually set them properly both in the declaration and all throughout the rest of the file

            Uploader uploader = new Uploader(_settings);

            bool success = uploader.UploadSFTP(); // Upload the file from the specified directory to the destination directory we just defined in the previous step.

            if (!success)
            {
                // Do something to note that failure happened.
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
