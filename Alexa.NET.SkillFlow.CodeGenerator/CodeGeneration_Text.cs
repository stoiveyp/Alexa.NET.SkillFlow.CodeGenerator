using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Text
    {
        public static void GenerateSay(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            if (text.Content.Count == 1)
            {
                var left = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("response"), "OutputSpeech");
                var right = text.AsCodeOutputSpeech(generate);
                var singleSayAssign = new CodeAssignStatement(left, right);
                generate.Statements.Add(singleSayAssign);
            }
            else
            {
                CodeGeneration_Randomiser.Ensure(context);
            }
        }
    }
}
