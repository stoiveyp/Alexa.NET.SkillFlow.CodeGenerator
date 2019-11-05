using System.CodeDom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Alexa.NET.Response.APL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Visuals
    {
        public static void GenerateAplCall(CodeGeneratorContext context, string layout)
        {
            EnsureAPLHelper(context);
            AddLayout(context,layout);
        }

        private static void AddLayout(CodeGeneratorContext context, string layout)
        {
            if (!context.OtherFiles.ContainsKey("apldocuments.json"))
            {
                context.OtherFiles.Add("apldocuments.json", new JObject());
            }

            var layouts = context.OtherFiles["apldocuments.json"] as JObject;

            if (layouts.ContainsKey(layout))
            {
                return;
            }

            layouts.Add(layout,new JObject());
        }


        private static void EnsureAPLHelper(CodeGeneratorContext context)
        {
            if (context.CodeFiles.ContainsKey("APLHelper"))
            {
                return;
            }

            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response.APL"));
            ns.Imports.Add(new CodeNamespaceImport("System.IO"));
            ns.Imports.Add(new CodeNamespaceImport("Newtonsoft.Json"));
            code.Namespaces.Add(ns);

            var randomiserClass = CodeGeneration_Visuals.GenerateHelper();
            ns.Types.Add(randomiserClass);

            context.CodeFiles.Add("APLHelper", code);
        }

        private static CodeTypeDeclaration GenerateHelper()
        {
            var classCode = new CodeTypeDeclaration("APLHelper")
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Public
            };


            classCode.Members.Add(LayoutContainer());
            classCode.Members.Add(GenerateConstructor());
            classCode.Members.Add(GenerateGetLayout());

            return classCode;
        }

        private static CodeTypeMember LayoutContainer()
        {
            return new CodeMemberField(typeof(Dictionary<string, Layout>), "_apl")
            {
                Attributes = MemberAttributes.Static,
                InitExpression = new CodeObjectCreateExpression(typeof(Dictionary<string,Layout>))
            };
        }

        private static CodeTypeMember GenerateConstructor()
        {
            var constructor = new CodeConstructor
            {
                Attributes = MemberAttributes.Static,
                Name = "APLHelper"
            };

            constructor.Statements.Add(new CodeSnippetStatement(
                "using (var reader = new JsonTextReader(new StreamReader(File.OpenRead(\"apldocuments.json\"))))"));
            constructor.Statements.Add(new CodeSnippetStatement("{"));

            var deserialize = new CodeMethodInvokeExpression(
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(JsonSerializer)),"Create"),
                "Deserialize",new CodeVariableReferenceExpression("reader"));
            constructor.Statements.Add(deserialize);

            constructor.Statements.Add(new CodeSnippetStatement("}"));

            return constructor;
        }

        private static CodeTypeMember GenerateGetLayout()
        {
            var getLayout = new CodeMemberMethod
            {
                Name = "GetLayout",
                Attributes = MemberAttributes.Public,
                ReturnType = new CodeTypeReference(typeof(Layout)),
            };
            getLayout.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));

            getLayout.Statements.Add(new CodeMethodReturnStatement(new CodeIndexerExpression(new CodeVariableReferenceExpression("_apl"),
                new CodeVariableReferenceExpression("name"))));

            return getLayout;
        }
    }
}