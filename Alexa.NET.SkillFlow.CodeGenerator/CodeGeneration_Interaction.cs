using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Alexa.NET.Management.InteractionModel;
using Alexa.NET.Request.Type;
using Alexa.NET.Response.Directive;

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
            CodeGeneration_Instructions.SetVariable(newMethod.Statements, "_marker", context.Marker);
        }

        public static void AddIntent(CodeGeneratorContext context, List<string> hearPhrases)
        {
            var fallback = hearPhrases.Any(p => p == "*");
            if (fallback)
            {
                hearPhrases.Remove("*");
            }

            var dictionary = hearPhrases.ToDictionary(hp => hp, hp => context.Language.IntentTypes?.FirstOrDefault(i => i.Samples.Contains(hp)));

            var nulls = dictionary.Where(kvp => kvp.Value == null).Select(k => k.Key).ToArray();

            var intentName = "Intent_" + context.Marker;
            if (nulls.Any())
            {
                var intent = new IntentType
                {
                    Name = intentName,
                    Samples = nulls
                };

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);

                foreach (var it in nulls.ToArray())
                {
                    context.CreateIntentRequestHandler(intentName);
                    dictionary[it] = intent;
                }
            }

            foreach (var item in dictionary.Keys.ToArray().Except(nulls))
            {
                var sharedIntent = dictionary[item];
                var sharedHandlerClass = context.RequestHandlers[sharedIntent.Name].Namespaces[0].Types[0];
                var safeItemName = item.Safe();
                if (sharedIntent.Samples.Length > 1)
                {
                    //Move phrase to its own intent
                    sharedIntent.Samples = sharedIntent.Samples.Except(new[] { item }).ToArray();
                    var intent = new IntentType
                    {
                        Name = safeItemName,
                        Samples = new[] { item }
                    };
                    sharedIntent = intent;

                    context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);
                    dictionary[item] = intent;
                    var newHandler = context.CreateIntentRequestHandler(safeItemName);
                    var newStatements = newHandler.Namespaces[0].Types[0].HandleStatements();
                    foreach (var statement in sharedHandlerClass.HandleStatements().OfType<CodeConditionStatement>())
                    {
                        newStatements.Add(statement);
                    }
                }
                else
                {
                    CodeGeneration_RequestHandlers.AddIfMarker(sharedHandlerClass, context);
                }

                var originalName = sharedIntent.Name;
                sharedIntent.Name = safeItemName;
                var rh = context.RequestHandlers[originalName];

                context.RequestHandlers.Remove(originalName);
                context.RequestHandlers.Add(safeItemName, rh);

                var handlerType = rh.Namespaces[0].Types[0];
                handlerType.Name = safeItemName;
                var constructor = handlerType.Members.OfType<CodeConstructor>().First();
                constructor.BaseConstructorArgs.Clear();
                constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(safeItemName));
            }

            //TODO: Handle fallback handlers
            if (fallback)
            {
                //TODO: update fallback with helper method that sets the fallback handler for that marker position
                //TODO: use same helper method with recap support
                context.CreateIntentRequestHandler(BuiltInIntent.Fallback);
            }

        }
    }
}