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
                    context.CreateIntentRequestHandler(intentName);
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
    }
}