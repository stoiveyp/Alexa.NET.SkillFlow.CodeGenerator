using System;
using System.Collections.Generic;
using System.Text;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeConstants
    {
        public const string GetCurrentSceneMethodName = "GetScene";
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
    }
}
