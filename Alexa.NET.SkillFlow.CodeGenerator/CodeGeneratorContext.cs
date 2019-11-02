using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Alexa.NET.Management.Skills;
using Alexa.NET.SkillFlow.Generator;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneratorContext : SkillFlowContext
    {
        public CodeGeneratorContext() : this(CodeGeneratorOptions.Default)
        {

        }

        public CodeGeneratorContext(CodeGeneratorOptions options)
        {
            Options = options ?? CodeGeneratorOptions.Default;
            InteractionModel = new SkillInteraction();
        }

        public SkillInteraction InteractionModel { get; set; }

        public Dictionary<string, MemoryStream> OtherFiles { get; set; } = new Dictionary<string, MemoryStream>();

        public Dictionary<string, CodeCompileUnit> CodeFiles { get; } = new Dictionary<string, CodeCompileUnit>();

        public CodeGeneratorOptions Options { get; protected set; }
        public CodeTypeDeclaration CurrentClass { get; set; }

        public async Task Output(string directoryFullName)
        {
            foreach (var supplemental in OtherFiles)
            {
                using (var supplementalStream = File.OpenWrite(Path.Combine(directoryFullName, supplemental.Key)))
                {
                    supplemental.Value.Seek(0, SeekOrigin.Begin);
                    await supplemental.Value.CopyToAsync(supplementalStream);
                }
            }

            using (var csharp = CodeDomProvider.CreateProvider(CodeDomProvider.GetLanguageFromExtension(".cs")))
            {
                foreach (var codefile in CodeFiles)
                {
                    using (var textWriter =
                        new StreamWriter(File.OpenWrite(Path.Combine(directoryFullName, codefile.Key) + ".cs")))
                    {
                        csharp.GenerateCodeFromCompileUnit(
                            codefile.Value,
                            textWriter,
                            new System.CodeDom.Compiler.CodeGeneratorOptions());
                    }
                }
            }
        }
    }
}
