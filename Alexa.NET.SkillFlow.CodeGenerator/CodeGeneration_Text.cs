using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Text
    {
        public static void GenerateSay(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            CodeGeneration_Randomiser.Ensure(context);
            var left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("response"), "OutputSpeech");
            var right = text.AsCodeOutputSpeech(generate);
            var singleSayAssign = new CodeAssignStatement(left, right);
            generate.Statements.Add(singleSayAssign);
        }

        public static void GenerateReprompt(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            CodeGeneration_Randomiser.Ensure(context);

            var right = text.AsCodeOutputSpeech(generate);
            var reprompt = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "reprompt",new CodeObjectCreateExpression(typeof(Reprompt)));
            generate.Statements.Add(reprompt);

            generate.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("reprompt"),"OutputSpeech"), right));

            var left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("response"), "Reprompt");
            var singleSayAssign = new CodeAssignStatement(left, new CodeVariableReferenceExpression("reprompt"));
            generate.Statements.Add(singleSayAssign);
        }

        public static void GenerateRecap(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            CodeGeneration_Fallback.AddToFallback(context,new CodeStatement[]{});
        }
    }
}
