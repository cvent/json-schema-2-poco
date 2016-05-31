using System.CodeDom;
using Microsoft.CSharp;

namespace Cvent.SchemaToPoco.Core.CodeToLanguage
{
    /// <summary>
    ///     Compile a CodeCompileUnit to C# code.
    /// </summary>
    public class CodeCompileUnitToCSharp : CodeCompileUnitToLanguageBase
    {
        public CodeCompileUnitToCSharp(CodeCompileUnit codeCompileUnit) : base(codeCompileUnit)
        {
        }

        /// <summary>
        ///     Main executor function.
        /// </summary>
        /// <returns>A string of generated C# code.</returns>
        public override string Execute()
        {
            using (var codeProvider = new CSharpCodeProvider())
            {
                return CodeUnitToLanguage(codeProvider);
            }
        }
    }
}
