using System;
using System.CodeDom;
using System.Collections.Generic;
using Cvent.SchemaToPoco.Core.CodeToLanguage;
using Cvent.SchemaToPoco.Core.Types;
using Cvent.SchemaToPoco.Core.Util;
using Cvent.SchemaToPoco.Core.Wrappers;
using Cvent.SchemaToPoco.Types;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;

namespace Cvent.SchemaToPoco.Core
{
    /// <summary>
    ///     Main controller.
    /// </summary>
    public class JsonSchemaToPoco
    {
        /// <summary>
        ///     Logger.
        /// </summary>
        private Logger _log;

        /// <summary>
        ///     Configuration.
        /// </summary>
        private readonly JsonSchemaToPocoConfiguration _configuration;

        /// <summary>
        ///     Keeps track of the found schemas.
        /// </summary>
        private Dictionary<Uri, JsonSchemaWrapper> _schemas = new Dictionary<Uri, JsonSchemaWrapper>();

        /// <summary>
        ///     Initialize settings.
        /// </summary>
        public JsonSchemaToPoco(JsonSchemaToPocoConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        ///     Main executor method.
        /// </summary>
        /// <returns>A status code.</returns>
        public int Execute()
        {
            try
            {
                ConfigureLogging();

                // Load schemas given a json file or directory
                LoadSchemas();

                // Generate code
                Generate();

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                _log.Fatal(e);
                return (int)ExitCodes.AbnormalExit;
            }
        }

        /// <summary>
        ///     Configuring the logger.
        /// </summary>
        private void ConfigureLogging()
        {
            var coloredConsoleTarget = new ColoredConsoleTarget
            {
                Layout = "${date:format=yyyy-MM-dd} ${time:format=hh:mm:ss} [${level}] ${message}"
            };
            var loggingRule = new LoggingRule("*", LogLevel.Debug, coloredConsoleTarget);
            LogManager.Configuration = new LoggingConfiguration();
            LogManager.Configuration.AddTarget("Console", coloredConsoleTarget);
            LogManager.Configuration.LoggingRules.Add(loggingRule);
            LogManager.ReconfigExistingLoggers();

            _log = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        ///     Load all the schemas from a file (or directory).
        /// </summary>
        private void LoadSchemas()
        {
            var resolver = new JsonSchemaResolver(_configuration.Namespace, !_configuration.Verbose, _configuration.OutputDirectory);
            FileAttributes attr = File.GetAttributes(_configuration.JsonSchemaFileLocation);

            // if specified path is a directory, load schemas from each file, otherwise just load schemas from that path
            bool isDirectory = (attr & FileAttributes.Directory) > 0;
            if (isDirectory)
            {
                _schemas = new Dictionary<Uri, JsonSchemaWrapper>();
                foreach (String fileName in Directory.GetFiles(_configuration.JsonSchemaFileLocation))
                {
                    Dictionary<Uri, JsonSchemaWrapper> resolvedSchemas = resolver.ResolveSchemas(fileName);
                    foreach (Uri key in resolvedSchemas.Keys)
                    {
                        // Schemas may refer to eachother, don't add the same schema twice just because it was included by reference
                        if (!_schemas.ContainsKey(key))
                        {
                            _schemas.Add(key,resolvedSchemas[key]);
                        }
                    }
                }
            }
            else
            {
                _schemas = resolver.ResolveSchemas(_configuration.JsonSchemaFileLocation);
            }
        }

        /// <summary>
        ///     Generate C# code.
        /// </summary>
        private void Generate()
        {
            var generatedCode = GenerateHelper();

            // Create directory to generate files
            if (!_configuration.Verbose)
            {
                IoUtils.CreateDirectoryFromNamespace(_configuration.OutputDirectory, _configuration.Namespace);
            }

            foreach (var entry in generatedCode)
            {
                if (!_configuration.Verbose)
                {
                    string saveLoc = _configuration.OutputDirectory + Path.DirectorySeparatorChar + entry.Key.Namespace.Replace('.', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar + entry.Key.Schema.Title +
                                     ".cs";
                    IoUtils.GenerateFile(entry.Value, saveLoc);
                    Console.WriteLine("Wrote " + saveLoc);
                }
                else
                {
                    Console.WriteLine(entry.Value);
                }
            }
        }

        /// <summary>
        ///     Return a Dictionary containing a map of the generated JsonSchemaWrappers with the generated code as a string.
        /// </summary>
        /// <returns>A mapping of all the JSON schemas and the generated code.</returns>
        private Dictionary<JsonSchemaWrapper, string> GenerateHelper()
        {
            var generatedCode = new Dictionary<JsonSchemaWrapper, string>();

            foreach (JsonSchemaWrapper s in _schemas.Values)
            {
                if (s.ToCreate)
                {
                    var jsonSchemaToCodeUnit = new JsonSchemaToCodeUnit(s, s.Namespace, _configuration.AttributeType);
                    CodeCompileUnit codeUnit = jsonSchemaToCodeUnit.Execute();
                    var csharpGenerator = new CodeCompileUnitToCSharp(codeUnit);

                    generatedCode.Add(s, csharpGenerator.Execute());
                }
            }

            return generatedCode;
        }

        /// <summary>
        ///     Static method to return a Dictionary of JsonSchemaWrapper and its corresponding C# generated code.
        /// </summary>
        /// <param name="schemaLoc">Location of JSON schema.</param>
        /// <returns>A mapping of all the JSON schemas and the generated code.</returns>
        public static Dictionary<JsonSchemaWrapper, string> GenerateFromFile(string schemaLoc)
        {
            var controller = new JsonSchemaToPoco(
                new JsonSchemaToPocoConfiguration
                {
                    JsonSchemaFileLocation = schemaLoc
                }
            );
            controller.LoadSchemas();
            return controller.GenerateHelper();
        }

        /// <summary>
        ///     Static method to return generated code for a single JSON schema with no references.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The generated code.</returns>
        public static string Generate(JsonSchemaToPocoConfiguration configuration)
        {
            return Generate(configuration.JsonSchemaFileLocation, configuration.Namespace, configuration.AttributeType);
        }

        /// <summary>
        ///     Static method to return generated code for a single JSON schema with no references.
        /// </summary>
        /// <param name="schema">Location of schema file.</param>
        /// <param name="ns">The namespace.</param>
        /// <param name="type">The attribute type.</param>
        /// <returns>The generated code.</returns>
        public static string Generate(string schema, string ns = "generated", AttributeType type = AttributeType.SystemDefault)
        {
            var jsonSchemaToCodeUnit = new JsonSchemaToCodeUnit(JsonSchemaResolver.ConvertToWrapper(schema), ns, type);
            CodeCompileUnit codeUnit = jsonSchemaToCodeUnit.Execute();
            var csharpGenerator = new CodeCompileUnitToCSharp(codeUnit);
            return csharpGenerator.Execute();
        }
    }
}
