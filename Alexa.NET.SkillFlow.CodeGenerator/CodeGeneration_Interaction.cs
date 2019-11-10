using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Management.InteractionModel;
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