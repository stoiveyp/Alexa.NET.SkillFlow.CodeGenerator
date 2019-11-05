using System.CodeDom;
using System.Threading.Tasks;
using Alexa.NET.Response;
using Alexa.NET.Response.APL;
using Alexa.NET.SkillFlow.Generator;
using Alexa.NET.SkillFlow.Instructions;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGenerator : SkillFlowGenerator<CodeGeneratorContext>
    {
        protected override Task Begin(Story story, CodeGeneratorContext context)
        {
            CodeGeneration_Story.CreateProjectFile(context);
            return base.Begin(story, context);
        }

        protected override Task Begin(Scene scene, CodeGeneratorContext context)
        {
            var code = CodeGeneration_Scene.Generate(scene, context);
            var sceneClass = code.Namespaces[0].Types[0];
            context.CodeFiles.Add(CodeGeneration_Scene.SceneClassName(scene.Name), code);
            context.CodeScope.Push(sceneClass);
            return base.Begin(scene, context);
        }

        protected override Task Begin(Text text, CodeGeneratorContext context)
        {
            var generate = ((CodeTypeDeclaration)context.CodeScope.Peek()).GetGenerateMethod();
            generate.CleanIfEmpty();

            switch (text.TextType.ToLower())
            {
                case "say":
                    CodeGeneration_Text.GenerateSay(generate, text, context);
                    break;
                case "reprompt":
                    CodeGeneration_Text.GenerateReprompt(generate, text, context);
                    break;
                case "recap":
                    CodeGeneration_Text.GenerateRecap(generate, text, context);
                    break;
            }
            return base.Begin(text, context);
        }

        protected override Task Begin(Visual story, CodeGeneratorContext context)
        {
            var gen = ((CodeTypeDeclaration) context.CodeScope.Peek()).GetGenerateMethod();
            gen.CleanIfEmpty();

            var aplRef = CodeGeneration_Visuals.AddRenderDocument(gen, "apl");

            context.CodeScope.Push(aplRef);
            return base.Begin(story, context);
        }

        protected override Task End(Visual story, CodeGeneratorContext context)
        {
            context.CodeScope.Pop();
            return base.End(story, context);
        }

        protected override Task Render(VisualProperty property, CodeGeneratorContext context)
        {
            var render = context.CodeScope.Pop() as CodeVariableReferenceExpression;
            var gen = ((CodeTypeDeclaration)context.CodeScope.Peek()).GetGenerateMethod();
            switch (property.Key)
            {
                case "template":
                    var layoutCall = CodeGeneration_Visuals.GenerateAplCall(context,property.Value);
                    gen.Statements.Add(new CodeAssignStatement(
                        new CodePropertyReferenceExpression(render, "Document.MainTemplate"),
                        layoutCall));
                    break;
                case "background":
                    var bgDs = CodeGeneration_Visuals.EnsureDataSource(gen,"apl");
                    gen.Statements.Add(CodeGeneration_Visuals.AddDataSourceProperty(bgDs, "background", property.Value));
                    break;
                case "title":
                    var titleDs = CodeGeneration_Visuals.EnsureDataSource(gen, "apl");
                    gen.Statements.Add(CodeGeneration_Visuals.AddDataSourceProperty(titleDs, "title", property.Value));
                    break;
                case "subtitle":
                    var subtitleDs = CodeGeneration_Visuals.EnsureDataSource(gen, "apl");
                    gen.Statements.Add(CodeGeneration_Visuals.AddDataSourceProperty(subtitleDs, "subtitle", property.Value));
                    break;

            }
            context.CodeScope.Push(render);
            return Noop(context);
        }

        protected override Task Render(SceneInstruction instruction, CodeGeneratorContext context)
        {
            var gen = ((CodeTypeDeclaration)context.CodeScope.Peek()).GetGenerateMethod();
            gen.CleanIfEmpty();

            switch (instruction)
            {
                case GoTo gto:
                    gen.Statements.Add(new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(CodeGeneration_Scene.SceneClassName(gto.SceneName)),
                        "Generate",
                        new CodeVariableReferenceExpression("request"),
                        new CodeVariableReferenceExpression("responseBody")));
                    break;
            }
            return base.Render(instruction, context);
        }
    }
}
