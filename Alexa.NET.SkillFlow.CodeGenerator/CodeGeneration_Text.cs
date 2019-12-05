using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Text
    {
        public static void GenerateSay(CodeMemberMethod generate, Text text, CodeGeneratorContext context)
        {
            CodeGeneration_Randomiser.Ensure(context);

            foreach (var content in text.Content)
            {
                var variableReady = VariableSplitArray(content);
                var method = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("Output"),
                    "AddSpeech",
                    new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                    variableReady.Length == 1 ? variableReady[0] : 
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)), "Concat",
                        variableReady));
                generate.Statements.Add(method);
            }
        }

        public static Match[] GetVariables(string content)
        {
            return ContentVariableRegex.Matches(content).Cast<Match>().ToArray();
        }

        private static readonly Regex ContentVariableRegex = new Regex(@"\{(?<name>\w+)\}", RegexOptions.Compiled);
        private static CodeExpression[] VariableSplitArray(string content)
        {
            var currentPosition = 0;
            var matches = GetVariables(content);

            if (!matches.Any())
            {
                return new CodeExpression[] { new CodePrimitiveExpression(content) };
            }

            var list = new List<CodeExpression>();
            foreach (var match in matches)
            {
                if (match.Index != currentPosition)
                {
                    list.Add(new CodePrimitiveExpression(content.Substring(currentPosition,match.Index-currentPosition)));
                }

                var variable = match.Groups["name"].Value;
                list.Add(CodeGeneration_Instructions.GetVariable(variable,typeof(string)));
                currentPosition = match.Index + match.Length;
            }

            if (currentPosition != content.Length)
            {
                list.Add(new CodePrimitiveExpression(content.Substring(currentPosition, content.Length - currentPosition)));
            }
            return list.ToArray();
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
