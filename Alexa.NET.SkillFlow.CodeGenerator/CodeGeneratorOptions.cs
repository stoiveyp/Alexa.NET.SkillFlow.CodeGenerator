using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneratorOptions
    {
        public string RootNamespace { get; set; }
        public string SkillName { get; set; }

        public string SafeRootNamespace => string.IsNullOrWhiteSpace(RootNamespace) ? "CodeGenerated" : RootNamespace;

        public static readonly CodeGeneratorOptions Default = new CodeGeneratorOptions();
    }
}