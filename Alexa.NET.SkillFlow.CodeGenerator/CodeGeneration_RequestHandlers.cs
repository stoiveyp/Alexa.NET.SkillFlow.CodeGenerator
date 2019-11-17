using System;
using System.CodeDom;
using System.Linq;
using System.Linq.Expressions;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.RequestHandlers;
using Alexa.NET.RequestHandlers.Handlers;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_RequestHandlers
    {
        public static CodeCompileUnit CreateLaunchRequestHandler(this CodeGeneratorContext context, bool addIfMarker = true)
        {
            string launchRequestName = "Launch";
            if (context.RequestHandlers.ContainsKey(launchRequestName))
            {
                return context.RequestHandlers[launchRequestName];
            }

            var mainClass = GenerateLaunchHandler(launchRequestName, context, addIfMarker);
            return CreateRequestHandlerUnit(context, launchRequestName, mainClass);
        }

        public static CodeCompileUnit CreateIntentRequestHandler(this CodeGeneratorContext context, string intentName, bool addIfMarker = true)
        {
            if (context.RequestHandlers.ContainsKey(intentName))
            {
                return context.RequestHandlers[intentName];
            }

            var mainClass = GenerateIntentHandler(intentName, context, addIfMarker);
            return CreateRequestHandlerUnit(context, intentName, mainClass);
        }

        private static CodeCompileUnit CreateRequestHandlerUnit(CodeGeneratorContext context, string key, CodeTypeDeclaration mainClass)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers.Handlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            ns.Types.Add(mainClass);
            context.RequestHandlers.Add(key, code);
            return code;
        }

        private static CodeTypeDeclaration GenerateLaunchHandler(string className, CodeGeneratorContext context, bool addIfMarker)
        {
            return GenerateHandlerClass(context, className, mainClass =>
            {
                mainClass.BaseTypes.Add(new CodeTypeReference("LaunchRequestHandler<APLSkillRequest>"));

                var constructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Public
                };

                mainClass.Members.Add(constructor);
            }, addIfMarker);
        }

        private static CodeTypeDeclaration GenerateIntentHandler(string className, CodeGeneratorContext context, bool addIfMarker)
        {
            return GenerateHandlerClass(context, className.Safe(), mainClass =>
            {
                mainClass.BaseTypes.Add(new CodeTypeReference("IntentNameRequestHandler<APLSkillRequest>"));

                var constructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Public
                };

                constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(className));
                mainClass.Members.Add(constructor);
            }, addIfMarker);
        }

        private static CodeTypeDeclaration GenerateHandlerClass(CodeGeneratorContext context, string className, Action<CodeTypeDeclaration> adaptToHandler, bool addIfMarker)
        {
            var mainClass = new CodeTypeDeclaration(className)
            {
                IsClass = true
            };

            adaptToHandler(mainClass);

            var method = new CodeMemberMethod
            {
                Name = "Handle",
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
                ReturnType = new CodeTypeReference("async Task<SkillResponse>")
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    "information"));

            method.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference("var"),
                    "response",
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(ResponseBuilder)), "Tell", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(string)), "Empty"))
                )
            );

            method.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("response")));
            mainClass.Members.Add(method);
            if (addIfMarker)
            {
                AddIfMarker(mainClass, context);
            }

            return mainClass;
        }

        public static void AddIfMarker(CodeTypeDeclaration mainClass, CodeGeneratorContext context)
        {
            var statements = mainClass.HandleStatements();

            var statement = context.RunMarker();

            //TODO: global append support - no if, just do it

            var ifCall = new CodeConditionStatement
            {
                Condition = new CodeBinaryOperatorExpression(
                    new CodeMethodInvokeExpression(
                        new CodePropertyReferenceExpression(
                            new CodeVariableReferenceExpression("await information"), "State"),
                        "Get<string>",
                        new CodePrimitiveExpression("_marker")),
                    CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(context.GenerateMarker(1))),
                TrueStatements = { statement },
                FalseStatements = { new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("await " + BuiltInIntent.Fallback.Safe()),
                    "Fallback",
                    new CodeVariableReferenceExpression("information"),
                    new CodeVariableReferenceExpression("response"))
                }
            };

            if (statements.Count == 0)
            {
                statements.Add(ifCall);
            }
            else
            {
                statements.Insert(statements.Count - 1, ifCall);
            }
        }
    }
}
