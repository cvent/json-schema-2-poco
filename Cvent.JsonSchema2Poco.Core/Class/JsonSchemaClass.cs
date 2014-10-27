using System;
using System.CodeDom;
using System.Collections.Generic;
using Newtonsoft.Json.Schema;

namespace Cvent.JsonSchema2Poco.Class
{
    public class JsonSchemaClass
    {
        #region Properties
        public string Id { get; set; }
        public CodeNamespaceImportCollection Imports { get; set; }
        public String Namespace { get; set; }
        public CodeComment Comment { get; set; }
        public String Name { get; set; }
        public List<CodeMemberField> Fields { get; set; }
        public List<CodeMemberProperty> Properties { get; set; }
        public List<CodeAssignStatement> PropertyDefaults { get; set; } 
        public List<JsonSchemaClass> DependentClasses { get; set; } 
        public CodeTypeDeclarationCollection EmbeddedTypes { get; set; }
        #endregion

        #region Public Methods
        public JsonSchemaClass()
        {
            Imports = new CodeNamespaceImportCollection();
            Fields = new List<CodeMemberField>();
            Properties = new List<CodeMemberProperty>();
            PropertyDefaults = new List<CodeAssignStatement>();
            DependentClasses = new List<JsonSchemaClass>();
            EmbeddedTypes = new CodeTypeDeclarationCollection();
        }
        
        public static JsonSchemaClass CreateFromSchema(JsonSchema schema, string schemaName, string schemaNamespace)
        {
            var classBuilder = new JsonSchemaClassBuilder();
            return classBuilder.Build(schema, schemaName, schemaNamespace);
        }

        public CodeCompileUnit GetClassRepresentation(bool usePartialClasses)
        {
            // DEFINE THE CONSTRUCTOR.
            var constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;
            PropertyDefaults.ForEach(x => constructor.Statements.Add(x));

            // DEFINE THE IN MEMORY CLASS REPRESENTATION OF THE SCHEMA.
            var jsonClassRepresentation = new CodeTypeDeclaration();
            jsonClassRepresentation.IsClass = true;
            jsonClassRepresentation.IsPartial = usePartialClasses;
            jsonClassRepresentation.Name = Name;
            jsonClassRepresentation.Comments.Add(new CodeCommentStatement(Comment));
            Fields.ForEach(x => jsonClassRepresentation.Members.Add(x));
            Properties.ForEach(x => jsonClassRepresentation.Members.Add(x));
            foreach (CodeTypeDeclaration embeddedType in EmbeddedTypes)
            {
                jsonClassRepresentation.Members.Add(embeddedType);
            }
            if (constructor.Statements.Count > 0)
            {
                jsonClassRepresentation.Members.Add(constructor);
            }

            // DEFINE THE NAMESPACE FOR THE IN MEMORY CLASS.
            var jsonNamespaceRepresentation = new CodeNamespace(Namespace);
            jsonNamespaceRepresentation.Types.Add(jsonClassRepresentation);
            foreach (CodeNamespaceImport import in Imports)
            {
                jsonNamespaceRepresentation.Imports.Add(import);
            }

            var jsonFullClassRepresentation = new CodeCompileUnit();
            jsonFullClassRepresentation.Namespaces.Add(jsonNamespaceRepresentation);
            return jsonFullClassRepresentation;
        }
        #endregion
    }
}
