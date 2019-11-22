using System.Text;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneratorOptions
    {
        public string RootNamespace { get; set; }
        public string SkillName { get; set; }

        public string SafeRootNamespace => string.IsNullOrWhiteSpace(RootNamespace) ? "SkillFlowGenerated" : RootNamespace.Safe();
        public string SafeSkillName => string.IsNullOrWhiteSpace(SkillName) ? "SkillFlow" : SkillName.Safe();

        public static readonly CodeGeneratorOptions Default = new CodeGeneratorOptions();
    }
}