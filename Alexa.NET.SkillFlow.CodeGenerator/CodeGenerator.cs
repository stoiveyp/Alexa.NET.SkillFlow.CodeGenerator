using System;
using System.CodeDom;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.SkillFlow.Generator;
using Alexa.NET.SkillFlow.Instructions;
using Alexa.NET.SkillFlow.Terminators;
using Reprompt = Alexa.NET.SkillFlow.Terminators.Reprompt;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGenerator : SkillFlowGenerator<CodeGeneratorContext>
    {
        protected override Task Begin(Story story, CodeGeneratorContext context)
        {
            CodeGeneration_Story.CreateStoryFiles(context);
            return base.Begin(story, context);
        }
        protected override Task Begin(Scene scene, CodeGeneratorContext context)
        {
            var code = CodeGeneration_Scene.Generate(scene, context);
            var sceneClass = code.FirstType();
            context.SceneFiles.Add(CodeGeneration_Scene.SceneClassName(scene.Name), code);
            context.CodeScope.Push(sceneClass);
            context.CodeScope.Push(sceneClass.GetMainMethod());

            CodeGeneration_Navigation.RegisterScene(context, scene.Name,
                new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(sceneClass.Name), CodeConstants.SceneInteractionMethod));

            if (scene.Name.Equals(SpecialScenes.Start, StringComparison.OrdinalIgnoreCase))
            {
                context.CreateLaunchRequestHandler();
            }

            return base.Begin(scene, context);
        }

        protected override Task End(Scene scene, CodeGeneratorContext context)
        {
            context.CodeScope.Clear();

            return base.End(scene, context);
        }

        protected override Task Begin(Text text, CodeGeneratorContext context)
        {
            var generate = (CodeMemberMethod)context.CodeScope.Peek();

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
            var gen = (CodeMemberMethod)context.CodeScope.Peek();
            return base.Begin(story, context);
        }

        protected override Task Render(VisualProperty property, CodeGeneratorContext context)
        {
            var gen = (CodeMemberMethod)context.CodeScope.Peek();
            switch (property.Key)
            {
                case "template":
                    CodeGeneration_Visuals.EnsureLayout(context,property.Value);
                    var method = new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression("Output"),
                        "SetTemplate",
                        new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                        new CodePrimitiveExpression(property.Value));
                    gen.Statements.Add(method);
                    break;
                case "background":
                case "title":
                case "subtitle":
                    var propertyMethod = new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression("Output"),
                        "SetDataProperty",
                        new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                        new CodePrimitiveExpression(property.Key.ToLower()),
                        new CodePrimitiveExpression(property.Value));
                    gen.Statements.Add(propertyMethod);
                    break;
            }
            return Noop(context);
        }

        protected override Task Begin(SceneInstructions instructions, CodeGeneratorContext context)
        {
            var gen = (CodeMemberMethod)context.CodeScope.Peek();
            return base.Begin(instructions, context);
        }

        protected override Task Begin(SceneInstructionContainer instructions, CodeGeneratorContext context)
        {
            CodeStatementCollection statements = context.CodeScope.Statements();

            switch (instructions)
            {
                case If ifStmt:
                {
                    var codeIf = new CodeConditionStatement(CodeGeneration_Condition.Generate(ifStmt.Condition));
                    statements.Add(codeIf);
                    context.CodeScope.Push(codeIf);
                    break;
                }
                case Hear hear when !context.CodeScope.OfType<Hear>().Any():
                    CodeGeneration_Interaction.AddHearMarker(context, statements);
                    CodeGeneration_Interaction.AddIntent(context, hear.Phrases, statements);
                    break;
            }


            return base.Begin(instructions, context);
        }

        protected override Task End(SceneInstructionContainer instructions, CodeGeneratorContext context)
        {
            if (context.CodeScope.Peek() is CodeConditionStatement || context.CodeScope.Peek() is CodeMemberMethod)
            {
                context.CodeScope.Pop();
            }

            if (context.CodeScope.Peek() is CodeTypeDeclaration typeDeclaration)
            {
                context.CodeScope.Push(typeDeclaration.GetMainMethod());
            }

            return base.End(instructions, context);
        }

        protected override Task Render(SceneInstruction instruction, CodeGeneratorContext context)
        {
            CodeStatementCollection statements;
            switch (context.CodeScope.Peek())
            {
                case CodeMemberMethod member:
                    statements = member.Statements;
                    break;
                case CodeTypeDeclaration codeType:
                    statements = codeType.GetMainMethod().Statements;
                    break;
                case CodeConditionStatement stmt:
                    statements = stmt.TrueStatements;
                    break;
                default:
                    return Noop(context);
            }

            CodeGeneration_Instructions.EnsureStateMaintenance(context);
            switch (instruction)
            {
                case Clear clear:
                    statements.Clear(clear.Variable);
                    break;
                case ClearAll clearAll:
                    statements.ClearAll();
                    break;
                case Decrease decrease:
                    statements.Decrease(decrease.Variable, decrease.Amount);
                    break;
                case Flag flag:
                    statements.SetVariable(flag.Variable, true);
                    break;
                case GoTo gto:
                    statements.Add(CodeGeneration_Navigation.GoToScene(gto.SceneName));
                    statements.Add(new CodeMethodReturnStatement());
                    break;
                case GoToAndReturn goToAndReturn:
                    statements.Add(CodeGeneration_Navigation.GoToScene(goToAndReturn.SceneName));
                    break;
                case Increase increase:
                    statements.Increase(increase.Variable, increase.Amount);
                    break;
                case Set set:
                    statements.SetVariable(set.Variable, set.Value);
                    break;
                case SlotAssignment slotAssignment:
                    context.SetSlotType(slotAssignment.SlotName, slotAssignment.SlotType);
                    break;
                case Unflag unflag:
                    statements.SetVariable(unflag.Variable, false);
                    break;
                case End end:
                    statements.Reset();
                    statements.Add(new CodeMethodReturnStatement());
                    break;
                case Repeat repeat:
                    statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "lastSpeech",
                        CodeGeneration_Instructions.GetVariable("scene_lastSpeech", typeof(string), false)));
                    statements.Add(new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression("Output"),
                        "AddSpeech",CodeConstants.RequestVariableRef,
                    new CodeVariableReferenceExpression("lastSpeech"),new CodePrimitiveExpression(true)));
                    statements.Add(new CodeMethodReturnStatement());
                    break;
                case Restart restart:
                    statements.ClearAll("scene_");
                    statements.ClearAll("_scene");
                    CodeGeneration_Navigation.GoToScene("start");
                    statements.Add(new CodeMethodReturnStatement());
                    break;
                case Resume resume:
                    statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("await Navigation"),
                        "Resume", CodeConstants.RequestVariableRef,new CodePrimitiveExpression(true)));
                    break;
                case Return returnCmd:
                    statements.Add(new CodeMethodReturnStatement());
                    break;
                case Reprompt reprompt:
                    statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "reprompt",
                        CodeGeneration_Instructions.GetVariable("scene_reprompt", typeof(string), false)));
                    statements.Add(new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression("Output"),
                        "AddSpeech", CodeConstants.RequestVariableRef,
                        new CodeVariableReferenceExpression("reprompt"), new CodePrimitiveExpression(true)));
                    statements.Add(new CodeMethodReturnStatement());
                    break;
                case Back back:
                    //implement scene stack? Dictionary access to generates?
                    break;
                case Pause pause:
                    break;
            }
            return base.Render(instruction, context);
        }
    }
}
