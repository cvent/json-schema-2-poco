﻿using System;
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
            var ids = GetIds(data);
            data = StandardizeReferences(parent, data, ids);
            var definition = new
            {
                csharpType = string.Empty,
                csharpInterfaces = new string[] { },
                properties = new Dictionary<string, JObject>()
            };
            var deserialized = JsonConvert.DeserializeAnonymousType(data, definition);
            var dependencies = new List<JsonSchemaWrapper>();

            MatchCollection matches = Regex.Matches(data, @"\""\$ref\""\s*:\s*\""(.*)\""");
            foreach (Match match in matches)
            {
                // Get the full path to the file, and change the reference to match
                string refName = match.Groups[1].Value;
                var currPath = new Uri(refName, UriKind.RelativeOrAbsolute);
                if (ids.Contains(refName)) 
                {
                    //internal reference by ID, no need to load a new schema or add a dependency
                    continue;
                }
                var currUri = IoUtils.GetAbsoluteUri(parent, currPath, true);

                JsonSchemaWrapper schema;

                if (!_schemas.ContainsKey(currUri))
                {
                    // if this is a self reference, no need to load anything or add a dependency
                    if (parent.Equals(currUri))
                    {
                        continue;
                    }

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
            JsonSchema parsed = null;

            RemoveDuplicateSchemas();

            // work around the situation where we don't have a schema in our list of schemas,
            // but the resolver itself has already loaded that schema because it's a sub-schema
            // of a schema we've already loaded. In this case, find the already-resolved
            // schema and add that to our list of schemas
            foreach (var schema in _resolver.LoadedSchemas)
            {
                if (schema.Id != null &&  schema.Id.Equals(curr.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    parsed = schema;
                }
            }
            try
            {
                if (parsed == null)
                {
                    parsed = JsonSchema.Parse(data, _resolver);
                }
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
        /// We avoid adding duplicate schema IDs to the list to be parsed, but if we first parse the schema itself
        /// and then parse another schema that refers to it, then somewhere in the NewtonSoft parsing code we wind
        /// up adding a second copy of the referenced schema to _resolver.LoadedSchemas
        /// Then when the resolver attempts to follow the reference, it throws an error because it has
        /// two copies of the schema. Since they are identical we can get around this by just removing the
        /// extra copy.
        /// </summary>
        private void RemoveDuplicateSchemas()
        {
            ICollection<JsonSchema> dups = new HashSet<JsonSchema>();
            ICollection<String> schemaIds = new HashSet<String>();

            foreach (JsonSchema schema in _resolver.LoadedSchemas)
            {
                if (schema.Id != null)
                {
                    if (schemaIds.Contains(schema.Id))
                    {
                        dups.Add(schema);
                    }
                    else
                    {
                        schemaIds.Add(schema.Id);
                    }
                }
            }
            foreach (JsonSchema dup in dups)
            {
                _resolver.LoadedSchemas.Remove(dup);
            }
        }

        /// <summary>
        ///     Convert all $ref attributes to absolute paths.
        /// </summary>
        /// <param name="parentUri">The parent Uri to resolve relative paths against.</param>
        /// <param name="data">The JSON schema.</param>
        /// <param name="ids">the list of IDs defined in this schema</param>
        /// <returns>The JSON schema with standardized $ref attributes.</returns>
        private string StandardizeReferences(Uri parentUri, string data, ICollection<string> ids)
        {
            var lines = new List<string>(data.Split('\n'));
            var pattern = new Regex(@"(""\$ref""\s*:\s*"")(.*)("")");

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    var matched = pattern.Match(lines[i]);
                    var matchedPath = matched.Groups[2].Value;
                    if (matchedPath.StartsWith("#"))
                    {
                        //JSON pointer syntax isn't handled properly, but by creating our .json files with a convention
                        //that the last part of the pointer path matches the ID of the schema being declared in the same file,
                        //the reference can be fixed up by changing it to that ID (while still allowing the JSON pointer to work for Java)
                        string[] parts = matchedPath.Split('/');
                        string modifiedPath = parts[parts.Length - 1];
                        if (ids.Contains(modifiedPath))
                        {
                            lines[i] = matched.Groups[1].Value + modifiedPath + matched.Groups[3].Value + ",";
                            continue;
                        }
                        else
                        {
                            throw new InvalidOperationException(String.Format(
                                "'{0}' appears to be a JSON pointer, which is not directly supported. Attempted to use the last part as a local ID reference but '{1}' was not found in this document.",
                                matchedPath, modifiedPath));
                        }
                    }
                    // only modify the reference if it's not referencing an ID in this file
                    if (!ids.Contains(matchedPath))
                    {
                        if (!matchedPath.EndsWith(".json"))
                        {
                            matchedPath += ".json";
                        }
                        var absPath = IoUtils.GetAbsoluteUri(parentUri, new Uri(matchedPath, UriKind.RelativeOrAbsolute), true);
                        lines[i] = matched.Groups[1].Value + absPath + matched.Groups[3].Value + ",";
                    }
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Build list of IDs defined in this file. Can be used to check if a reference
        /// is referencing an ID (rather than an external file)
        /// </summary>
        private ICollection<string> GetIds(string data)
        {
            var ids = new List<string>();
            var lines = new List<string>(data.Split('\n'));
            var idPattern = new Regex(@"(""id""\s*:\s*"")(.*)(\"")");
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (idPattern.IsMatch(lines[i]))
                {
                    ids.Add(idPattern.Match(lines[i]).Groups[2].Value);
                }
            }

            // special ID for a self-reference
            // NOTE: this syntax currently fails when we get to the parse step in Newtonsoft, but we should note it
            //       as a valid ID here anyway as attempting to re-write such an entry won't help
            ids.Add("#");

            return ids;
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
