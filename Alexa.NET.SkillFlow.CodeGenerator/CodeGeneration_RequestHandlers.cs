﻿using System;
using System.CodeDom;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_RequestHandlers
    {
        public static CodeCompileUnit CreateStopRequestHandler(this CodeGeneratorContext context)
        {
            string launchRequestName = "AMAZON.StopIntent";
            if (context.RequestHandlers.ContainsKey(launchRequestName))
            {
                return context.RequestHandlers[launchRequestName];
            }

            var mainClass = GenerateLaunchHandler(launchRequestName, context);
            return CreateRequestHandlerUnit(context, launchRequestName, mainClass);
        }

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

        public static CodeCompileUnit CreateIntentRequestHandler(this CodeGeneratorContext context, string intentName)
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
            var handler = GenerateHandlerClass(className, mainClass =>
            {
                mainClass.BaseTypes.Add(new CodeTypeReference("LaunchRequestHandler<APLSkillRequest>"));

                var constructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Public
                };

                mainClass.Members.Add(constructor);
                return false;
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
            var handler = GenerateHandlerClass(className.Safe(), mainClass =>
            {
                mainClass.BaseTypes.Add(new CodeTypeReference("IntentNameRequestHandler<APLSkillRequest>"));

                var constructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Public
                };

                className = className.StartsWith("AMAZON_") ? "AMAZON." + className.Substring(7) : className;
                constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(className));
                mainClass.Members.Add(constructor);
                return true;
            });

            return handler;
        }

        private static CodeTypeDeclaration GenerateHandlerClass(string className, Func<CodeTypeDeclaration,bool> adaptToHandler)
        {
            var mainClass = new CodeTypeDeclaration(className)
            {
                IsClass = true
            };

            var addUpdateFromSlot = adaptToHandler(mainClass);

            var method = new CodeMemberMethod
            {
                Name = CodeConstants.HandlerPrimaryMethod,
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
                ReturnType = new CodeTypeReference("async Task<SkillResponse>")
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    CodeConstants.RequestVariableName));

            if (addUpdateFromSlot)
            {
                method.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("State"),
                    "UpdateFromSlots", CodeConstants.RequestVariableRef));
            }

            method.Statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "handled",
                new CodePrimitiveExpression(false)));

            method.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("await Output"),CodeConstants.OutputGenerateMethod,new CodeVariableReferenceExpression("request"))));
            mainClass.Members.Add(method);
            return mainClass;
        }
    }
}
