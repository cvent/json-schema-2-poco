using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cvent.JsonSchema2Poco.Class.Property.Types;
using Cvent.JsonSchema2Poco.Schema;
using Cvent.JsonSchema2Poco.Class;
using Microsoft.CSharp;
using Newtonsoft.Json.Schema;

namespace Cvent.JsonSchema2Poco
{
    public class JsonSchema2Poco
    {
        public static void Generate(IGenerationConfig configuration)
        {
            if (Directory.Exists(configuration.TargetDirectory))
            {
                if (configuration.RemoveOldOutput)
                {
                    Directory.Delete(configuration.TargetDirectory, true);
                }
            }
            else
            {
                Directory.CreateDirectory(configuration.TargetDirectory);
            }

            // PARSE THE SCHEMAS.
            var schemas = new List<JsonSchema>();
            configuration.Sources.ToList().ForEach(schemaUri => schemas.Add(JsonSchemaParser.Parse(schemaUri)));

            // SET PROPERTY CONFIGS.
            SetPropertyConfigs(configuration);

            // CREATE THE CLASS REPRESENTATION OF THE SCHEMAS.
            var schemaClassRepresentations = new List<JsonSchemaClass>();
            schemas.ForEach(schema => schemaClassRepresentations.Add(JsonSchemaClass.CreateFromSchema(schema, null, configuration.Namespace)));
            var uniqueSchemas = GetUniqueSchemaClasses(schemaClassRepresentations.Where(schema => schema != null));

            // GENERATE THE CLASSES.
            foreach (var schemaClass in uniqueSchemas)
            {
                File.WriteAllText(Path.Combine(configuration.TargetDirectory, schemaClass.Name + ".cs"), GetCSharpClassText(schemaClass.GetClassRepresentation(configuration.UsePartialClasses)));
            }
        }

        private static void SetPropertyConfigs(IGenerationConfig configuration)
        {
            IntegerPropertyType.UseLongs = configuration.UseLongsForInts;
            NumberPropertyType.UseDoubles = configuration.UseDoublesForFloats;
        }

        private static string GetCSharpClassText(CodeCompileUnit classRepresentation)
        {
            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var writer = new IndentedTextWriter(stringWriter, "    ");
            var codeProvider = new CSharpCodeProvider();
            codeProvider.GenerateCodeFromCompileUnit(classRepresentation, writer, new CodeGeneratorOptions
            {
                BlankLinesBetweenMembers = true,
                VerbatimOrder = false,
                BracingStyle = "C"
            });

            return stringBuilder.ToString();
        }

        private static List<JsonSchemaClass> GetUniqueSchemaClasses(IEnumerable<JsonSchemaClass> schemas)
        {
            var uniqueClassesById = new Dictionary<string, JsonSchemaClass>();
            var uniqueClassesByName = new Dictionary<string, JsonSchemaClass>();
            foreach (var schema in schemas)
            {
                GetUniqueSchemaClasses(schema, uniqueClassesById, uniqueClassesByName);
            }
            return uniqueClassesById.Values.ToList();
        }

        private static void GetUniqueSchemaClasses(JsonSchemaClass jsonClass, Dictionary<string, JsonSchemaClass> uniqueClassesById, Dictionary<string, JsonSchemaClass> uniqueClassesByName)
        {
            foreach (var dependentClass in jsonClass.DependentClasses)
            {
                GetUniqueSchemaClasses(dependentClass, uniqueClassesById, uniqueClassesByName);
            }

            if (uniqueClassesById.ContainsKey(jsonClass.Id))
            {
                jsonClass.Name = uniqueClassesById[jsonClass.Id].Id;
                return;
            }

            while (uniqueClassesByName.ContainsKey(jsonClass.Name))
            {
                jsonClass.Name = jsonClass.Name + "_";
            }

            uniqueClassesById.Add(jsonClass.Id, jsonClass);
            uniqueClassesByName.Add(jsonClass.Name, jsonClass);
        }
    }
}
