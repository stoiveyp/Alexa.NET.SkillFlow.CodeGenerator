using System.CodeDom;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Alexa.NET.Request;
using Alexa.NET.RequestHandlers;
using Newtonsoft.Json.Linq;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Story
    {
        private static XDocument CreateProjectOutline(bool includeLambda)
        {
            void AddItemGroup(XElement parent, string name, string version)
            {
                parent.Add(new XElement("PackageReference",
                    new XAttribute("Include", name),
                    new XAttribute("Version", version)));
            }

            var itemGroup = new XElement("ItemGroup");
            AddItemGroup(itemGroup, "Alexa.NET", "1.10.2");
            AddItemGroup(itemGroup, "Alexa.NET.APL", "4.6.0");
            AddItemGroup(itemGroup, "Alexa.NET.RequestHandlers", "4.1.1");

            if (includeLambda)
            {
                AddItemGroup(itemGroup, "Amazon.Lambda.Core", "1.1.0");
                AddItemGroup(itemGroup, "Amazon.Lambda.Serialization.Json", "1.6.0");
            }

            var doc = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", new XText("netcoreapp2.1")),
                        new XElement("GenerateRuntimeConfigurationFiles", new XText("true")),
                        new XElement("AWSProjectType", new XText("Lambda"))
                    ), itemGroup
                )
            );

            return doc;
        }

        public static void CreateStoryFiles(CodeGeneratorContext context)
        {
            CreateProjectFile(context);
            CreatePipelineCreation(context);
            if (context.Options.IncludeLambda)
            {
                CreateLambdaFunction(context);
                CreateLambdaJson(context);
            }
        }

        private static void CreateLambdaJson(CodeGeneratorContext context)
        {
            var content = new JObject
            {
                {"configuration", "Release"},
                {"framework", "netcoreapp2.1"},
                {"function-runtime", "dotnetcore2.1"},
                {"function-memory-size", 256},
                {"function-timeout", 30},
                {
                    "function-handler",
                    $"{context.Options.SafeSkillName}::{context.Options.SafeRootNamespace}.Function::FunctionHandler"
                },
                {"function-name", context.Options.SafeSkillName}
            };
            context.OtherFiles.Add("aws-lambda-tools-defaults.json", content);
        }

        private static void CreatePipelineCreation(CodeGeneratorContext context)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            var mainClass = GeneratePipelineClass();
            ns.Types.Add(mainClass);
            context.OtherFiles.Add("Pipeline.cs", code);
        }

        private static CodeTypeDeclaration GeneratePipelineClass()
        {
            var main = new CodeTypeDeclaration
            {
                Name = "Pipeline",
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
            };

            main.Members.Add(new CodeMemberField(new CodeTypeReference("AlexaRequestPipeline<APLSkillRequest>"), "_pipeline") { Attributes = MemberAttributes.Static });

            main.Members.Add(new CodeTypeConstructor());

            var processMethod = new CodeMemberMethod
            {
                Name = "Process",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("Task<SkillResponse>")
            };
            processMethod.Parameters.Add(new CodeParameterDeclarationExpression("APLSkillRequest", "request"));

            processMethod.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_pipeline"),
                "Process", CodeConstants.RequestVariableRef)));


            main.Members.Add(processMethod);

            return main;
        }

        private static void CreateLambdaFunction(CodeGeneratorContext context)
        {
            var code = new CodeCompileUnit();
            code.AssemblyCustomAttributes.Add(
                new CodeAttributeDeclaration(
                    new CodeTypeReference("LambdaSerializer"),new CodeAttributeArgument(
                        new CodeTypeOfExpression(new CodeTypeReference("Amazon.Lambda.Serialization.Json.JsonSerializer")))));
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);
            var global = new CodeNamespace();
            global.Imports.Add(new CodeNamespaceImport("Amazon.Lambda.Core"));
            code.Namespaces.Add(global);

            var mainClass = GenerateLambdaClass();
            ns.Types.Add(mainClass);
            context.OtherFiles.Add("Function.cs", code);
        }

        private static CodeTypeDeclaration GenerateLambdaClass()
        {
            var main = new CodeTypeDeclaration
            {
                Name = "Function",
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
            };

            var method = new CodeMemberMethod
            {
                Name = "FunctionHandler",
                Attributes = MemberAttributes.Public,
                ReturnType = new CodeTypeReference("Task<SkillResponse>")
            };
            method.Parameters.Add(new CodeParameterDeclarationExpression("APLSkillRequest", "input"));
            method.Parameters.Add(new CodeParameterDeclarationExpression("ILambdaContext", "_"));
            method.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Pipeline"), "Process", new CodeVariableReferenceExpression("input"))));

            main.Members.Add(method);

            return main;
        }

        public static void CreateProjectFile(CodeGeneratorContext context)
        {
            var output = CreateProjectOutline(context.Options.IncludeLambda);

            var storage = new MemoryStream();
            using (var newProject = new StreamWriter(storage, Encoding.UTF8, 1024, true))
            {
                using (var xml = XmlWriter.Create(newProject, new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Indent = true
                }))
                {
                    output.Save(xml);
                }
                newProject.Flush();
                newProject.Close();
            }
            context.OtherFiles.Add($"{context.Options.SafeSkillName}.csproj", storage);
        }
    }
}