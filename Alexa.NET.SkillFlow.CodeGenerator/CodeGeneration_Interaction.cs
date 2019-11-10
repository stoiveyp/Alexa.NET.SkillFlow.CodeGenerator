using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Management.InteractionModel;
using Alexa.NET.Request;
using Alexa.NET.RequestHandlers;
using Alexa.NET.RequestHandlers.Handlers;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Interaction
    {
        public static void AddHearMarker(CodeGeneratorContext context)
        {
            //TODO: Add statement that sets marker variable as string - used for shared markers to know which call to make

            while (context.CodeScope.Peek().GetType() != typeof(CodeTypeDeclaration))
            {
                context.CodeScope.Pop();
            }

            var type = context.CodeScope.Peek() as CodeTypeDeclaration;
            var count = type.Members.OfType<CodeMemberMethod>().Count(m => m.Name.StartsWith("Hear"));
            var newMethod = new CodeMemberMethod
            {
                Name = "Hear_" + count,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("async Task")
            };
            newMethod.AddResponseParams();
            type.Members.Add(newMethod);
            context.CodeScope.Push(newMethod);
        }

        public static void AddIntent(CodeGeneratorContext context, List<string> hearPhrases)
        {
            var fallback = hearPhrases.Any(p => p == "*");
            if (fallback)
            {
                hearPhrases.Remove("*");
            }

            var dictionary = hearPhrases.ToDictionary(hp => hp, hp => context.Language.IntentTypes?.FirstOrDefault(i => i.Samples.Contains(hp)));

            var nulls = dictionary.Where(kvp => kvp.Value == null).ToArray();

            var intentName = "Intent_" + context.Marker;
            if (nulls.Any())
            {
                var intent = new IntentType
                {
                    Name = intentName,
                    Samples = nulls.Select(kvp => kvp.Key).ToArray()
                };

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);

                foreach (var it in nulls.ToArray())
                {
                    CreateIntentRequestHandler(context, intentName);
                    dictionary[it.Key] = intent;
                }
            }

            //TODO: Isolate shared handlers - like "yes"

            //TODO: Handle fallback handlers
            if (fallback)
            {
                //dictionary.Add("*",EnsureFallbackRequestHandler(context));
            }

            //TODO: Add RequestHandler Call to each handler based on marker
            foreach (var shared in dictionary.Keys.Except(nulls.Select(n => n.Key)))
            {
                Console.WriteLine(shared);
            }

            
        }

        public static CodeCompileUnit CreateIntentRequestHandler(CodeGeneratorContext context, string intentName)
        {
            if (context.RequestHandlers.ContainsKey(intentName))
            {
                return context.RequestHandlers[intentName];
            }

            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            var mainClass = GenerateHandlerClass(intentName,context);
            ns.Types.Add(mainClass);
            context.RequestHandlers.Add(intentName, code);
            return code;
        }

        private static CodeTypeDeclaration GenerateHandlerClass(string rhName, CodeGeneratorContext context)
        {
            var mainClass = new CodeTypeDeclaration(rhName)
            {
                IsClass = true
            };
            mainClass.BaseTypes.Add(new CodeTypeReference(typeof(IntentNameRequestHandler<APLSkillRequest>)));

            var constructor = new CodeConstructor
            {
                Attributes = MemberAttributes.Public
            };

            constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(rhName));
            mainClass.Members.Add(constructor);


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
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(ResponseBuilder)), "Empty")
                )
            );

            var methodCall = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await " + ((CodeTypeDeclaration) context.CodeScope.Skip(1).First()).Name),
                ((CodeMemberMethod) context.CodeScope.First()).Name,
                new CodeVariableReferenceExpression("information"),
                new CodeVariableReferenceExpression("response"));
            method.Statements.Add(methodCall);
            method.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("response")));

            
            mainClass.Members.Add(method);
            return mainClass;
        }
    }
}