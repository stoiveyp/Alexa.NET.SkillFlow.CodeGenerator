using System.CodeDom;

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
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
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

        private static CodeTypeMember CreateMain()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.ScenePrimaryMethod,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("async Task")
            };

            method.AddFlowParams(true);
            return method;
        }

        private static CodeTypeMember CreateInteract(string className)
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.SceneInteractionMethod,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("async Task")
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

            statements.AddInteraction(CodeConstants.MainSceneMarker,invoke);

            return method;
        }
    }
}
