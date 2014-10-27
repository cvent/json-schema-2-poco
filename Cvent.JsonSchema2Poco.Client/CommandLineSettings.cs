using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NDesk.Options;

namespace Cvent.JsonSchema2Poco
{
    /// <summary>
    /// Command line settings.
    /// </summary>
    internal class CommandLineSettings : IGenerationConfig
    {

        public HashSet<Uri> Sources { get; private set; }
        public string TargetDirectory { get; private set; }
        public string Namespace { get; private set; }
        public bool UsePartialClasses { get; private set; }
        public bool UseLongsForInts { get; private set; }
        public bool UseDoublesForFloats { get; private set; }
        public bool RemoveOldOutput { get; private set; }

        public CommandLineSettings()
        {
            Sources = new HashSet<Uri>();
            Namespace = "Generated";
            TargetDirectory = Directory.GetCurrentDirectory();
            RemoveOldOutput = false;
        }

        public static CommandLineSettings ParseCommandLineParameters(IEnumerable<string> args)
        {
            var settings = new CommandLineSettings();
            var sources = string.Empty;
            var showHelp = false;
            var options = new OptionSet
            {
                {"n=|namespace=", "Namespace containing all of the generated classes", ns => settings.Namespace = ns},
                {"s=|schemas=", "File path to the schema file", s => sources = s},
                {"o=|output=", "Directory to save files", fn => settings.TargetDirectory = fn},
                {
                    "l|long", "Indicates if longs should be used instead of ints for integers.",
                    fn => settings.UseLongsForInts = fn != null
                },
                {
                    "f|floats", "Indicates if doubles should be used instead of floats for numbers.",
                    fn => settings.UseDoublesForFloats = fn != null
                },
                {
                    "p|partial", "Flag to set if the classes generated will be partial.",
                    fn => settings.UsePartialClasses = fn != null
                },
                {
                    "r|remove", "Flag to remove files in output directory before generation.",
                    fn => settings.RemoveOldOutput = fn != null
                },
                {"?|help", "Show this help message", h => showHelp = !string.IsNullOrWhiteSpace(h)}
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try --help for more information.");
                Environment.Exit((int) ExitCodes.InvalidInputParameters);
            }

            if (showHelp)
            {
                var description =
                    new StringBuilder("JSON schema to POCO\nhttps://github.com/cvent/json-schema-2-poco\n\n");
                options.WriteOptionDescriptions(new StringWriter(description));
                Console.WriteLine(description.ToString());
                Environment.Exit((int) ExitCodes.Success);
            }

            if (Directory.Exists(sources))
            {
                var schemaFiles = Directory.GetFiles(sources, "*.json", SearchOption.AllDirectories);
                schemaFiles.ToList().ForEach(file => settings.Sources.Add(new Uri(file, UriKind.RelativeOrAbsolute)));
            }
            else
            {
                settings.Sources.Add(new Uri(sources, UriKind.RelativeOrAbsolute));
            }

            return settings;
        }
    }
}
