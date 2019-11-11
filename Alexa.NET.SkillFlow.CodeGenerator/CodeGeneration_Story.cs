using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Story
    {
        private static XDocument CreateProjectOutline()
        {
            var doc = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", new XText("netcoreapp2.1"))),
                    new XElement("ItemGroup",
                        new XElement("PackageReference",
                            new XAttribute("Include", "Alexa.NET"),
                            new XAttribute("Version", "1.10.1")),
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

        public static void CreateProjectFile(CodeGeneratorContext context)
        {
            var output = CreateProjectOutline();

            var storage = new MemoryStream();
            using (var newProject = new StreamWriter(storage,Encoding.UTF8,1024,true))
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
            context.OtherFiles.Add($"{context.Options.SafeSkillName}.csproj", storage);
        }
    }
}