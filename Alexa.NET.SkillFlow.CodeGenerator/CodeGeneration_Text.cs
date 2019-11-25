using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Text
    {
        public static void GenerateSay(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            CodeGeneration_Randomiser.Ensure(context);
            var left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("responseBody"), "OutputSpeech");
            var right = text.AsCodeOutputSpeech(generate);
            var singleSayAssign = new CodeAssignStatement(left, right);
            generate.Statements.Add(singleSayAssign);
        }

        public static void GenerateReprompt(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            CodeGeneration_Randomiser.Ensure(context);

            var right = text.AsCodeOutputSpeech(generate);
            var reprompt = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "reprompt", new CodeObjectCreateExpression(typeof(Reprompt)));
            generate.Statements.Add(reprompt);

            generate.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("reprompt"), "OutputSpeech"), right));

            var left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("responseBody"), "Reprompt");
            var singleSayAssign = new CodeAssignStatement(left, new CodeVariableReferenceExpression("reprompt"));
            generate.Statements.Add(singleSayAssign);
        }

        public static void GenerateRecap(CodeTypeDeclaration type, Text text, CodeGeneratorContext context)
        {
            var method = new CodeMemberMethod
            {
                Name = "Recap",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("async Task")
            };

            method.AddFlowParams();
            type.Members.Add(method);
            GenerateSay(method, text, context);

            CodeGeneration_Fallback.AddToFallback(context,
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(type.Name), "Recap",
                    new CodeVariableReferenceExpression("information"),
                    new CodeVariableReferenceExpression("response"))
            );

        }
    }
}
