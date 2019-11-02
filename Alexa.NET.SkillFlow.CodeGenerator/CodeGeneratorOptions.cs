using System.Text;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneratorOptions
    {
        public string RootNamespace { get; set; }
        public string SkillName { get; set; }

        public string SafeRootNamespace => string.IsNullOrWhiteSpace(RootNamespace) ? "SkillFlowGenerated" : Safe(RootNamespace);
        public string SafeSkillName => string.IsNullOrWhiteSpace(SkillName) ? "SkillFlow" : Safe(SkillName);

        private string Safe(string skillName)
        {
            var osb = new StringBuilder();
            foreach (char s in skillName)
            {
                if (s == ' ' || !char.IsLetterOrDigit(s))
                {
                    osb.Append('_');
                    continue;
                }

                osb.Append(s);
            }

            return osb.ToString();
        }

        public static readonly CodeGeneratorOptions Default = new CodeGeneratorOptions();
    }
}