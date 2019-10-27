using System;
using System.CodeDom;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.RequestHandlers;
using Alexa.NET.Response;
using Alexa.NET.SkillFlow.Generator;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGenerator : SkillFlowGenerator<CodeGeneratorContext>
    {
        protected override Task Begin(Scene scene, CodeGeneratorContext context)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace("SkillFlow");
            code.Namespaces.Add(ns);

            var mainClass = GenerateSceneClass(scene);
            ns.Types.Add(mainClass);

            context.CodeFiles.Add(SceneClassName(scene.Name), code);
            context.CurrentClass = code;
            return base.Begin(scene, context);
        }

        private CodeTypeDeclaration GenerateSceneClass(Scene scene)
        {
            var mainClass = new CodeTypeDeclaration(SceneClassName(scene.Name))
            {
                IsClass = true
            };

            var method = new CodeMemberMethod
            {
                Name = "Generate",
                ReturnType = new CodeTypeReference(typeof(SkillResponse))
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(typeof(AlexaRequestInformation<APLSkillRequest>),"request"));

            var throwStatement = new CodeThrowExceptionStatement(new CodeObjectCreateExpression(typeof(NotImplementedException)));
            method.Statements.Add(throwStatement);

            mainClass.Members.Add(method);
            return mainClass;
        }


        private string SceneClassName(string sceneName)
        {
            return "Scene_" + char.ToUpper(sceneName[0]) + sceneName.Substring(1).Replace(" ", "_");
        }
    }
}
