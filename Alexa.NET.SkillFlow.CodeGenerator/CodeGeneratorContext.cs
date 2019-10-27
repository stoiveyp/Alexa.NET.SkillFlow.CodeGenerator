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

        public Dictionary<string, CodeCompileUnit> CodeFiles { get; } = new Dictionary<string, CodeCompileUnit>();

        public CodeGeneratorOptions Options { get; protected set; }
        public CodeCompileUnit CurrentClass { get; set; }

        public async Task Output(string directoryFullName)
        {
            CreateProjectFile(directoryFullName);
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

        private XDocument CreateProjectOutline()
        {
            var doc = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", new XText("netcoreapp2.1"))),
                    new XElement("ItemGroup",
                        new XElement("PackageReference",
                            new XAttribute("Include", "Alexa.NET"),
                            new XAttribute("Version", "1.8.2")),
                        new XElement("PackageReference",
                            new XAttribute("Include", "Alexa.NET.APL"),
                            new XAttribute("Version", "4.4.0")),
                        new XElement("PackageReference",
                            new XAttribute("Include", "Alexa.NET.RequestHandlers"),
                            new XAttribute("Version", "4.1.1"))
                    )
                )
            );

            return doc;
        }

        private void CreateProjectFile(string directoryFullName)
        {
            var csProjectLocation = Path.Combine(directoryFullName, "SkillFlow.csproj");
            var output = CreateProjectOutline();

            using (var newProject = new StreamWriter(new FileStream(csProjectLocation, FileMode.Create)))
            {
                using (var xml = XmlWriter.Create(newProject, new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Indent = true
                }))
                {
                    output.Save(xml);
                }
                newProject.Flush();
                newProject.Close();
            }
        }
    }
}
