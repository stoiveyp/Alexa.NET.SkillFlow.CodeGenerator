using System.CodeDom;
using System.Runtime.CompilerServices;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Visuals
    {
        public static void GenerateAplCall(CodeGeneratorContext context)
        {
            Ensure(context);

        }

        private static void Ensure(CodeGeneratorContext context)
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



            return classCode;
        }
    }
}