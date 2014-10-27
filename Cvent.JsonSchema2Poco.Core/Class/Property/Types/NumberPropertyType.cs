using System.CodeDom;
using Newtonsoft.Json.Linq;

namespace Cvent.JsonSchema2Poco.Class.Property.Types
{
    /// <summary>
    /// Represents the Number schema property. To handle all domains the type is represented as a double precision float.
    /// </summary>
    class NumberPropertyType : JsonSchemaPropertyType
    {
        public static bool UseDoubles { get; set; }

        /// <see cref="JsonSchemaPropertyType"/>
        public override CodeTypeReference GetType(JsonPropertyInfo info)
        {
            var required = info.Definition.Required.HasValue && info.Definition.Required.Value;
            var defaultPresent = GetDefaultAssignment(info) != null;
            if (UseDoubles)
            {
                return required || defaultPresent
                    ? new CodeTypeReference(typeof (double))
                    : new CodeTypeReference(typeof (double?));
            }
            return required || defaultPresent
                    ? new CodeTypeReference(typeof(float))
                    : new CodeTypeReference(typeof(float?));
        }

        /// <see cref="JsonSchemaPropertyType"/>
        public override CodeAssignStatement GetDefaultAssignment(JsonPropertyInfo info)
        {
            if (info.Definition.Default == null || info.Definition.Default.Type != JTokenType.Float)
                return null;

            return UseDoubles 
                ? CreatePrimitiveDefaultAssignment(info.FieldName, info.Definition.Default.Value<double>())
                : CreatePrimitiveDefaultAssignment(info.FieldName, info.Definition.Default.Value<float>());
        }
    }
}
