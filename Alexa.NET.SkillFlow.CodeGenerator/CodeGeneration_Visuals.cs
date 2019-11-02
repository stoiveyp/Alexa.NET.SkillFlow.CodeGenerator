using System.CodeDom;
using System.Runtime.CompilerServices;
using Alexa.NET.Response.APL;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Visuals
    {
        public static void GenerateAplCall(CodeGeneratorContext context, string layout)
        {
            EnsureAPLHelper(context);
            AddBlankLayout(context,layout);
        }

        private static void AddBlankLayout(CodeGeneratorContext context, string layout)
        {
            throw new System.NotImplementedException();
        }


        private static void EnsureAPLHelper(CodeGeneratorContext context)
        {
            if (context.CodeFiles.ContainsKey("APLHelper"))
            {
                return;
            }

            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.APL"));
            code.Namespaces.Add(ns);

            var randomiserClass = CodeGeneration_Visuals.GenerateHelper();
            ns.Types.Add(randomiserClass);

            context.CodeFiles.Add("APLHelper", code);
        }

        private static CodeTypeDeclaration GenerateHelper()
        {
            var classCode = new CodeTypeDeclaration("APLHelper");

            var getLayout = new CodeMemberMethod
            {
                Name = "GetLayout",
                Attributes = MemberAttributes.Public,
                ReturnType = new CodeTypeReference(typeof(APLDocument))
            };
            getLayout.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));

            classCode.Members.Add(getLayout);
            return classCode;
        }
    }
}