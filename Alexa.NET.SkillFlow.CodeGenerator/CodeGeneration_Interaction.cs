using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Alexa.NET.Management.InteractionModel;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Interaction
    {


        public static void AddIntent(CodeGeneratorContext context, List<string> hearPhrases)
        {
            //Add to Skill
            var dictionary = hearPhrases.ToDictionary(hp => hp,hp => context.Language.IntentTypes?.FirstOrDefault(i => i.Samples.Contains(hp)));

            var nulls = dictionary.Where(kvp => kvp.Value == null);

            var intent = new IntentType
            {
                Name = context.Marker,
                Samples = nulls.Select(kvp => kvp.Key).ToArray()
            };

            context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);
            foreach (var it in nulls.ToArray())
            {
                dictionary[it.Key] = intent;
            }
        }
    }
}