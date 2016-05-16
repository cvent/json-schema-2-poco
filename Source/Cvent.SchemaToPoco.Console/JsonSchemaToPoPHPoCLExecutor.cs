using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cvent.SchemaToPoPHPo.Core;
using Cvent.SchemaToPoPHPo.Core.Types;
using Cvent.SchemaToPoPHPo.Types;
using NDesk.Options;

namespace Cvent.SchemaToPoPHPo.Console
{
    /// <summary>
    ///     Main executor class for the CLI.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class JsonSchemaToPoPHPoCLExecutor
    {
        /// <summary>
        ///     Main controller.
        /// </summary>
        private readonly JsonSchemaToPoPHPo _controller;

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
        public JsonSchemaToPoPHPoCLExecutor(IEnumerable<string> args)
        {
            _settings = ConfigureCommandLineOptions(args);
            
            _controller = new JsonSchemaToPoPHPo(_settings.Config);
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
                {"n=|namespace=", "Namespace containing all of the generated classes", ns => settings.Config.Namespace = ns},
                {"s=|schema=", "File path to the schema file", s => settings.Config.JsonSchemaFileLocation = s},
                {"o=|output=", "Directory to save files", fn => settings.Config.OutputDirectory = fn},
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
            return new JsonSchemaToPoPHPoCLExecutor(args).Execute();
        }
    }
}
