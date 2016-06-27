using Cvent.SchemaToPoco.Core.Util;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Cvent.SchemaToPoco.Core.CodeToLanguage
{
    public class PhpCodeGenerator : ICodeGenerator
    {
        private IndentedTextWriter _output;
        private CodeGeneratorOptions _options;
        private readonly Dictionary<string,string> _typeMapping = new Dictionary<string, string>();

        public PhpCodeGenerator()
        {
            _typeMapping.Add("system.boolean","boolean");
            _typeMapping.Add("system.double", "float");
            _typeMapping.Add("system.int16", "integer");
            _typeMapping.Add("system.int32", "integer");
            _typeMapping.Add("system.int64", "integer");
            _typeMapping.Add("system.string", "string");
            _typeMapping.Add("list<string>", "string[]");
            _typeMapping.Add("list<int>", "int[]");
            _typeMapping.Add("list<double>", "float[]");

        }
        public bool IsValidIdentifier(string value)
        {
            throw new NotImplementedException();
        }

        public void ValidateIdentifier(string value)
        {
            throw new NotImplementedException();
        }

        public string CreateEscapedIdentifier(string value)
        {
            throw new NotImplementedException();
        }

        public string CreateValidIdentifier(string value)
        {
            throw new NotImplementedException();
        }

        public string GetTypeOutput(CodeTypeReference type)
        {
            throw new NotImplementedException();
        }

        public bool Supports(GeneratorSupport supports)
        {
            throw new NotImplementedException();
        }

        public void GenerateCodeFromExpression(CodeExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            throw new NotImplementedException();
        }

        public void GenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o)
        {
            throw new NotImplementedException();
        }

        public void GenerateCodeFromNamespace(CodeNamespace e, TextWriter w, CodeGeneratorOptions o)
        {
            throw new NotImplementedException();
        }

        public void GenerateCodeFromCompileUnit(CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o)
        {
            _options = o ?? new CodeGeneratorOptions();
            if (_output == null)
            {
                _output = new IndentedTextWriter(w, "     ");
            }
            
            GeneratePhpFileStart();
            GeneratePhpNameSpaces(e);
            GeneratePhpFileEnd();
        }

        private void GeneratePhpNameSpaces(CodeCompileUnit codeCompileUnit)
        {
            foreach (CodeNamespace ns in codeCompileUnit.Namespaces)
            {
                _output.Write("namespace {0};", ns.Name.Replace(".","\\"));
                OutputStartingBrace();
                _output.Indent++;
                GeneratePhpNameTypes(ns);
                _output.Indent--;
                _output.WriteLine("}");
                
            }
        }

        private void GeneratePhpNameTypes(CodeNamespace ns)
        {
            foreach (CodeTypeDeclaration type in ns.Types)
            {
                GenerateClass(type);
                GenerateEnum(type);
                _output.WriteLineNoTabs(string.Empty);
            }
        }

        private void GenerateEnum(CodeTypeDeclaration type)
        {
            if (type.IsEnum)
            {
                _output.WriteLine("require_once (dirname(__FILE__) .  '/../../../../../includes/Enum.php');");
                _output.Write("class {0} extends Enum ", StringUtils.LowerFirst(type.Name));
                OutputStartingBrace();

                _output.Indent++;

                GeneratePhpMembers(type);

                _output.Indent--;
                _output.WriteLine("}");
            }
        }

        private void GenerateClass(CodeTypeDeclaration type)
        {
            if (type.IsClass)
            {
                _output.Write("class {0} ", StringUtils.LowerFirst(type.Name));
                OutputStartingBrace();

                _output.Indent++;

                GeneratePhpMembers(type);

                _output.Indent--;
                _output.WriteLine("}");
            }
        }

        private void GeneratePhpMembers(CodeTypeDeclaration type)
        {
            foreach (CodeTypeMember member in type.Members)
            {
                if(member.Name == ".ctor")
                    continue;
                if (type.IsEnum)
                {
                    _output.WriteLine("const {0} = '{1}';", member.Name, member.Name);
                }
                else
                {
                    if (member is CodeMemberField)
                    {

                        var cmf = member as CodeMemberField;
                        var declaringType = cmf.Type.BaseType;
                        var memberName = member.Name.Replace(" { get; set; } //", string.Empty);
                        var phpJsonNDocAttribute = GetPhpType(declaringType);
                        _output.WriteLine("/**");
                        _output.WriteLine(" * @var {0}", StringUtils.LowerFirst(phpJsonNDocAttribute));
                        _output.WriteLine(" */");
                        _output.WriteLine("public ${0};", StringUtils.LowerFirst(memberName));
                    }
                }
            }
        }

        private string GetPhpType(string declaringType)
        {
            string phpType = declaringType;
            if (_typeMapping.ContainsKey(declaringType.ToLower()))
            {
                phpType = _typeMapping[declaringType.ToLower()];
            }
            else if (phpType.StartsWith("List<"))
            {
                phpType = phpType.Replace("List<", "").Replace(">", "[]");
            }
            Debug.WriteLine("Declaring Type: " + declaringType + " new type: "  + phpType);
            return phpType;
        }

        private void OutputStartingBrace()
        {
            if (_options.BracingStyle == "C")
            {
                _output.WriteLine("");
                _output.WriteLine("{");
            }
            else
            {
                _output.WriteLine(" {");
            }
        }

        private void GeneratePhpFileEnd()
        {
            _output.WriteLineNoTabs(string.Empty);
            _output.WriteLineNoTabs("?>");
        }

        private void GeneratePhpFileStart()
        {
            
            _output.WriteLine("<?php");
            _output.Indent = 1;
            _output.WriteLine(@"//------------------------------------------------------------------------------");
            _output.WriteLine(@"// <auto-generated>");
            _output.WriteLine(@"//     This code was generated by a tool.");
            _output.WriteLine(@"//     Runtime Version: " + Environment.Version);
            _output.WriteLine(@"//");
            _output.WriteLine(@"//     Changes to this file may cause incorrect behavior and will be lost if");
            _output.WriteLine(@"//     the code is regenerated.");
            _output.WriteLine(@"// </auto-generated>");
            _output.WriteLine(@"//------------------------------------------------------------------------------");
            _output.WriteLineNoTabs(string.Empty);
            
        }

        public void GenerateCodeFromType(CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o)
        {
            throw new NotImplementedException();
        }


    }
}
