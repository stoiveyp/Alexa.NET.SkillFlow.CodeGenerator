using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Management.InteractionModel;
using Alexa.NET.Management.Skills;
using Alexa.NET.SkillFlow.Generator;
using Newtonsoft.Json;

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
        }

        public string InvocationName { get; set; } = "Skill Flow";

        public Language Language { get; set; } = new Language();

        public Dictionary<string, object> OtherFiles { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, CodeCompileUnit> CodeFiles { get; } = new Dictionary<string, CodeCompileUnit>();

        public CodeGeneratorOptions Options { get; protected set; }
        public Stack<CodeObject> CodeScope { get; set; } = new Stack<CodeObject>();
        public string Marker => string.Concat(CodeScope.OfType<CodeMemberMethod>().Select(c => c.Name));

        public async Task Output(string directoryFullName)
        {
            var json = JsonSerializer.Create(new JsonSerializerSettings{Formatting = Newtonsoft.Json.Formatting.Indented});
            foreach (var supplemental in OtherFiles)
            {
                var writer = File.Open(Path.Combine(directoryFullName, supplemental.Key),FileMode.Create,FileAccess.Write);
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

            var oldName = Language.InvocationName;
            Language.InvocationName = InvocationName;
            using (var manifestStream = File.Open(Path.Combine(directoryFullName, "skillManifest.json"), FileMode.Create, FileAccess.Write))
            {
                using (var jsonWriter = new JsonTextWriter(new StreamWriter(manifestStream)))
                {
                    var interactionModel = new SkillInteraction
                    {
                        Language = Language
                    };
                    json.Serialize(jsonWriter, interactionModel);
                    await jsonWriter.FlushAsync();
                }
            }

            Language.InvocationName = oldName;

            using (var csharp = CodeDomProvider.CreateProvider(CodeDomProvider.GetLanguageFromExtension(".cs")))
            {
                await Task.WhenAll(CodeFiles.Select(async c =>
                {
                    using (var textWriter =
                        new StreamWriter(File.Open(Path.Combine(directoryFullName, c.Key) + ".cs", FileMode.Create, FileAccess.Write)))
                    {
                        csharp.GenerateCodeFromCompileUnit(
                            c.Value,
                            textWriter,
                            new System.CodeDom.Compiler.CodeGeneratorOptions());
                        await textWriter.FlushAsync();
                    }

                }));
            }
        }
    }
}
