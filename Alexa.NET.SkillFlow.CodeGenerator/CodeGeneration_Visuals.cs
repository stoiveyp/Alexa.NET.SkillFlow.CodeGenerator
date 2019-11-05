using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Alexa.NET.APL.DataSources;
using Alexa.NET.Response;
using Alexa.NET.Response.APL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Visuals
    {
        public static CodeMethodInvokeExpression GenerateAplCall(CodeGeneratorContext context, string layout)
        {
            EnsureAPLHelper(context);
            AddLayout(context, layout);
            return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("APLHelper"), "GetLayout", new CodePrimitiveExpression(layout));

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

            layouts.Add(layout, new JObject());
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
                InitExpression = new CodeObjectCreateExpression(typeof(Dictionary<string, Layout>))
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
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(JsonSerializer)), "Create"),
                "Deserialize", new CodeVariableReferenceExpression("reader"));
            constructor.Statements.Add(deserialize);

            constructor.Statements.Add(new CodeSnippetStatement("}"));

            return constructor;
        }

        private static CodeTypeMember GenerateGetLayout()
        {
            var getLayout = new CodeMemberMethod
            {
                Name = "GetLayout",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference(typeof(Layout)),
            };
            getLayout.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));

            getLayout.Statements.Add(new CodeMethodReturnStatement(new CodeIndexerExpression(new CodeVariableReferenceExpression("_apl"),
                new CodeVariableReferenceExpression("name"))));

            return getLayout;
        }

        public static CodeObject AddRenderDocument(CodeMemberMethod gen, string variableName)
        {
            gen.Statements.Add(new CodeVariableDeclarationStatement(typeof(RenderDocumentDirective), variableName,
                new CodeObjectCreateExpression(typeof(RenderDocumentDirective))));
            var aplRef = new CodeVariableReferenceExpression(variableName);
            gen.Statements.Add(new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("response"), "Directives"), "Add", aplRef));
            gen.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(aplRef, "Document"),
                new CodeObjectCreateExpression(typeof(APLDocument))));
            return aplRef;
        }

        public static CodeVariableReferenceExpression EnsureDataSource(CodeMemberMethod gen, string variableName)
        {
            var ds = gen.Statements.OfType<CodeVariableDeclarationStatement>()
                .FirstOrDefault(cv => cv.Name == "dataSource");

            if (ds != null)
            {
                return new CodeVariableReferenceExpression("dataSource");
            }

            var ifStmt = new CodeConditionStatement
            {
                Condition = new CodeBinaryOperatorExpression(
                    new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression(variableName), "DataSources.ContainsKey",
                        new CodePrimitiveExpression("visualProperties")),
                    CodeBinaryOperatorType.ValueEquality,
                    new CodePrimitiveExpression(false)
                ),
                TrueStatements = {
                    new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression(variableName),
                        "DataSources.Add",
                        new CodePrimitiveExpression("visualProperties"),
                        new CodeObjectCreateExpression(typeof(KeyValueDataSource)))
                    }
            };
            gen.Statements.Add(ifStmt);
            gen.Statements.Add(new CodeVariableDeclarationStatement(typeof(KeyValueDataSource), "dataSource",
                new CodeCastExpression(typeof(KeyValueDataSource),new CodeArrayIndexerExpression(
                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(variableName),
                        "DataSources"), new CodePrimitiveExpression("visualProperties")))));
            return new CodeVariableReferenceExpression("dataSource");
        }

        public static CodeExpression AddDataSourceProperty(CodeVariableReferenceExpression dataSource, string property, string value)
        {
            return new CodeMethodInvokeExpression(dataSource,"Properties.Add",new CodePrimitiveExpression(property),new CodePrimitiveExpression(value));
        }
    }
}