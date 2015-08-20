using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cvent.SchemaToPoco.Core.Types;
using Cvent.SchemaToPoco.Core.Util;
using Cvent.SchemaToPoco.Core.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NLog;

namespace Cvent.SchemaToPoco.Core
{
    /// <summary>
    ///     Resolve JSON schema $ref attributes
    /// </summary>
    public class JsonSchemaResolver
    {
        /// <summary>
        ///     Logger.
        /// </summary>
        private readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     The absolute path to the base generated directory.
        /// </summary>
        private readonly string _baseDir;

        /// <summary>
        ///     Whether or not to create directories.
        /// </summary>
        private readonly bool _createDirs;

        /// <summary>
        ///     The namespace.
        /// </summary>
        private readonly string _ns;

        /// <summary>
        ///     Resolving schemas so that they can be parsed.
        /// </summary>
        private readonly Newtonsoft.Json.Schema.JsonSchemaResolver _resolver = new Newtonsoft.Json.Schema.JsonSchemaResolver();

        /// <summary>
        ///     Keeps track of the found schemas.
        /// </summary>
        private readonly Dictionary<Uri, JsonSchemaWrapper> _schemas = new Dictionary<Uri, JsonSchemaWrapper>();

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="ns">settings.Namespace</param>
        /// <param name="createDirs">settings.Verbose</param>
        /// <param name="baseDir">The base directory of the generated files.</param>
        public JsonSchemaResolver(string ns, bool createDirs, string baseDir)
        {
            _ns = ns;
            _createDirs = createDirs;
            _baseDir = baseDir;
        }

        /// <summary>
        ///     Resolve all schemas.
        /// </summary>
        /// <param name="filePath">Path to the current file.</param>
        /// <returns>A Dictionary containing all resolved schemas.</returns>
        public Dictionary<Uri, JsonSchemaWrapper> ResolveSchemas(string filePath)
        {
            var uri = IoUtils.GetAbsoluteUri(new Uri(Directory.GetCurrentDirectory()), new Uri(filePath, UriKind.RelativeOrAbsolute), false);
            
            // Resolve the root schema
            JsonSchemaWrapper schema = ResolveSchemaHelper(uri, uri);
            if (!_schemas.ContainsKey(uri))
            {
                _schemas.Add(uri, schema);
            }
            return _schemas;
        }

        /// <summary>
        ///     Recursively resolve all schemas. All references to external schemas must have .json extension.
        ///     This is done by:
        ///         1. Scanning the schema for $ref attributes.
        ///         2. Attempting to construct a Uri object to represent the reference.
        ///         3. Passing it into a resolver to create a network of schemas.
        ///         4. Modifying the original schema's $ref attributes with the full, unique Uri.
        ///         5. Setting the id of the referenced schemas to its full, unique Uri.
        /// </summary>
        /// <param name="parent">Path to the parent file.</param>
        /// <param name="current">Path to the current file.</param>
        /// <returns>An extended wrapper for the JsonSchema.</returns>
        /// TODO check if parent is needed - right now it assumes parent for all children
        private JsonSchemaWrapper ResolveSchemaHelper(Uri parent, Uri current)
        {
            var uri = IoUtils.GetAbsoluteUri(parent, current, true);
            var data = IoUtils.ReadFromPath(uri);

            return ResolveSchemaHelper(uri, parent, data);
        }

