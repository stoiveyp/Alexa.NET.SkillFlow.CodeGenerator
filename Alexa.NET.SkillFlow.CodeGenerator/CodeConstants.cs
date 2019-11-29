using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeConstants
    {
        public const string FallbackMarker = "_fallback";
        public const string IsCandidateMethodName = "IsCandidate";
        public const string EnableCandidateMethodName = "EnableCandidate";
        public const string ClearCandidateMethodName = "ClearCandidates";
        public const string CurrentSceneMethodName = "CurrentScene";
        public const string AddInteractionMethodName = "AddInteraction";
        public const string MainSceneMarker = "_main";
        public const string RequestVariableName = "request";
        public const string ResponseVariableName = "response";
        public const string NavigationResumeMethodName = "Resume";
        public const string InteractionParameterName = "interaction";
        public const string HandlerPrimaryMethod = "Handle";
        public const string SceneInteractionMethod = "Interact";
        public const string ScenePrimaryMethod = "Main";
        public const string NavigationMethodName = "Interact";
        public const string OutputGenerateMethod = "Generate";

        public static readonly CodeVariableReferenceExpression RequestVariableRef =
            new CodeVariableReferenceExpression(RequestVariableName);

        public static readonly CodeTypeReference Var = new CodeTypeReference("var");
        public static readonly CodeTypeReference AsyncTask = new CodeTypeReference("async Task");

        public static CodeExpression GeneratePickFrom(IEnumerable<string> content)
        {
            if (content.Count() == 1)
            {
                return new CodePrimitiveExpression(content.First());
            }
            return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Randomiser"),"PickRandom",content.Select(s => new CodePrimitiveExpression(s)).ToArray());
        }
    }
}
