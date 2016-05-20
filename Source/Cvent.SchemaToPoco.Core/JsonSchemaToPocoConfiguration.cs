using System.IO;
using Cvent.SchemaToPoco.Core.Types;

namespace Cvent.SchemaToPoco.Core
{
    /// <summary>
    ///     Configuration class for JsonSchemaToPoco.
    /// </summary>
    public class JsonSchemaToPocoConfiguration
    {
        /// <summary>
        ///     Assign default values.
        /// </summary>
        public JsonSchemaToPocoConfiguration()
        {
            Namespace = "generated";
            OutputDirectory = Directory.GetCurrentDirectory();
            Verbose = false;
            AttributeType = AttributeType.SystemDefault;
        }

        /// <summary>
        ///     Location of root JSON schema file.
        /// </summary>
        public string JsonSchemaFileLocation { get; set; }

        /// <summary>
        ///     Namespace to set for generated files.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        ///     Directory to output generated files.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        ///     Whether or not to just print out files in console without generating.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        ///     Type of validation attribute to use.
        /// </summary>
        public AttributeType AttributeType { get; set; }

        /// <summary>
        ///     Language we are exporting to (csharp, php)
        /// </summary>
        public LanguageExportType LanguageExportType {get; set;}
    }
}
