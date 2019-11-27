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

            var method = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Output"),
                "AddSpeech",
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName));
            foreach (var s in text.Content)
            {
                method.Parameters.Add(new CodePrimitiveExpression(s));
            }
            generate.Statements.Add(method);
        }

        public static void GenerateReprompt(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            CodeGeneration_Randomiser.Ensure(context);

            generate.Statements.Add(new CodeMethodInvokeExpression(
                CodeConstants.RequestVariableRef,
                "SetValue",
                new CodePrimitiveExpression("scene_reprompt"),
                CodeConstants.GeneratePickFrom(text.Content)));
        }

        public static void GenerateRecap(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            CodeGeneration_Randomiser.Ensure(context);

            generate.Statements.Add(new CodeMethodInvokeExpression(
                CodeConstants.RequestVariableRef,
                "SetValue",
                new CodePrimitiveExpression("scene_recap"),
                CodeConstants.GeneratePickFrom(text.Content)));
        }
    }
}
