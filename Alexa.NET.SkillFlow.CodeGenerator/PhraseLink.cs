using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Alexa.NET.Management.InteractionModel;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class PhraseLink
    {
        public PhraseLink(string phrase, string marker)
        {
            OriginalPhrase = phrase;
            OriginalMark = marker;
        }

        public string OriginalPhrase { get; }
        public string OriginalMark { get; }

        public string ClassName => IntentName.Safe();

        public string IntentName => OriginalPhrase switch
        {
            "yes" => "AMAZON.YesIntent",
            "no" => "AMAZON.NoIntent",
            "help" => "AMAZON.HelpIntent",
            "stop" => "AMAZON.StopIntent",
            _ => OriginalMark.Safe()
        };

        public string Phrase => OriginalPhrase switch
        {
            "yes" => null,
            "no" => null,
            "help" => null,
            "stop" => null,
            _ => OriginalPhrase
        };

        public bool IsMatch(IntentType intentType)
        {
            return intentType.Name == IntentName || intentType.Samples.Contains(Phrase);
        }

        public Slot[] Slots(CodeGeneratorContext context)
        {
            if (string.IsNullOrWhiteSpace(Phrase))
            {
                return new Slot[]{};
            }
            return CodeGeneration_Text.GetVariables(Phrase).Select(m => m.Groups[1].Value).Select(s =>
                new Slot {Name = s, Type = context.Slots.ContainsKey(s) ? context.Slots[s] : null}).ToArray();
        }
    }
}
