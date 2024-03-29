﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.RequestHandlers;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Navigation
    {
        private static CodeStatementCollection _sceneRegistration;
        public static void RegisterScene(CodeGeneratorContext context, string sceneName, CodeMethodReferenceExpression runScene)
        {
            EnsureNavigation(context);
            _sceneRegistration.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_scenes"), "Add",
                new CodePrimitiveExpression(sceneName), runScene));
        }

        public static CodeMethodInvokeExpression GoToScene(string sceneName)
        {
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await Navigation"),
                CodeConstants.NavigationMethodName,
                new CodePrimitiveExpression(sceneName),
                new CodePrimitiveExpression(CodeConstants.MainSceneMarker),
                new CodeVariableReferenceExpression("request"));
        }

        public static CodeMethodInvokeExpression AddInteraction(string sceneName, string interactionName)
        {
            return AddInteraction(
                new CodePrimitiveExpression(sceneName),
                new CodePrimitiveExpression(interactionName));
        }

        public static CodeMethodInvokeExpression AddInteraction(CodeExpression sceneName, CodeExpression interactionName)
        {
            var methodInvoke = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Navigation"),
                CodeConstants.AddInteractionMethodName,
                CodeConstants.RequestVariableRef,
                sceneName,
                interactionName);

            return methodInvoke;
        }

        private static void EnsureNavigation(CodeGeneratorContext context)
        {
            if (context.OtherFiles.ContainsKey("Navigation.cs"))
            {
                return;
            }

            context.OtherFiles.Add("Navigation.cs", CreateNavigationFile(context, CreateNavigationClass()));
        }

        private static CodeCompileUnit CreateNavigationFile(CodeGeneratorContext context, CodeTypeDeclaration navigationClass)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("System"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            ns.Imports.Add(new CodeNamespaceImport("System.Linq"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            ns.Types.Add(navigationClass);
            return code;
        }

        private static CodeTypeDeclaration CreateNavigationClass()
        {
            var type = new CodeTypeDeclaration
            {
                Name = "Navigation",
                Attributes = MemberAttributes.Public
            };


            type.StartDirectives.Add(new CodeRegionDirective(
                CodeRegionMode.Start, Environment.NewLine + "\tstatic"));

            type.EndDirectives.Add(new CodeRegionDirective(
                CodeRegionMode.End, string.Empty));


            type.Members.Add(CreateNotTrackedScenes());
            type.Members.Add(CreateLookup());
            type.Members.Add(CreateStaticConstructor());
            type.Members.Add(CreateInteractLatestScene());
            type.Members.Add(CreateInteract());
            type.Members.Add(CreateAddInteractionMethod());
            type.Members.Add(CreateCurrentSceneMethod());
            type.Members.Add(CreateResume());
            type.Members.Add(CreateEnableCandidateInteraction());
            type.Members.Add(CreateHasCandidatesInteractions());
            type.Members.Add(CreateIsEnabledCandidateInteractions());
            type.Members.Add(CreateClearCandidateInteractions());
            type.Members.Add(CreatePause());
            type.Members.Add(CreateBack());

            return type;
        }

        private static CodeTypeMember CreateBack()
        {
            var method = new CodeMemberMethod
            {
                Name = "Back",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = CodeConstants.AsyncTask

            };
            method.AddRequestParam();
            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "sceneList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string[]>", new CodePrimitiveExpression(SceneListItemName))));

            method.Statements.Add(new CodeConditionStatement(
                new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("sceneList"), CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(null)),CodeBinaryOperatorType.BooleanOr,new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("!sceneList"),"Any")), new CodeMethodReturnStatement()));

            var scenes = new CodeVariableReferenceExpression("scenes");
            method.Statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "scenes",
                new CodeObjectCreateExpression(new CodeTypeReference("List<string>"),
                    new CodeVariableReferenceExpression("sceneList"))));
            method.Statements.Add(new CodeMethodInvokeExpression(scenes, "Remove",
                new CodeMethodInvokeExpression(scenes, "Last")));
            method.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"),
                "SetValue",
                new CodePrimitiveExpression("_scenes"),
                new CodeMethodInvokeExpression(scenes, "ToArray")));
            method.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("await Navigation"),
                "Resume", CodeConstants.RequestVariableRef, new CodePrimitiveExpression(true)));
            return method;
        }

        private static CodeTypeMember CreatePause()
        {
            var method = new CodeMemberMethod
            {
                Name = "Pause",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = CodeConstants.AsyncTask

            };
            method.AddRequestParam();

            var pauseRequest = new CodeConditionStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_scenes"), "ContainsKey", new CodePrimitiveExpression("pause")),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("await Navigation"),
                "Interact",
                new CodePrimitiveExpression("pause"),
                new CodePrimitiveExpression(CodeConstants.MainSceneMarker),
                CodeConstants.RequestVariableRef)));
            method.Statements.Add(pauseRequest);
            return method;
        }

        public static readonly string[] NotTrackedSceneNames = new[] {"global prepend", "global append", "resume", "pause"};

        private static CodeMemberField CreateNotTrackedScenes()
        {
            return new CodeMemberField(typeof(string[]), "_notTracked")
            {
                Attributes = MemberAttributes.Static,
                InitExpression = new CodeArrayCreateExpression(typeof(string),
                    NotTrackedSceneNames.Select(s => new CodePrimitiveExpression(s)).ToArray())
            };
        }

        private static CodeTypeMember CreateHasCandidatesInteractions()
        {
            var method = new CodeMemberMethod
            {
                Name = "HasCandidates",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference(typeof(bool))
            };
            method.AddRequestParam();

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "candidateList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string[]>", new CodePrimitiveExpression(CandidateVariableName))));
            method.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("candidateList == null ? false : candidateList.Any()")));
            return method;
        }

        private static CodeTypeMember CreateIsEnabledCandidateInteractions()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.IsCandidateMethodName,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference(typeof(bool))
            };

            method.AddRequestParam();
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "candidate"));

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "candidateList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string[]>", new CodePrimitiveExpression(CandidateVariableName))));
            method.Statements.Add(new CodeSnippetExpression("var list = candidateList == null ? new List<string>() : new List<string>(candidateList)"));
            method.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("list"), "Contains",
                new CodeVariableReferenceExpression("candidate"))));
            return method;
        }

        private static CodeTypeMember CreateEnableCandidateInteraction()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.EnableCandidateMethodName,
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.AddRequestParam();
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "interactionName"));

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "candidateList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string[]>", new CodePrimitiveExpression(CandidateVariableName))));
            method.Statements.Add(new CodeSnippetExpression("var list = candidateList == null ? new List<string>() : new List<string>(candidateList)"));

            method.Statements.Add(new CodeConditionStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("list"),"Contains",new CodeVariableReferenceExpression("interactionName")),new CodeMethodReturnStatement()));

            method.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("list"), "Add",
                new CodeVariableReferenceExpression("interactionName")));
            method.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"),
                "SetValue",
                new CodePrimitiveExpression(CandidateVariableName),
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("list"), "ToArray")));
            return method;
        }

        private const string SceneListItemName = "_scenes";
        private const string CandidateVariableName = "_candidates";

        private static CodeTypeMember CreateClearCandidateInteractions()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.ClearCandidateMethodName,
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };
            method.AddRequestParam();
            method.Statements.Add(new CodeMethodInvokeExpression(
                CodeConstants.RequestVariableRef,
                "Clear",
                new CodePrimitiveExpression(CandidateVariableName)));
            return method;
        }

        private static CodeTypeMember CreateResume()
        {
            var method = new CodeMemberMethod
            {
                Name = "Resume",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("Task")
            };
            method.AddFlowParams();
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "ignoreScene = false"));

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "sceneList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string[]>", new CodePrimitiveExpression(SceneListItemName))));

            var startCheck = new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression("sceneList"),
                    CodeBinaryOperatorType.ValueEquality,
                    new CodePrimitiveExpression(null)));
            startCheck.TrueStatements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Navigation"),
                "Interact",
                new CodePrimitiveExpression("start"),
                new CodePrimitiveExpression(CodeConstants.MainSceneMarker),
                CodeConstants.RequestVariableRef)));
            method.Statements.Add(startCheck);

            var resumeCheck = new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression("!ignoreScene"),
                    CodeBinaryOperatorType.BooleanAnd,
                    new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_scenes"), "ContainsKey", new CodePrimitiveExpression("resume"))));

            resumeCheck.TrueStatements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Navigation"),
                "Interact",
                new CodePrimitiveExpression("resume"),
                new CodePrimitiveExpression(CodeConstants.MainSceneMarker),
                CodeConstants.RequestVariableRef)));

            method.Statements.Add(resumeCheck);
            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "lastInteraction",
                new CodeMethodInvokeExpression(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("sceneList"), "Last"), "Split", new CodePrimitiveExpression('|'))));
            method.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Navigation"),
                "Interact",
                new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("lastInteraction"), new CodePrimitiveExpression(0)),
                new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("lastInteraction"), new CodePrimitiveExpression(1)),
                CodeConstants.RequestVariableRef)));


            return method;
        }

        private static CodeTypeMember CreateCurrentSceneMethod()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.CurrentSceneMethodName,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference(typeof(string))
            };
            method.AddRequestParam();

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "sceneList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string[]>", new CodePrimitiveExpression(SceneListItemName))));
            method.Statements.Add(new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression("sceneList"),
                    CodeBinaryOperatorType.ValueEquality,
                    new CodePrimitiveExpression(null)),
                new CodeStatement[]
                {
                    new CodeMethodReturnStatement(
                        new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(string)), "Empty"))
                },
                new CodeStatement[]
                {
                    new CodeVariableDeclarationStatement(typeof(string),"lastInteraction",new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("sceneList"),"Last")),
                    new CodeMethodReturnStatement(new CodeArrayIndexerExpression(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("lastInteraction"),"Split",new CodePrimitiveExpression('|')),new CodePrimitiveExpression(0)))
                }));

            return method;
        }

        private static CodeTypeMember CreateAddInteractionMethod()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.AddInteractionMethodName,
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.AddRequestParam();
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "sceneName"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "interactionName"));

            method.Statements.Add(new CodeConditionStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_notTracked"), "Contains", new CodeVariableReferenceExpression("sceneName")),
                new CodeMethodReturnStatement()));

            var mainMarkerCheck = new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression("interactionName"),
                    CodeBinaryOperatorType.ValueEquality,
                    new CodePrimitiveExpression(CodeConstants.MainSceneMarker)));

            mainMarkerCheck.TrueStatements.ClearAll("scene_");
            method.Statements.Add(mainMarkerCheck);

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "sceneList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string[]>", new CodePrimitiveExpression(SceneListItemName))));
            method.Statements.Add(new CodeSnippetExpression("var list = sceneList == null ? new List<string>() : new List<string>(sceneList)"));
            method.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("list"), "Add",
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)), "Concat",
                    new CodeVariableReferenceExpression("sceneName"),
                    new CodePrimitiveExpression("|"),
                    new CodeVariableReferenceExpression("interactionName"))));
            method.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"),
                "SetValue",
                new CodePrimitiveExpression("_scenes"),
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("list"), "ToArray")));
            return method;
        }

        private static CodeTypeMember CreateInteractLatestScene()
        {
            var gtMethod = new CodeMemberMethod
            {
                Name = CodeConstants.NavigationMethodName,
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                ReturnType = new CodeTypeReference("Task")
            };

            gtMethod.AddInteractionParams();
            gtMethod.AddFlowParams();

            var latestScene = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Navigation"),
                CodeConstants.CurrentSceneMethodName,
                new CodeVariableReferenceExpression("request"));

            gtMethod.Statements.Add(
                new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Navigation"),
            "Interact",
            latestScene, new CodeVariableReferenceExpression("interaction"), CodeConstants.RequestVariableRef)));

            return gtMethod;
        }

        private static CodeTypeMember CreateInteract()
        {
            var gtMethod = new CodeMemberMethod
            {
                Name = CodeConstants.NavigationMethodName,
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                ReturnType = new CodeTypeReference("Task")
            };

            gtMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sceneName"));
            gtMethod.AddInteractionParams();
            gtMethod.AddFlowParams();

            gtMethod.Statements.Add(AddInteraction(new CodeVariableReferenceExpression("sceneName"), new CodeVariableReferenceExpression("interaction")));
            gtMethod.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("_scenes"),
                    new CodeVariableReferenceExpression("sceneName")), "Invoke",
                new CodeVariableReferenceExpression(CodeConstants.InteractionParameterName),
                CodeConstants.RequestVariableRef)));

            return gtMethod;
        }

        private static CodeMemberField CreateLookup()
        {
            var lookupType = typeof(Dictionary<string, Func<string, AlexaRequestInformation<APLSkillRequest>, Task>>);
            return new CodeMemberField(lookupType, "_scenes")
            {
                Attributes = MemberAttributes.Static,
                InitExpression = new CodeObjectCreateExpression(lookupType)
            };
        }

        private static CodeTypeMember CreateStaticConstructor()
        {
            var constructor = new CodeTypeConstructor();
            _sceneRegistration = constructor.Statements;
            return constructor;
        }

        public static CodeMethodInvokeExpression EnableCandidate(string interactionName)
        {
            return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Navigation"),
                CodeConstants.EnableCandidateMethodName,
                CodeConstants.RequestVariableRef,
                new CodePrimitiveExpression(interactionName));
        }
    }
}
