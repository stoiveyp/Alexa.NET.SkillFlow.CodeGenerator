using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Alexa.NET.Management.InteractionModel;
using Alexa.NET.Request.Type;

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

            //TODO: Isolate shared handlers - like "yes"
            foreach (var item in dictionary.Keys.ToArray().Except(nulls))
            {
                var sharedIntent = dictionary[item];
                var sharedHandler = context.RequestHandlers[sharedIntent.Name].Namespaces[0].Types[0];

                var multiple = false;
                if (sharedIntent.Samples.Length > 1)
                {
                    //Move phrase to its own intent
                    sharedIntent.Samples = sharedIntent.Samples.Except(new []{item}).ToArray();
                    var intent = new IntentType
                    {
                        Name = item.Safe(),
                        Samples = new[] {item}
                    };

                    context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);
                    dictionary[item] = intent;
                }

                if (multiple)
                {
                    //TODO: Duplicate the request handler so the shared phrase has isolated logic compared to other phrases
                }

                //TODO: regardless - update the request handler to match the single phrase, not the context marker it was used for


                CodeGeneration_RequestHandlers.AddIfMarker(sharedHandler, context);
            }

            //TODO: Handle fallback handlers
            if (fallback)
            {
                context.CreateIntentRequestHandler(BuiltInIntent.Fallback);
            }

        }
    }
}