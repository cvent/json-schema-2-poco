using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cvent.JsonSchema2Poco.Schema
{
    /// <summary>
    /// A JSON schema parser which resolves references within the specified schema. Json.Net's default parser
    /// does not handle resolving referenced schemas. The parser expects references to follow the URI schema and
    /// relative resolution defined in the JSON schema specification.
    /// </summary>
    public static class JsonSchemaParser
    {
        /// <summary>
        /// Parses the JSON schema from the provided resource.
        /// </summary>
        /// <param name="schemaUri">The URI for the schema to be parsed.</param>
        /// <returns>The parsed JSON schema.</returns>
        public static JsonSchema Parse(Uri schemaUri)
        {
            var referenceSchemaResolver = new JsonSchemaResolver();
            return Parse(schemaUri, referenceSchemaResolver);
        }

        /// <summary>
        /// Parses the JSON schema from the provided reference while adding all reference schemas
        /// to the provided resolver. 
        /// </summary>
        /// <param name="schemaUri">The URI for the schema to be parsed.</param>
        /// <param name="referenceSchemaResolver">The lookup of referenced schemas.</param>
        /// <returns>The parsed JSON schema.</returns>
        private static JsonSchema Parse(Uri schemaUri, JsonSchemaResolver referenceSchemaResolver)
        {
            var schemaContent = GetJsonFromResource(schemaUri);
            // RESOLVE THE ID OF THE SCHEMA TO AN ABSOLUTE URI.
            var id = GetProperty(schemaContent, "id", JTokenType.String);
            if (id != null)
            {
                var resolvedUri = new Uri(schemaUri, new Uri(id.Value<string>(), UriKind.RelativeOrAbsolute));
                schemaContent["id"].Replace(JToken.Parse(string.Format(@"""{0}""", resolvedUri)));
            }
            else
            {
                // The Id was not present. Simply resolve as the URI of the parent schema.
                schemaContent.Add("id", JToken.Parse(string.Format(@"""{0}""", schemaUri)));
            }
            ResolveReferenceSchemas(schemaContent, schemaUri, referenceSchemaResolver);
            return JsonSchema.Parse(schemaContent.ToString(), referenceSchemaResolver);
        }

        /// <summary>
        /// A recursive method that walks through the json schema looking for references in the schema and resolving them. If a reference
        /// is found an attempt to retrieve it is made and it's references resolved as well. 
        /// 
        /// To help build the schema resolver used by Json.Net the schemas parsed are altered in two ways.
        ///     Injecting schema Ids
        ///         All Schemas whether referenced or embedded will have their IDs resolved following the Json Schema specification. The resolved
        ///         ID is then injected into the schema to facilitate referencing. 
        ///     Resolving Reference URIs To Full URIs
        ///         TAll references will be resolved their full URIs to match the injected IDs.
        /// </summary>
        /// <param name="schemaContent">The json representation of the schema.</param>
        /// <param name="baseSchemaUri">The base URI to resolve all other child/referenced schemas against.</param>
        /// <param name="referenceSchemaResolver">The schema resolver used to track all child schemas.</param>
        private static void ResolveReferenceSchemas(JObject schemaContent, Uri baseSchemaUri, JsonSchemaResolver referenceSchemaResolver)
        {
            // RESOLVE REFERENCED SCHEMA.
            // The $ref property is checked for first because all other properties are to be ignored when present.
            var reference = schemaContent.SelectToken("$ref");
            if (reference != null && reference.Type == JTokenType.String)
            {
                // Convert $ref URI to absolute URI.
                var referenceUri = new Uri(baseSchemaUri, new Uri(reference.Value<string>(), UriKind.RelativeOrAbsolute));
                schemaContent["$ref"].Replace(JToken.Parse(string.Format(@"""{0}""", referenceUri)));

                bool referenceSchemaAlreadyParsed = referenceSchemaResolver.GetSchema(referenceUri.ToString()) != null;
                if (!referenceSchemaAlreadyParsed)
                {
                    Parse(referenceUri, referenceSchemaResolver);    
                }
                return;
            }

            // RESOLVE REFERENCE SCHEMAS WITHIN AN ARRAY.
            var arrayItem = GetProperty(schemaContent, "items", JTokenType.Object);
            if (arrayItem != null)
            {
                ResolveReferenceSchemas((JObject)arrayItem, baseSchemaUri, referenceSchemaResolver);
                return;
            }
            
            // RESOLVE REFERENCE SCHEMAS WITHIN A TUPLE.
            var tupleItems = GetProperty(schemaContent, "items", JTokenType.Array);
            if (tupleItems != null)
            {
                tupleItems.ToList()
                    .ForEach(item => ResolveReferenceSchemas((JObject) item, baseSchemaUri, referenceSchemaResolver));
                return;
            }

            // RESOLVE EXTENDED REFERENCE SCHEMAS.
            var extends = GetProperty(schemaContent, "extends", JTokenType.Object);
            if(extends != null)
            {
                ResolveReferenceSchemas((JObject)extends, baseSchemaUri, referenceSchemaResolver);
            }

            extends = GetProperty(schemaContent, "extends", JTokenType.Array);
            if(extends != null)
            {
                extends.ToList().ForEach(schema => ResolveReferenceSchemas(
                    (JObject)schema, baseSchemaUri, referenceSchemaResolver));
            }

            // RESOLVE REFERENCE SCHEMAS IN THE SCHEMA'S PROPERTIES.
            var type = GetProperty(schemaContent, "type", JTokenType.String);
            var properties = GetProperty(schemaContent, "properties", JTokenType.Object);
            var isSchema = type != null && type.Value<string>() == "object" && properties != null;
            if (!isSchema)
            {
                // The current json object is not a schema but a property of a schema. No futher processing
                // is needed.
                return;
            }
            
            // RESOLVE THE ID OF THE SCHEMA TO AN ABSOLUTE URI.
            var id = GetProperty(schemaContent, "id", JTokenType.String);
            if (id != null)
            {
                var resolvedUri = new Uri(baseSchemaUri, new Uri(id.Value<string>(), UriKind.RelativeOrAbsolute));
                schemaContent["id"].Replace(JToken.Parse(string.Format(@"""{0}""", resolvedUri)));
            }
            else
            {
                // The Id was not present. Simply resolve as the URI of the parent schema.
                schemaContent.Add("id", JToken.Parse(string.Format(@"""{0}""", baseSchemaUri)));
                id = schemaContent.SelectToken("id");
            }

            foreach (var token in properties)
            {
                if (token.Type != JTokenType.Property)
                    continue;

                var property = (JProperty)token;
                if (property.Value.Type != JTokenType.Object)
                    continue;

                // The URI for the property is updated to be a JSON pointer to help the resolver keep uniqueness. 
                var idUri = new Uri(id.Value<string>(), UriKind.Absolute);
                var possibleSchemaUri = AppendPropertyAsJsonPointer(idUri, property.Name);
                ResolveReferenceSchemas((JObject)property.Value, possibleSchemaUri, referenceSchemaResolver);
            }
        }

        /// <summary>
        /// Appends the property to the JSON Pointer in the fragment of the resource. If the
        /// fragment does not exist it it will be created. It is assumed if a fragment exists
        /// it follows the Json pointer scheme.
        /// </summary>
        /// <param name="uri">The resource to append the property to.</param>
        /// <param name="propertyName">the property to append to the json pointer.</param>
        /// <returns>The updated uri with the fragment addition.</returns>
        private static Uri AppendPropertyAsJsonPointer(Uri uri, string propertyName)
        {
            var fragmentComponent = string.IsNullOrEmpty(uri.Fragment)
                    ? string.Format("#/{0}", propertyName)
                    : string.Format("{0}{1}{2}", 
                        uri.Fragment, 
                        uri.Fragment.EndsWith("/") ? "" : "/",
                        propertyName);
            return new Uri(uri, new Uri(fragmentComponent, UriKind.Relative));
        }

        /// <summary>
        /// Gets the property with the specific name if it is of the correct type. 
        /// </summary>
        /// <param name="jsonObject">The json object to retrieve the property from.</param>
        /// <param name="propertyName">The name of the property to retrieve.</param>
        /// <param name="propertyType">The type of the property to retrieve.</param>
        /// <returns>The retrieved property. Null if the property was not found or the incorrect type.</returns>
        private static JToken GetProperty(JObject jsonObject, string propertyName, JTokenType propertyType)
        {
            var propertyFound = jsonObject[propertyName] != null && jsonObject[propertyName].Type == propertyType;
            return propertyFound ? jsonObject[propertyName] : null;
        }

        /// <summary>
        /// Retrieves the JSON from the provided resource. Note that not all resource types have
        /// implementations for retrieving a resource. Only file, http, and https have been implemented.
        /// </summary>
        /// <param name="jsonResource">The resource containing the JSON to be retrieved.</param>
        /// <returns>The parsed json object from the resource.</returns>
        private static JObject GetJsonFromResource(Uri jsonResource)
        {
            string schemaText;
            switch (jsonResource.Scheme.ToLower())
            {
                case "file":
                    schemaText = File.ReadAllText(jsonResource.AbsolutePath);
                    break;
                case "http":
                case "https":
                    using (var client = new WebClient())
                    {
                        schemaText = client.DownloadString(jsonResource.ToString());
                    }
                    break;
                case "ftp":
                case "gopher":
                case "idap":
                case "mailto":
                case "net.pipe":
                case "net.tcp":
                case "news":
                case "nntp":
                case "telnet":
                case "uuid":
                    throw new ArgumentException(
                        string.Format(
                            "Failed to retrieve schema. Error: The uri scheme {0} for the json schema {1} is currently not supported by the application.",
                            jsonResource.Scheme, jsonResource));
                default:
                    throw new ArgumentException(
                        string.Format(
                            "Failed to retrieve schema. Error: An unknown uri scheme {0} for the json schema {1} is currently not supported by the application.",
                            jsonResource.Scheme, jsonResource));
            }

            return JObject.Parse(schemaText);
        }
    }
}
