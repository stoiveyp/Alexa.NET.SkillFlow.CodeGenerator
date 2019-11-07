using System.Collections.Generic;
using System.Security.Cryptography;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Interaction
    {
        public static void AddIntent(CodeGeneratorContext context, List<string> hearPhrases, object marker)
        {
         //Add to skill manifest with intent name and id.
         //If the intent works already then grab the same id

         //Add request handler for the intent name
         //Ensure the current marker is one of the recognised markers
        }

        public static object AddMarker(CodeGeneratorContext context, List<SceneInstruction> hearInstructions)
        {
            //Create scene and method marker
            //future statements go into the method created by this method (add to scopes)
            //first statement of the method is to update the scene and method markers
            return string.Empty;
        }
    }
}