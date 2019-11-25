using System;
using System.Collections.Generic;
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
    }
}
