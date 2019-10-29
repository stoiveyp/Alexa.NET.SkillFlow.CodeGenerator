using System.CodeDom;
using System.Security.Cryptography;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Randomiser
    {
        public static CodeTypeDeclaration Generate()
        {
            var classDef = new CodeTypeDeclaration("Randomiser");

            var rndGenerator = new CodeMemberField(typeof(RandomNumberGenerator).AsSimpleName(), "_generator")
            {
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                InitExpression = new CodeMethodInvokeExpression(typeof(RandomNumberGenerator).AsSimpleExpression(), "Create")
            };

            classDef.Members.Add(rndGenerator);


            //public static string PickRandom(params string[] options)
            //{
            //    return options[Randomiser.Next(0, options.Length - 1)];
            //}

            return classDef;
        }

        public static void Ensure(CodeGeneratorContext context)
        {
            if (context.CodeFiles.ContainsKey("Randomiser"))
            {
                return;
            }

            var code = new CodeCompileUnit();
            var ns = new CodeNamespace("SkillFlow");
            ns.Imports.Add(new CodeNamespaceImport("System.Security.Cryptography"));
            code.Namespaces.Add(ns);

            var randomiserClass = CodeGeneration_Randomiser.Generate();
            ns.Types.Add(randomiserClass);

            context.CodeFiles.Add("Randomiser", code);
        }
    }
}
