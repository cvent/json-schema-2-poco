using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cvent.SchemaToPoco.Core;
using Cvent.SchemaToPoco.Core.Types;
using Cvent.SchemaToPoco.Types;
using NDesk.Options;

namespace Cvent.SchemaToPoco.Console
{
    /// <summary>
    ///     Main executor class for the CLI.
    /// </summary>
    public class JsonSchemaToPocoCLExecutor
    {
        /// <summary>
        ///     Main controller.
        /// </summary>
        private readonly JsonSchemaToPoco _controller;

        /// <summary>
        ///     Arguments from command line, raw format.
        /// </summary>
        private OptionSet _options;

        /// <summary>
        ///     Arguments from command line.
        /// </summary>
        private readonly CommandLineSettings _settings;

        /// <summary>
        ///     Constructor taking in command line arguments.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public JsonSchemaToPocoCLExecutor(IEnumerable<string> args)
        {
            _settings = ConfigureCommandLineOptions(args);
            
            _controller = new JsonSchemaToPoco(_settings.Config);
        }

        /// <summary>
        ///     Configure command line options.
        /// </summary>
        /// <param name="arguements">Arguments from the command line.</param>
        /// <returns>The command line options.</returns>
        private CommandLineSettings ConfigureCommandLineOptions(IEnumerable<string> arguements)
        {
            var settings = new CommandLineSettings();

            _options = new OptionSet
            {
                {"n=|namespace=", "Namespace contaning all of the generated classes", ns => settings.Config.Namespace = ns},
                {"s=|schema=", "File path to the schema file", s => settings.Config.JsonSchemaFileLocation = s},
                {"o=|output=", "Directory to save files", fn => settings.Config.OutputDirectory = fn},
                {"l=|language=","Language to export (csharp or php)",  ln  => settings.Config.LanguageExportType = (LanguageExportType)Enum.Parse(typeof(LanguageExportType),ln,true)},
                {
                    "v|verbose", "Print out files in console without generating",
                    v => settings.Config.Verbose = !string.IsNullOrWhiteSpace(v)
                },
                {
                    "a=|attribute=", "Attribute type (1 - Default DataAnnotations, 2 - JSON.net compatible attributes",
                    fn => settings.Config.AttributeType = (AttributeType) Enum.Parse(typeof(AttributeType), fn)
                },
                {"?|help", "Show this help message", h => settings.ShowHelp = !string.IsNullOrWhiteSpace(h)}
            };

            //if the language parameter was not set, default it to csharp.
            if (settings.Config.LanguageExportType == 0)
            {
                settings.Config.LanguageExportType = LanguageExportType.CSharp;
            }

            _options.Parse(arguements);

            return settings;
        }

        /// <summary>
        ///     Main executor method.
        /// </summary>
        /// <returns>Status code.</returns>
        private int Execute()
        {
            if (_settings.ShowHelp)
            {
                var description =
                    new StringBuilder("JSON schema to POCO\nhttps://github.com/cvent/json-schema-2-poco\n\n");
                _options.WriteOptionDescriptions(new StringWriter(description));
                System.Console.WriteLine(description.ToString());

                return (int)ExitCodes.Ok;
            }

            return _controller.Execute();
        }

        /// <summary>
        ///     Main method.
        /// </summary>
        /// <param name="args">Arguments from command line.</param>
        /// <returns>An exit code.</returns>
        public static Int32 Main(string[] args)
        {
            return new JsonSchemaToPocoCLExecutor(args).Execute();
        }
    }
}
