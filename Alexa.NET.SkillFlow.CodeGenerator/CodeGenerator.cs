using System.CodeDom;
using System.Threading.Tasks;
using Alexa.NET.SkillFlow.Generator;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGenerator : SkillFlowGenerator<CodeGeneratorContext>
    {
        protected override Task Begin(Scene scene, CodeGeneratorContext context)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace("SkillFlow");
            code.Namespaces.Add(ns);

            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.APL"));

            var mainClass = new CodeTypeDeclaration(SceneClassName(scene.Name)) { IsClass = true };
            ns.Types.Add(mainClass);

            context.CodeFiles.Add(scene.Name, code);
            return base.Begin(scene, context);
        }

        private string SceneClassName(string sceneName)
        {
            return "Scene_" + char.ToUpper(sceneName[0]) + sceneName.Substring(1).Replace(" ", "_");
        }
    }
}
