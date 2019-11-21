using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class Utility
    {
        public static CodeTypeDeclaration FirstType(this CodeCompileUnit unit)
        {
            return unit.Namespaces[0].Types[0];
        }

        public static CodeStatementCollection HandleStatements(this CodeTypeDeclaration type)
        {
            return MethodStatements(type, "Handle");
        }

        public static CodeStatementCollection MethodStatements(this CodeTypeDeclaration type, string methodName, bool generate = false)
        {
            var candidate = type.Members.OfType<CodeMemberMethod>()
                .FirstOrDefault(m => m.Name == methodName);

            return candidate?.Statements;
        }

        public static CodeMethodInvokeExpression RunMarker(this CodeGeneratorContext context, bool wait = true)
        {
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(
                    (wait ? "await " : string.Empty) + ((CodeTypeDeclaration) context.CodeScope.Skip(1).First()).Name),
                ((CodeMemberMethod) context.CodeScope.First()).Name,
                new CodeVariableReferenceExpression("information"),
                new CodeVariableReferenceExpression("response"));
        }

        public static CodeStatementCollection Statements(this Stack<CodeObject> stack)
        {
            switch (stack.Peek())
            {
                case CodeMemberMethod member:
                    return member.Statements;
                case CodeConditionStatement conditional:
                    return conditional.TrueStatements;
                default:
                    return null;
            }
        }

        public static string Safe(this string candidate)
        {
            var osb = new StringBuilder();
            foreach (char s in candidate)
            {
                if (s == ' ' || !char.IsLetterOrDigit(s))
                {
                    osb.Append('_');
                    continue;
                }

                osb.Append(s);
            }

            return osb.ToString();
        }

        public static T[] Add<T>(this T[] array, params T[] toAdd)
        {
            return Add(array, toAdd.AsEnumerable());
        }

        public static T[] Add<T>(this T[] array, IEnumerable<T> toAdd)
        {
            return (array ?? new T[] { }).Concat(toAdd).ToArray();
        }

        public static void AddResponseParams(this CodeMemberMethod method, bool includeResponseVariable = false)
        {
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<APLSkillRequest>"), "request"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SkillResponse).AsSimpleName(), "response"));
            if (includeResponseVariable)
            {
                var assignment = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "responseBody", new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("response"), "Response"));
                method.Statements.Add(assignment);
            }
        }

        public static CodeMemberMethod GetGenerateMethod(this CodeTypeDeclaration currentClass)
        {
            return currentClass.Members.OfType<CodeMemberMethod>().First(cmm => cmm.Name == "Generate");
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
            if (text.Content.Count == 0)
            {
                throw new InvalidSkillFlowException($"No content in {text.TextType}");
            }

            var varNames = text.Content.Select((c, i) => AddOutputSpeech(method, $"{text.TextType}_{i}", c)).ToArray();

            if (varNames.Length == 1)
            {
                return new CodeVariableReferenceExpression(varNames.First());
            }

            var generatorCall = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Randomiser"),
                "PickRandom",
                varNames.Select(v => new CodeVariableReferenceExpression(v)).Cast<CodeExpression>().ToArray());
            var randomVar = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), text.TextType, generatorCall);
            method.Statements.Add(randomVar);
            return new CodeVariableReferenceExpression(text.TextType);
        }

        private static string AddOutputSpeech(CodeMemberMethod method, string varName, string content)
        {
            var isSsml = content.Any(c => c == '<' || c == '>');
            var creationType = isSsml ? typeof(SsmlOutputSpeech) : typeof(PlainTextOutputSpeech);

            method.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("var"), varName)
            {
                InitExpression = new CodeObjectCreateExpression(creationType, new CodePrimitiveExpression(content))
            });
            return varName;
        }


    }
}
