using System;
using System.CodeDom;

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

            var mainClass = GenerateLaunchHandler(launchRequestName, context);
            return CreateRequestHandlerUnit(context, launchRequestName, mainClass);
        }

        public static CodeCompileUnit CreateIntentRequestHandler(this CodeGeneratorContext context, string intentName, bool addIfMarker = true)
        {
            if (context.RequestHandlers.ContainsKey(intentName))
            {
                return context.RequestHandlers[intentName];
            }

            CodeGeneration_Output.Ensure(context);

            var mainClass = GenerateIntentHandler(intentName, context);
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

        private static CodeTypeDeclaration GenerateLaunchHandler(string className, CodeGeneratorContext context)
        {
            var handler = GenerateHandlerClass(context, className, mainClass =>
            {
                mainClass.BaseTypes.Add(new CodeTypeReference("LaunchRequestHandler<APLSkillRequest>"));

                var constructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Public
                };

                mainClass.Members.Add(constructor);
            });

            var statements = handler.MethodStatements(CodeConstants.HandlerPrimaryMethod);
            var resumeMethod = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await Navigation"),
                CodeConstants.NavigationResumeMethodName,
                new CodeVariableReferenceExpression("request"));

            statements.AddBeforeReturn(resumeMethod);
            return handler;
        }

        private static CodeTypeDeclaration GenerateIntentHandler(string className, CodeGeneratorContext context)
        {
            var handler = GenerateHandlerClass(context, className.Safe(), mainClass =>
            {
                mainClass.BaseTypes.Add(new CodeTypeReference("IntentNameRequestHandler<APLSkillRequest>"));

                var constructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Public
                };

                constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(className));
                mainClass.Members.Add(constructor);
            });

            return handler;
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
                Name = CodeConstants.HandlerPrimaryMethod,
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
                ReturnType = new CodeTypeReference("async Task<SkillResponse>")
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    CodeConstants.RequestVariableName));

            method.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference("var"),
                    CodeConstants.ResponseVariableName,
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(ResponseBuilder)),
                        "Ask",
                        new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(string)), "Empty"),
                    new CodePrimitiveExpression(null))
                )
            );

            method.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("await Output"),CodeConstants.OutputGenerateMethod,new CodeVariableReferenceExpression("request"))));
            mainClass.Members.Add(method);
            return mainClass;
        }
    }
}
