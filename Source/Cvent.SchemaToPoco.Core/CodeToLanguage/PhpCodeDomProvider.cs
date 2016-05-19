using System;
using System.CodeDom.Compiler;
using PHP.Core.CodeDom;

namespace Cvent.SchemaToPoco.Core.CodeToLanguage
{
    public class PhpCodeDomProvider : CodeDomProvider, ICodeGenerator
    {
        ICodeGenerator _generator;
        public PhpCodeDomProvider()
        {
            _generator = new PhpCodeGenerator();
        }
        [Obsolete("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeGenerator CreateGenerator()
        {
            return _generator;
        }

        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeCompiler CreateCompiler()
        {
            throw new System.NotImplementedException();
        }

        public void ValidateIdentifier(string value)
        {
            throw new NotImplementedException();
        }
    }
}