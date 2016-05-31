using System.CodeDom;
using Microsoft.VisualBasic;

namespace Cvent.SchemaToPoco.Core.CodeToLanguage
{
    /// <summary>
    ///     Compile a CodeCompileUnit to VB.net code.
    /// </summary>
    public class CodeCompileUnitToVisualBasic : CodeCompileUnitToLanguageBase
    {
        public CodeCompileUnitToVisualBasic(CodeCompileUnit codeCompileUnit) : base(codeCompileUnit)
        {
        }

        /// <summary>
        ///     Main executor function.
        /// </summary>
        /// <returns>A string of generated VB.net code.</returns>
        public override string Execute()
        {
            using (var codeProvider = new VBCodeProvider())
            {
                return CodeUnitToLanguage(codeProvider);
            }
        }
    }
}
