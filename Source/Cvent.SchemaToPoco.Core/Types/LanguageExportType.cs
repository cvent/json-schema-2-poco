using System.ComponentModel;

namespace Cvent.SchemaToPoco.Core.Types
{
    /// <summary>
    ///     Determine type of language (php, csharp) to generate.
    /// </summary>
    public enum LanguageExportType
    {
        [Description(".cs")]
        CSharp = 1,
        [Description(".php")]
        Php = 2,
    }
}
