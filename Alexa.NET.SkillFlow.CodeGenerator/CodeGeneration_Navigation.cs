using System;
using System.CodeDom;
using System.Collections.Generic;
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
                new CodeVariableReferenceExpression("request"),
                new CodeVariableReferenceExpression("response"));
        }

        public static CodeMethodInvokeExpression InvokeInteraction(string interactionName)
        {
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await Navigation"),
                CodeConstants.NavigationMethodName,
                new CodePrimitiveExpression(interactionName),
                new CodeVariableReferenceExpression("request"),
                new CodeVariableReferenceExpression("response"));
        }

        public static CodeMethodInvokeExpression AddInteraction(string sceneName, string interactionName)
        {
            var methodInvoke = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Navigation"),
                CodeConstants.AddInteractionMethodName,
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                new CodePrimitiveExpression(sceneName),
                new CodePrimitiveExpression(interactionName));

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

            
            type.Members.Add(CreateLookup());
            type.Members.Add(CreateStaticConstructor());
            type.Members.Add(CreateInteractLatestScene());
            type.Members.Add(CreateInteract());
            type.Members.Add(CreateLogInteractionMethod());
            type.Members.Add(CreateCurrentSceneMethod());
            type.Members.Add(CreateResume());
            type.Members.Add(CreateEnableCandidateInteraction());
            type.Members.Add(CreateIsEnabledCandidateInteractions());
            type.Members.Add(CreateClearCandidateInteractions());

            return type;
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
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
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

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "sceneList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string[]>", new CodePrimitiveExpression(SceneListItemName))));

            var startCheck = new CodeConditionStatement(
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression("sceneList"),
                    CodeBinaryOperatorType.ValueEquality,
                    new CodePrimitiveExpression(null)));
            startCheck.TrueStatements.Add(AddInteraction("start", CodeConstants.MainSceneMarker));
            startCheck.TrueStatements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Navigation"),
                "Interact",
                new CodePrimitiveExpression("start"),
                new CodePrimitiveExpression(CodeConstants.MainSceneMarker),
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                new CodeVariableReferenceExpression(CodeConstants.ResponseVariableName))));

            startCheck.FalseStatements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "lastInteraction",
                new CodeMethodInvokeExpression(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("sceneList"), "Last"),"Split",new CodePrimitiveExpression('|'))));
            startCheck.FalseStatements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Navigation"),
                "Interact",
                new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("lastInteraction"),new CodePrimitiveExpression(0)),
                new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("lastInteraction"), new CodePrimitiveExpression(1)),
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                new CodeVariableReferenceExpression(CodeConstants.ResponseVariableName))));

            method.Statements.Add(startCheck);
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

        private static CodeTypeMember CreateLogInteractionMethod()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.AddInteractionMethodName,
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.AddRequestParam();
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "sceneName"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "interactionName"));

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string[]), "sceneList",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"),"GetValue<string[]>",new CodePrimitiveExpression(SceneListItemName))));
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

            gtMethod.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("_scenes"),
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Navigation"), CodeConstants.CurrentSceneMethodName, new CodeVariableReferenceExpression("request"))), "Invoke",
                new CodeVariableReferenceExpression(CodeConstants.InteractionParameterName),
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                new CodeVariableReferenceExpression(CodeConstants.ResponseVariableName))));

            return gtMethod;
        }

        private static CodeTypeMember CreateInteract()
        {
            var gtMethod = new CodeMemberMethod
            {
                Name=CodeConstants.NavigationMethodName,
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                ReturnType = new CodeTypeReference("Task")
            };

            gtMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sceneName"));
            gtMethod.AddInteractionParams();
            gtMethod.AddFlowParams();

            gtMethod.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("_scenes"),
                    new CodeVariableReferenceExpression("sceneName")), "Invoke",
                new CodeVariableReferenceExpression(CodeConstants.InteractionParameterName),
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName), 
                new CodeVariableReferenceExpression(CodeConstants.ResponseVariableName))));

            return gtMethod;
        }

        private static CodeMemberField CreateLookup()
        {
            var lookupType = typeof(Dictionary<string, Func<string, AlexaRequestInformation<APLSkillRequest>, SkillResponse, Task>>);
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
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                new CodePrimitiveExpression(interactionName));
        }
    }
}
