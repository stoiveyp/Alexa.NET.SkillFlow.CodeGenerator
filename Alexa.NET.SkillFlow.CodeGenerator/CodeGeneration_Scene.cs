﻿using System.CodeDom;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Scene
    {
        public static string SceneClassName(string sceneName)
        {
            return "Scene_" + char.ToUpper(sceneName[0]) + sceneName.Substring(1).Replace(" ", "_");
        }

        public static CodeCompileUnit Generate(Scene scene, CodeGeneratorContext context)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            var mainClass = GenerateSceneClass(scene);
            ns.Types.Add(mainClass);
            return code;
        }

        private static CodeTypeDeclaration GenerateSceneClass(Scene scene)
        {
            var mainClass = new CodeTypeDeclaration(SceneClassName(scene.Name))
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };

            mainClass.Members.Add(CreateMain());
            mainClass.Members.Add(CreateInteract(mainClass.Name));
            return mainClass;
        }

        private static CodeMemberMethod CreateMain()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.ScenePrimaryMethod,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = CodeConstants.AsyncTask
            };

            method.AddFlowParams();
            return method;
        }

        private static CodeMemberMethod CreateInteract(string className)
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.SceneInteractionMethod,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = CodeConstants.AsyncTask
            };
            method.AddInteractionParams();
            method.AddFlowParams();
            var statements = method.Statements;

            statements.Add(new CodeSnippetStatement("\t\tswitch("+ CodeConstants.InteractionParameterName +"){"));
            statements.Add(new CodeSnippetStatement("\t\t}"));

            var invoke = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await " + className),
                CodeConstants.ScenePrimaryMethod);
            invoke.AddFlowParameters();

            statements.AddInteraction(className,CodeConstants.MainSceneMarker,invoke);

            return method;
        }
    }
}
