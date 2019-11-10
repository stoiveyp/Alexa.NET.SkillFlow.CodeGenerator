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

            //Add to Skill
            var dictionary = hearPhrases.ToDictionary(hp => hp, hp => context.Language.IntentTypes?.FirstOrDefault(i => i.Samples.Contains(hp)));

            var nulls = dictionary.Where(kvp => kvp.Value == null).ToArray();

            if (nulls.Any())
            {
                var intent = new IntentType
                {
                    Name = context.Marker,
                    Samples = nulls.Select(kvp => kvp.Key).ToArray()
                };

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);

                foreach (var it in nulls.ToArray())
                {
                    CreateIntentRequestHandler(context);
                    dictionary[it.Key] = intent;
                }
            }

            foreach(var shared in dictionary.Keys.Except(nulls.Select(n => n.Key)))
            {
                Console.WriteLine(shared);
            }

            if (fallback)
            {
                //dictionary.Add("*",EnsureFallbackRequestHandler(context));
            }
        }

        public static CodeCompileUnit CreateIntentRequestHandler(CodeGeneratorContext context)
        {
            var rhName = "Intent_" + context.Marker;
            if (context.RequestHandlers.ContainsKey(rhName))
            {
                return context.RequestHandlers[rhName];
            }

            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            var mainClass = GenerateHandlerClass(rhName);
            ns.Types.Add(mainClass);
            context.RequestHandlers.Add(rhName, code);
            return code;
        }

        private static CodeTypeDeclaration GenerateHandlerClass(string rhName)
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
                new CodeThrowExceptionStatement(new CodeObjectCreateExpression(typeof(NotImplementedException))));

            mainClass.Members.Add(method);
            return mainClass;
        }
    }
}