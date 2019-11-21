using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using Alexa.NET.Request;
using Alexa.NET.RequestHandlers;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Navigation
    {
        public static void EnsureNavigation(CodeGeneratorContext context)
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
            ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            ns.Imports.Add(new CodeNamespaceImport("System.Linq"));
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

            var lookupType = new CodeTypeReference("Dictionary<string, Func<AlexaRequestInformation<APLSkillRequest>, SkillResponse, Task>>");
            var lookup = new CodeMemberField(lookupType, "_scenes")
            {
                Attributes = MemberAttributes.Static,
                InitExpression = new CodeObjectCreateExpression(lookupType)
            };
            type.Members.Add(lookup);
            type.Members.Add(CreateStaticConstructor());
            
            return type;
        }

        private static CodeStatementCollection _sceneRegistration;

        public static void RegisterScene(CodeGeneratorContext context, string sceneName, CodeMethodReferenceExpression runScene)
        {
            EnsureNavigation(context);
            _sceneRegistration.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_scenes"),"Add",
                new CodePrimitiveExpression(sceneName),runScene));
        }


        private static CodeTypeMember CreateStaticConstructor()
        {
            var constructor = new CodeTypeConstructor();
            _sceneRegistration = constructor.Statements;
            return constructor;
        }
    }
}
