using System.IO;
using System.Threading.Tasks;
using Alexa.NET.Management.Skills;
using Alexa.NET.SkillFlow.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneratorContext:SkillFlowContext
    {
        public static async Task<CodeGeneratorContext> Create(string csProjectLocation, CodeGeneratorOptions options = null)
        {
            if (!File.Exists(csProjectLocation))
            {
                var assembly = typeof(CodeGeneratorOptions).Assembly;
                Stream resource = assembly.GetManifestResourceStream("CSProjectTemplate.xml");
                using (var newProject = File.OpenWrite(csProjectLocation))
                {
                    await resource.CopyToAsync(newProject);
                }
            }

            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(csProjectLocation);
            return new CodeGeneratorContext(project, options ?? CodeGeneratorOptions.Default);
        }

        public CodeGeneratorContext():this(CodeGeneratorOptions.Default,true)
        {

        }

        public CodeGeneratorContext(CodeGeneratorOptions options) : this(options, true)
        {

        }

        public CodeGeneratorContext(Project project):this(CodeGeneratorOptions.Default,false)
        {
            Project = project;
        }
        public CodeGeneratorContext(Project project, CodeGeneratorOptions options) : this(options,false)
        {
            Project = project;
        }


        protected CodeGeneratorContext(CodeGeneratorOptions options, bool generateProject)
        {
            Options = options;
            InteractionModel = new SkillInteraction();
            if (generateProject)
            {
                var workspace = new AdhocWorkspace();
                Project = workspace.AddProject(Options.SafeRootNamespace, LanguageNames.CSharp);
            }
        }

        public SkillInteraction InteractionModel { get; set; }

        public Project Project{ get; set; }

        public CodeGeneratorOptions Options { get; protected set; }
    }
}
