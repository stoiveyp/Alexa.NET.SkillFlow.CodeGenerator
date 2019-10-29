using System;
using System.CodeDom;
using System.Linq;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class Utility
    {
        public static CodeMemberMethod GetGenerateMethod(this CodeTypeDeclaration currentClass)
        {
            return currentClass.Members.OfType<CodeMemberMethod>().First(cmm => cmm.Name == "Generate");
        }

        public static void CleanIfEmpty(this CodeMemberMethod method)
        {
            if (method.Statements.Count == 2 && method.Statements[1] is CodeThrowExceptionStatement throwStatement)
            {
                method.Statements.Remove(throwStatement);
            }
        }

        public static CodeTypeReference AsSimpleName(this Type type)
        {
            return new CodeTypeReference(type.Name);
        }

        public static CodeTypeReferenceExpression AsSimpleExpression(this Type type)
        {
            return new CodeTypeReferenceExpression(type.Name);
        }

        public static CodeVariableReferenceExpression AsCodeOutputSpeech(this Text text, CodeMemberMethod method)
        {
            if (text.Content.Count == 1)
            {
                var outputSpeech = text.Content[0];
                var creationType = outputSpeech.Any(c => c == '<' || c == '>')
                    ? typeof(SsmlOutputSpeech)
                    : typeof(PlainTextOutputSpeech);
                var variable = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), text.TextType, new CodeObjectCreateExpression(creationType));
                var assignment = new CodeAssignStatement(
                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(text.TextType), "Text"),
                    new CodePrimitiveExpression(outputSpeech));

                method.Statements.Add(variable);
                method.Statements.Add(assignment);
                return new CodeVariableReferenceExpression(text.TextType);
            }

            throw new NotImplementedException("Not written multiple statements yet");
        }
    }
}
