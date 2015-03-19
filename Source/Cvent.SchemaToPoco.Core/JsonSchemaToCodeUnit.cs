using System;
using System.CodeDom;
using System.Text.RegularExpressions;
using Cvent.SchemaToPoco.Core.Types;
using Cvent.SchemaToPoco.Core.Util;
using Cvent.SchemaToPoco.Core.Wrappers;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cvent.SchemaToPoco.Core
{
    /// <summary>
    ///     Model for converting a JsonSchema to a CodeCompileUnit
    /// </summary>
    public class JsonSchemaToCodeUnit
    {
        /// <summary>
        ///     The namespace for the document.
        /// </summary>
        private readonly string _codeNamespace;

        /// <summary>
        ///     The JsonSchema, for easy access.
        /// </summary>
        private readonly JsonSchema _schemaDocument;

        /// <summary>
        ///     The extended JsonSchema wrapper.
        /// </summary>
        private readonly JsonSchemaWrapper _schemaWrapper;

        /// <summary>
        ///     The annotation type.
        /// </summary>
        private readonly AttributeType _attributeType;

        public JsonSchemaToCodeUnit(JsonSchemaWrapper schema, string requestedNamespace, AttributeType attributeType)
        {
            if (schema == null || schema.Schema == null)
            {
                throw new ArgumentNullException("schema");
            }

            _schemaWrapper = schema;
            _schemaDocument = schema.Schema;
            _codeNamespace = requestedNamespace;
            _attributeType = attributeType;
        }

        public JsonSchemaToCodeUnit(JsonSchemaWrapper schema)
            : this(schema, "", AttributeType.SystemDefault)
        {
        }

        /// <summary>
        ///     Main executor function.
        /// </summary>
        /// <returns>A CodeCompileUnit.</returns>
        public CodeCompileUnit Execute()
        {
            var codeCompileUnit = new CodeCompileUnit();

            // Set namespace
            var nsWrap = new NamespaceWrapper(new CodeNamespace(_codeNamespace));

            // Set class
            var codeClass = new CodeTypeDeclaration(_schemaDocument.Title) {Attributes = MemberAttributes.Public};
            var clWrap = new ClassWrapper(codeClass);

            // Add imports for interfaces and dependencies
            nsWrap.AddImportsFromWrapper(_schemaWrapper);

            // Add comments and attributes for class
            if (!String.IsNullOrEmpty(_schemaDocument.Description))
            {
                clWrap.AddComment(_schemaDocument.Description);
            }

            // Add extended class
            if (_schemaDocument.Extends != null && _schemaDocument.Extends.Count > 0)
            {
                clWrap.AddInterface(JsonSchemaUtils.GetType(_schemaDocument.Extends[0], _codeNamespace).Name);
            }

            // Add interfaces
            foreach (Type t in _schemaWrapper.Interfaces)
            {
                clWrap.AddInterface(t.Name);
            }

            // Add properties with getters/setters
            if (_schemaDocument.Properties != null)
            {
                foreach (var i in _schemaDocument.Properties)
                {
                    JsonSchema schema = i.Value;

                    // Sanitize inputs
                    if (!String.IsNullOrEmpty(schema.Description))
                    {
                        schema.Description = Regex.Unescape(schema.Description);
                    }

                    Type type;
                    // If it is an enum
                    if (schema.Enum != null)
                    {
                        string name = i.Key.Capitalize();
                        var enumField = new CodeTypeDeclaration(name);
                        var enumWrap = new EnumWrapper(enumField);

                        // Add comment if not null
                        if (!String.IsNullOrEmpty(schema.Description))
                        {
                            enumField.Comments.Add(new CodeCommentStatement(schema.Description));
                        }

                        foreach (JToken j in schema.Enum)
                        {
                            enumWrap.AddMember(j.ToString().SanitizeIdentifier());
                        }

                        // Add to namespace
                        nsWrap.AddClass(enumWrap.Property);

                        // note type for property declaration
                        type = new TypeBuilderHelper(_codeNamespace).GetCustomType(name, true);
                    }
                    else
                    {
                        // WARNING: This assumes the namespace of the property is the same as the parent.
                        // This should not be a problem since imports are handled for all dependencies at the beginning.
                        type = JsonSchemaUtils.GetType(schema, _codeNamespace);
                    }

                    bool isCustomType = type.Namespace != null && type.Namespace.Equals(_codeNamespace);
                    string strType = String.Empty;

                    // Add imports
                    nsWrap.AddImport(type.Namespace);
                    nsWrap.AddImportsFromSchema(schema);

                    // Get the property type
                    if (isCustomType)
                    {
                        strType = JsonSchemaUtils.IsArray(schema) ? string.Format("{0}<{1}>", JsonSchemaUtils.GetArrayType(schema), type.Name) : type.Name;
                    }
                    else if (JsonSchemaUtils.IsArray(schema))
                    {
                        strType = string.Format("{0}<{1}>", JsonSchemaUtils.GetArrayType(schema),
                            new CSharpCodeProvider().GetTypeOutput(new CodeTypeReference(type)));
                    }

                    var field = new CodeMemberField
                    {
                        Attributes = MemberAttributes.Private,
                        Name = "_" + i.Key,
                        Type =
                            TypeUtils.IsPrimitive(type) && !JsonSchemaUtils.IsArray(schema)
                                ? new CodeTypeReference(type)
                                : new CodeTypeReference(strType)
                    };

                    // Add comment if not null
                    if (!String.IsNullOrEmpty(schema.Description))
                    {
                        field.Comments.Add(new CodeCommentStatement(schema.Description));
                    }

                    clWrap.Property.Members.Add(field);

                    // Add setters/getters
                    CodeMemberProperty property = CreateProperty("_" + i.Key,
                        i.Key.Capitalize(), field.Type.BaseType);
                    var prWrap = new PropertyWrapper(property);

                    // Add comments and attributes
                    prWrap.Populate(schema, _attributeType);

                    // Add default, if any
                    if (schema.Default != null)
                    {
                        clWrap.AddDefault(field.Name, field.Type, schema);
                    }

                    clWrap.Property.Members.Add(property);
                }
            }

            // Add class to namespace
            nsWrap.AddClass(clWrap.Property);
            codeCompileUnit.Namespaces.Add(nsWrap.Namespace);

            return codeCompileUnit;
        }

        /// <summary>
        ///     Creates a public property with getters and setters that wrap the
        ///     specified field.
        /// </summary>
        /// <param name="field">The field to get and set.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="type">The type of the property.</param>
        /// <returns>The property.</returns>
        public static CodeMemberProperty CreateProperty(string field, string name, string type)
        {
            var property = new CodeMemberProperty
            {
                Name = name,
                Type = new CodeTypeReference(type),
                Attributes = MemberAttributes.Public
            };

            property.SetStatements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(null, field),
                    new CodePropertySetValueReferenceExpression()));

            property.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(null, field)));

            return property;
        }
    }
}
