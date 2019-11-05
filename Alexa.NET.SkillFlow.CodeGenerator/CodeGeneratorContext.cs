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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public Dictionary<string, object> OtherFiles { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, CodeCompileUnit> CodeFiles { get; } = new Dictionary<string, CodeCompileUnit>();

        public CodeGeneratorOptions Options { get; protected set; }
        public CodeTypeDeclaration CurrentClass { get; set; }

        public async Task Output(string directoryFullName)
        {
            var json = JsonSerializer.Create(new JsonSerializerSettings{Formatting = Newtonsoft.Json.Formatting.Indented});
            foreach (var supplemental in OtherFiles)
            {
                var writer = File.OpenWrite(Path.Combine(directoryFullName, supplemental.Key));
                if (supplemental.Value is Stream suppStream)
                {
                    using (writer)
                    {
                        suppStream.Seek(0, SeekOrigin.Begin);
                        await suppStream.CopyToAsync(writer);
                    }
                }
                else
                {
                    using (var jsonWriter = new JsonTextWriter(new StreamWriter(writer)))
                    {
                        json.Serialize(jsonWriter, supplemental.Value);
                    }
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
