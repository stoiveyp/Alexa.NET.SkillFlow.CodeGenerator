﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Alexa.NET.Request;
using Alexa.NET.RequestHandlers;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class Utility
    {
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

        public static void AddResponseParams(this CodeMemberMethod method)
        {
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<APLSkillRequest>"), "request"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SkillResponse).AsSimpleName(), "responseBody"));
        }

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
