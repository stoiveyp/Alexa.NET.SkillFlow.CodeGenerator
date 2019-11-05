using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Scene
    {
        public static CodeCompileUnit Generate(Scene scene, CodeGeneratorContext context)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.APL.DataSources"));
            code.Namespaces.Add(ns);

            var mainClass = GenerateSceneClass(scene);
            ns.Types.Add(mainClass);
            return code;
        }

        private static CodeTypeDeclaration GenerateSceneClass(Scene scene)
        {
            var mainClass = new CodeTypeDeclaration(SceneClassName(scene.Name))
            {
                IsClass = true
            };

            var method = new CodeMemberMethod
            {
                Name = "Generate",
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<APLSkillRequest>"), "request"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SkillResponse).AsSimpleName(), "responseBody"));

            var assignment = new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "response", new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("responseBody"), "Response"));
            method.Statements.Add(assignment);

            var throwStatement = new CodeThrowExceptionStatement(new CodeObjectCreateExpression(typeof(NotImplementedException)));
            method.Statements.Add(throwStatement);

            mainClass.Members.Add(method);
            return mainClass;
        }

        public static string SceneClassName(string sceneName)
        {
            return "Scene_" + char.ToUpper(sceneName[0]) + sceneName.Substring(1).Replace(" ", "_");
        }
    }
}
