using System;
using System.CodeDom;
using System.Linq;
using Alexa.NET.Request;
using Alexa.NET.RequestHandlers;
using Alexa.NET.RequestHandlers.Handlers;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_RequestHandlers
    {
        public static CodeCompileUnit CreateLaunchRequestHandler(this CodeGeneratorContext context)
        {
            string launchRequestName = "Launch";
            if (context.RequestHandlers.ContainsKey(launchRequestName))
            {
                return context.RequestHandlers[launchRequestName];
            }

            var mainClass = CodeGeneration_RequestHandlers.GenerateLaunchHandler(launchRequestName, context);
            return CreateRequestHandlerUnit(context, launchRequestName, mainClass);
        }

        public static CodeCompileUnit CreateIntentRequestHandler(this CodeGeneratorContext context, string intentName)
        {
            if (context.RequestHandlers.ContainsKey(intentName))
            {
                return context.RequestHandlers[intentName];
            }

            var mainClass = CodeGeneration_RequestHandlers.GenerateIntentHandler(intentName, context);
            return CreateRequestHandlerUnit(context, intentName, mainClass);
        }

        private static CodeCompileUnit CreateRequestHandlerUnit(CodeGeneratorContext context, string key, CodeTypeDeclaration mainClass)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            ns.Types.Add(mainClass);
            context.RequestHandlers.Add(key, code);
            return code;
        }

        private static CodeTypeDeclaration GenerateLaunchHandler(string className, CodeGeneratorContext context)
        {
            return GenerateHandlerClass(context, className, mainClass =>
            {
                mainClass.BaseTypes.Add(new CodeTypeReference(typeof(LaunchRequestHandler<APLSkillRequest>)));

                var constructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Public
                };

                mainClass.Members.Add(constructor);
            });
        }

        private static CodeTypeDeclaration GenerateIntentHandler(string className, CodeGeneratorContext context)
        {
            return GenerateHandlerClass(context, className, mainClass =>
            {
                mainClass.BaseTypes.Add(new CodeTypeReference(typeof(IntentNameRequestHandler<APLSkillRequest>)));

                var constructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Public
                };

                constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(className));
                mainClass.Members.Add(constructor);
            });
        }

        private static CodeTypeDeclaration GenerateHandlerClass(CodeGeneratorContext context, string className, Action<CodeTypeDeclaration> adaptToHandler)
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
                new CodeParameterDeclarationExpression(typeof(AlexaRequestInformation<APLSkillRequest>),
                    "information"));

            method.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference("var"),
                    "response",
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(ResponseBuilder)), "Tell", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(string)), "Empty"))
                )
            );

            var methodCall = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await " + ((CodeTypeDeclaration)context.CodeScope.Skip(1).First()).Name),
                ((CodeMemberMethod)context.CodeScope.First()).Name,
                new CodeVariableReferenceExpression("information"),
                new CodeVariableReferenceExpression("response"));
            method.Statements.Add(methodCall);
            method.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("response")));


            mainClass.Members.Add(method);
            return mainClass;
        }
    }
}
