﻿using System;
using System.CodeDom;
using System.Linq;
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
            var sceneClass = code.FirstType();
            context.SceneFiles.Add(CodeGeneration_Scene.SceneClassName(scene.Name), code);
            context.CodeScope.Push(sceneClass);
            context.CodeScope.Push(sceneClass.GetGenerateMethod());

            if (scene.Name.Equals("start", StringComparison.OrdinalIgnoreCase))
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
            var gen = (CodeMemberMethod)context.CodeScope.Peek();
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
            gen.CleanIfEmpty();
            context.SetMarker(context.CodeScope.Statements());
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
                context.SetMarker(statements);
            }
            else if (instructions is Hear hear)
            {
                CodeGeneration_Interaction.AddHearMarker(context);


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
                context.CodeScope.Push(typeDeclaration.GetGenerateMethod());
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
                    statements = codeType.GetGenerateMethod().Statements;
                    break;
                case CodeConditionStatement stmt:
                    statements = stmt.TrueStatements;
                    break;
                default:
                    return Noop(context);
            }


            switch (instruction)
            {
                case GoTo gto:
                    statements.Add(new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression("await " + CodeGeneration_Scene.SceneClassName(gto.SceneName)),
                        "Generate",
                        new CodeVariableReferenceExpression("request"),
                        new CodeVariableReferenceExpression("responseBody")));
                    statements.Add(new CodeMethodReturnStatement());
                    break;
            }
            return base.Render(instruction, context);
        }
    }
}
