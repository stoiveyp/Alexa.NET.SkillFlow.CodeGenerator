using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Output
    {
        public static void Ensure(CodeGeneratorContext context)
        {
            if (context.OtherFiles.ContainsKey("Output.cs"))
            {
                return;
            }

            context.OtherFiles.Add("Output.cs", CreateOutputFile(context, CreateOutputClass()));
        }

        private static CodeTypeDeclaration CreateOutputClass()
        {
            var type = new CodeTypeDeclaration
            {
                Name = "Output",
                Attributes = MemberAttributes.Public
            };


            type.StartDirectives.Add(new CodeRegionDirective(
                CodeRegionMode.Start, Environment.NewLine + "\tstatic"));

            type.EndDirectives.Add(new CodeRegionDirective(
                CodeRegionMode.End, string.Empty));


            type.Members.Add(CreateGenerateMethod());
            type.Members.Add(SetTemplate());
            type.Members.Add(SetDataProperty());
            //type.Members.Add(CreateSetAPL());

            return type;
        }

        private static CodeMemberMethod SetDataProperty()
        {
            var method = new CodeMemberMethod
            {
                Name = "SetTemplate",
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    CodeConstants.RequestVariableName));
            method.Parameters.Add(new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(string)), "templateName"));

            method.Statements.Add(CodeGeneration_Instructions.SetVariable("scene_template", new CodeVariableReferenceExpression("templateName"), false));

            return method;
        }

        private static CodeMemberMethod SetTemplate()
        {
            var method = new CodeMemberMethod
            {
                Name = "SetTemplate",
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    CodeConstants.RequestVariableName));
            method.Parameters.Add(new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(string)), "templateName"));

            method.Statements.Add(CodeGeneration_Instructions.SetVariable("scene_template",new CodeVariableReferenceExpression("templateName"), false));

            return method;
        }

        private static CodeMemberMethod CreateGenerateMethod()
        {
            var method = new CodeMemberMethod
            {
                Name = CodeConstants.OutputGenerateMethod,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("async Task<SkillResponse>")
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    CodeConstants.RequestVariableName));

            method.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference("var"),
                    CodeConstants.ResponseVariableName,
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(ResponseBuilder)),
                        "Ask",
                        new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(string)), "Empty"),
                        new CodePrimitiveExpression(null))
                )
            );

            method.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("response")));

            return method;
        }

        private static CodeCompileUnit CreateOutputFile(CodeGeneratorContext context, CodeTypeDeclaration navigationClass)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("System"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            ns.Imports.Add(new CodeNamespaceImport("System.Linq"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            ns.Types.Add(navigationClass);
            return code;
        }
    }
}
