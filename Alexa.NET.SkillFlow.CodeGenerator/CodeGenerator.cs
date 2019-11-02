using System.Threading.Tasks;
using Alexa.NET.SkillFlow.Generator;

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
            context.CurrentClass = sceneClass;
            return base.Begin(scene, context);
        }

        protected override Task Begin(Text text, CodeGeneratorContext context)
        {
            var generate = context.CurrentClass.GetGenerateMethod();
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

        protected override Task Render(VisualProperty property, CodeGeneratorContext context)
        {
            switch (property.Key)
            {
                case "template":
                    CodeGeneration_Visuals.GenerateAplCall(context,property.Value);
                    break;
            }
            
            return Noop(context);
        }
    }
}
