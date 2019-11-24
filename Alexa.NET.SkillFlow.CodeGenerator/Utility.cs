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

        public static CodeStatementCollection MethodStatements(this CodeTypeDeclaration type, string methodName, bool generate = false)
        {
            var candidate = type.Members.OfType<CodeMemberMethod>()
                .FirstOrDefault(m => m.Name == methodName);

            return candidate?.Statements;
        }

        public static CodeMethodInvokeExpression RunMarker(this CodeGeneratorContext context, bool wait = true)
        {
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await Navigation"),
                CodeConstants.NavigationMethodName,
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                new CodeVariableReferenceExpression(CodeConstants.ResponseVariableName)
            );
        }

        public static void AddBeforeReturn(this CodeStatementCollection statements, params CodeObject[] codes)
        {
            var last = statements[statements.Count - 1];
            statements.Remove(last);
            foreach (var code in codes)
            {
                if (code is CodeStatement stmt)
                {
                    statements.Add(stmt);
                }
                else if (code is CodeExpression expr)
                {
                    statements.Add(expr);
                }
            }

            statements.Add(last);
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

        public static void AddInteractionParams(this CodeMemberMethod method)
        {
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), CodeConstants.InteractionParameterName));
        }

        public static CodeMethodInvokeExpression AddFlowParameters(this CodeMethodInvokeExpression method)
        {
            method.Parameters.Add(new CodeVariableReferenceExpression(CodeConstants.RequestVariableName));
            method.Parameters.Add(new CodeVariableReferenceExpression(CodeConstants.ResponseVariableName));
            return method;
        }

        public static void AddInteraction(this CodeStatementCollection statements, string interactionName,
            CodeMethodInvokeExpression method)
        {
            statements.AddBeforeReturn(
                new CodeSnippetStatement($"\t\t\tcase \"{interactionName}\":"),
                method,
                new CodeSnippetExpression("\t\t\tbreak")
                );
        }

        public static void AddRequestParam(this CodeMemberMethod method)
        {
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<APLSkillRequest>"), "request"));
        }

        public static void AddResponseParam(this CodeMemberMethod method)
        {
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SkillResponse).AsSimpleName(), "response"));
        }

        public static void AddFlowParams(this CodeMemberMethod method, bool includeResponseVariable = false)
        {
            AddRequestParam(method);
            AddResponseParam(method);

            if (includeResponseVariable)
            {
                var assignment = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "responseBody", new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("response"), "Response"));
                method.Statements.Add(assignment);
            }
        }

        public static CodeMemberMethod GetMainMethod(this CodeTypeDeclaration currentClass)
        {
            return currentClass.Members.OfType<CodeMemberMethod>().First(cmm => cmm.Name == CodeConstants.ScenePrimaryMethod);
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