        private JsonSchemaWrapper ResolveSchemaHelper(Uri curr, Uri parent, string data)
        {
            var definition = new
            {
                csharpType = string.Empty,
                csharpInterfaces = new string[] { },
                properties = new Dictionary<string, JObject>()
            };
            var deserialized = JsonConvert.DeserializeAnonymousType(data, definition);
            var dependencies = new List<JsonSchemaWrapper>();

            MatchCollection matches = Regex.Matches(data, @"\""\$ref\""\s*:\s*\""(.*.json)\""");
            foreach (Match match in matches)
            {
                // Get the full path to the file, and change the reference to match
                var currPath = new Uri(match.Groups[1].Value, UriKind.RelativeOrAbsolute);
                var currUri = IoUtils.GetAbsoluteUri(parent, currPath, true);

                JsonSchemaWrapper schema;

                if (!_schemas.ContainsKey(currUri))
                {
                    schema = ResolveSchemaHelper(parent, currUri);
                    _schemas.Add(currUri, schema);
                }
                else
                {
                    schema = _schemas[currUri];
                }

                // Add schema to dependencies
                dependencies.Add(schema);
            }

            // Go through properties to see if there needs to be more resolving
            if (deserialized != null && deserialized.properties != null)
            {
                foreach (var s in deserialized.properties)
                {
                    var properties = s.Value.Properties();

                    // Check that the property also has a top level key called properties or items
                    foreach (var prop in properties)
                    {
                        var isProp = prop.Name.Equals("properties");
                        var isItem = prop.Name.Equals("items");

                        // TODO ehhhh let's avoid hardcoding this
                        if (isProp || (isItem && prop.Value.ToString().Contains("\"properties\"")))
                        {
                            var propData = isProp ? s.Value.ToString() : prop.Value.ToString();

                            // Create dummy internal Uri
                            var dummyUri = new Uri(new Uri(curr + "/"), s.Key);

                            JsonSchemaWrapper schema = ResolveSchemaHelper(dummyUri, curr, propData);

                            if (!_schemas.ContainsKey(dummyUri))
                            {
                                _schemas.Add(dummyUri, schema);
                            }
                        }
                    }
                }
            } 

            // Set up schema and wrapper to return
            JsonSchema parsed;

            try
            {
                parsed = JsonSchema.Parse(StandardizeReferences(parent, data), _resolver);
            }
            catch (Exception)
            {
                _log.Error("Could not parse the schema: " + curr + "\nMake sure your schema is compatible." +
                           "Examine the stack trace below.");
                throw;
            }

            parsed.Id = curr.ToString();
            parsed.Title = parsed.Title.SanitizeIdentifier();
            var toReturn = new JsonSchemaWrapper(parsed) { Namespace = _ns, Dependencies = dependencies };

            // If csharpType is specified
            if (deserialized != null && !string.IsNullOrEmpty(deserialized.csharpType))
            {
                // Create directories and set namespace
                int lastIndex = deserialized.csharpType.LastIndexOf('.');
                string cType = deserialized.csharpType.Substring(lastIndex == -1 ? 0 : lastIndex + 1);

                toReturn.Namespace = deserialized.csharpType.Substring(0, lastIndex);
                toReturn.Schema.Title = cType;

                if (_createDirs)
                {
                    IoUtils.CreateDirectoryFromNamespace(_baseDir, toReturn.Namespace);
                }
            }

            // If csharpInterfaces is specified
            if (deserialized != null && deserialized.csharpInterfaces != null)
            {
                foreach (string s in deserialized.csharpInterfaces)
                {
                    // Try to resolve the type
                    Type t = Type.GetType(s, false);

                    // If type cannot be found, create a new type
                    if (t == null)
                    {
                        var builder = new TypeBuilderHelper(toReturn.Namespace);
                        t = builder.GetCustomType(s, !s.Contains("."));
                    }

                    toReturn.Interfaces.Add(t);
                }
            }

            return toReturn;
        }

        /// <summary>
        ///     Convert all $ref attributes to absolute paths.
        /// </summary>
        /// <param name="parentUri">The parent Uri to resolve relative paths against.</param>
        /// <param name="data">The JSON schema.</param>
        /// <returns>The JSON schema with standardized $ref attributes.</returns>
        private string StandardizeReferences(Uri parentUri, string data)
        {
            var lines = new List<string>(data.Split('\n'));
            var pattern = new Regex(@"(\""\$ref\""\s*:\s*\"")(.*.json)(\"")");

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    var matched = pattern.Match(lines[i]);
                    var matchedPath = matched.Groups[2].Value;
                    var absPath = IoUtils.GetAbsoluteUri(parentUri, new Uri(matchedPath, UriKind.RelativeOrAbsolute), true);
                    lines[i] = matched.Groups[1].Value + absPath + matched.Groups[3].Value + ",";
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        ///     Convert a schema with no references to a JsonSchemaWrapper.
        /// </summary>
        /// <param name="data">The JSON schema.</param>
        public static JsonSchemaWrapper ConvertToWrapper(string data)
        {
            return new JsonSchemaWrapper(JsonSchema.Parse(data));
        }
    }
}
