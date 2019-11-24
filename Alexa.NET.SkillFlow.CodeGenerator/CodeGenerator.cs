using System;
using System.CodeDom;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Response;
using Alexa.NET.Response.APL;
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
            CodeGeneration_Story.CreateProjectFile(context);
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
                    CodeGeneration_Text.GenerateRecap(context.CodeScope.Reverse().First(o => o is CodeTypeDeclaration) as CodeTypeDeclaration, text, context);
                    break;
            }
            return base.Begin(text, context);
        }

        protected override Task Begin(Visual story, CodeGeneratorContext context)
        {
            var gen = (CodeMemberMethod)context.CodeScope.Peek();

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
            var gen = (CodeMemberMethod)context.CodeScope.Peek();
            switch (property.Key)
            {
                case "template":
                    var layoutCall = CodeGeneration_Visuals.GenerateAplCall(context, property.Value);
                    gen.Statements.Add(new CodeAssignStatement(
                        new CodePropertyReferenceExpression(render, "Document.MainTemplate"),
                        layoutCall));
                    break;
                case "background":
                    var bgDs = CodeGeneration_Visuals.EnsureDataSource(gen, "apl");
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

        protected override Task Begin(SceneInstructions instructions, CodeGeneratorContext context)
        {
            var gen = (CodeMemberMethod)context.CodeScope.Peek();
            return base.Begin(instructions, context);
        }

        protected override Task Begin(SceneInstructionContainer instructions, CodeGeneratorContext context)
        {
            CodeStatementCollection statements = context.CodeScope.Statements();

            if (instructions is If ifstmt)
            {
                if (statements == null)
                {
                    throw new InvalidSkillFlowException("Check to see what happens with if statmeents after hear");
                }
                var codeIf = new CodeConditionStatement(CodeGeneration_Condition.Generate(ifstmt.Condition));
                statements.Add(codeIf);
                context.CodeScope.Push(codeIf);
            }
            else if (instructions is Hear hear)
            {
                CodeGeneration_Interaction.AddHearMarker(context, statements);

                CodeGeneration_Interaction.AddIntent(context, hear.Phrases);
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
                    statements.GoToScene(gto.SceneName);
                    statements.Add(new CodeMethodReturnStatement());
                    break;
                case GoToAndReturn goToAndReturn:
                    statements.GoToScene(goToAndReturn.SceneName);
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
                case Back back:
                    //implement scene stack? Dictionary access to generates?
                    break;
                case End end:
                    break;
                case Pause pause:
                    break;
                case Repeat repeat:
                    break;
                case Reprompt reprompt:
                    break;
                case Restart restart:
                    break;
                case Resume resume:
                    break;
                case Return returnCmd:
                    statements.Add(new CodeMethodReturnStatement());
                    break;
            }
            return base.Render(instruction, context);
        }
    }
}
