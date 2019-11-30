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

            type.Members.Add(CreateSetTemplate());
            type.Members.Add(CreateSetDataProperty());
            type.Members.Add(CreateAddSpeech());
            type.Members.Add(CreateGenerateMethod());
            type.Members.Add(CreateGenerateSpeech());
            type.Members.Add(CreateOutputSpeech());
            type.Members.Add(CreateAttachApl());
            type.Members.Add(CreateSetVisualProperty());
            type.Members.Add(CreateFallback());

            return type;
        }

        private static CodeMemberMethod CreateFallback()
        {
            var method = new CodeMemberMethod
            {
                Name = "Fallback",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = CodeConstants.AsyncTask
            };
            method.AddRequestParam();

            method.Statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "recap",
                CodeGeneration_Instructions.GetVariable("scene_recap",typeof(string),false)));

            method.Statements.Add(new CodeConditionStatement(
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)),"IsNullOrWhiteSpace",new CodeVariableReferenceExpression("recap")),
                new CodeVariableDeclarationStatement(CodeConstants.Var,"lastSpeech", CodeGeneration_Instructions.GetVariable("scene_lastSpeech", typeof(string), false)),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("Output"), "AddSpeech",new CodeVariableReferenceExpression(CodeConstants.RequestVariableName), new CodeVariableReferenceExpression("lastSpeech")))));

            method.Statements.Add(new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Output"),
                "AddSpeech",
                new CodeVariableReferenceExpression(CodeConstants.RequestVariableName), new CodeVariableReferenceExpression("recap")));

            return method;
        }

        private static CodeMemberMethod CreateSetVisualProperty()
        {
            var method = new CodeMemberMethod
            {
                Name = "SetVisualProperty",
                Attributes = MemberAttributes.Static
            };

            method.AddRequestParam();
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference("KeyValueDataSource"), "ds"));
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(string)), "name"));

            method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), "property",
                new CodeMethodInvokeExpression(CodeConstants.RequestVariableRef, "GetValue<string>",
                    new CodeVariableReferenceExpression("name"))));

            method.Statements.Add(new CodeConditionStatement(
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)),"IsNullOrWhiteSpace",new CodeVariableReferenceExpression("property")),
                new CodeMethodReturnStatement()));

            //ds.Properties.Add(name.Substring(6),property);
            method.Statements.Add(new CodeMethodInvokeExpression(
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("ds"),"Properties")
                ,"Add",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("name"),"Substring",new CodePrimitiveExpression(6)),new CodeVariableReferenceExpression("property")));
            return method;
        }

        private static CodeMemberMethod CreateAttachApl()
        {
            var method = new CodeMemberMethod
            {
                Name = "AttachApl",
                Attributes = MemberAttributes.Static
            };

            method.AddRequestParam();
            method.AddResponseParam();

            method.Statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "templateName",
                new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<string>",
                    new CodePrimitiveExpression("scene_template"))));

            var templateName = new CodeVariableReferenceExpression("templateName");

            method.Statements.Add(new CodeConditionStatement(
                new CodeBinaryOperatorExpression(templateName, CodeBinaryOperatorType.ValueEquality,new CodePrimitiveExpression(null)),
                new CodeMethodReturnStatement()));

            method.Statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "directive",
                new CodeObjectCreateExpression(new CodeTypeReference("RenderDocumentDirective"))));

            var aplDocProperty = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("directive"), "Document");
            //TODO: Add these statements here
            method.Statements.Add(new CodeAssignStatement(
                aplDocProperty,
                new CodeObjectCreateExpression(new CodeTypeReference("APLDocument"))));

            method.Statements.Add(new CodeAssignStatement(
                    new CodePropertyReferenceExpression(aplDocProperty, "MainTemplate"),
                    new CodeMethodInvokeExpression(
                        new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression("APLHelper"),
                            "GetLayout",
                            new CodeVariableReferenceExpression("templateName")),"AsMain")
                ));

            method.Statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "ds",
                new CodeObjectCreateExpression(new CodeTypeReference("KeyValueDataSource"))));

            var dsVar = new CodeVariableReferenceExpression("ds");
            var requestVar = new CodeVariableReferenceExpression(CodeConstants.RequestVariableName);
            method.Statements.Add(new CodeMethodInvokeExpression(_refOutput, "SetVisualProperty",
                CodeConstants.RequestVariableRef, dsVar,new CodePrimitiveExpression("scene_background")));
            method.Statements.Add(new CodeMethodInvokeExpression(_refOutput, "SetVisualProperty",
                CodeConstants.RequestVariableRef, dsVar, new CodePrimitiveExpression("scene_title")));
            method.Statements.Add(new CodeMethodInvokeExpression(_refOutput, "SetVisualProperty",
                CodeConstants.RequestVariableRef, dsVar, new CodePrimitiveExpression("scene_subtitle")));

            method.Statements.Add(new CodeMethodInvokeExpression(
                new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("directive"), "DataSources"),
                "Add", new CodePrimitiveExpression("visualProperty"), new CodeVariableReferenceExpression("ds")));

            method.Statements.Add(new CodeMethodInvokeExpression(
                new CodePropertyReferenceExpression(
                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(CodeConstants.ResponseVariableName),"Response"), "Directives"), "Add",
                new CodeVariableReferenceExpression("directive")));
            return method;
        }

        private static readonly CodeTypeReferenceExpression _refOutput = new CodeTypeReferenceExpression("Output");

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
                    new CodeMethodInvokeExpression(_refOutput, "GenerateSpeech", new CodeVariableReferenceExpression(CodeConstants.RequestVariableName))
                )
            );

            method.Statements.Add(new CodeConditionStatement(new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("request"),"SkillRequest"), "APLSupported"),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(
                    _refOutput,
                    "AttachApl",
                    new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),
                    new CodeVariableReferenceExpression(CodeConstants.ResponseVariableName)))));

            method.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("response")));

            return method;
        }

        private static CodeTypeMember CreateOutputSpeech()
        {
            var method = new CodeMemberMethod
            {
                Name = "CreateOutput",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("IOutputSpeech")
            };

            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "output"));

            method.Statements.Add(new CodeConditionStatement(new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("output"), CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(null)),
                new CodeMethodReturnStatement(new CodePrimitiveExpression(null))));

            var localOutput = new CodeVariableReferenceExpression("output");
            var ssmlCheck = new CodeConditionStatement(new CodeMethodInvokeExpression(
                localOutput, "Contains", new CodePrimitiveExpression("<")));

            ssmlCheck.TrueStatements.Add(new CodeAssignStatement(localOutput, new CodeMethodInvokeExpression(new CodeObjectCreateExpression(new CodeTypeReference("Speech"),new CodeObjectCreateExpression(new CodeTypeReference("PlainText"),new CodeVariableReferenceExpression("output"))),"ToXml") ));

            ssmlCheck.TrueStatements.Add(new CodeMethodReturnStatement(
                new CodeObjectCreateExpression(new CodeTypeReference("SsmlOutputSpeech"),
                    localOutput)));

            ssmlCheck.FalseStatements.Add(new CodeMethodReturnStatement(
                new CodeObjectCreateExpression(new CodeTypeReference("PlainTextOutputSpeech"),
                    localOutput)));

            method.Statements.Add(ssmlCheck);

            return method;
        }

        private static CodeMemberMethod CreateGenerateSpeech()
        {
            var method = new CodeMemberMethod
            {
                Name = "GenerateSpeech",
                Attributes = MemberAttributes.Static,
                ReturnType = new CodeTypeReference("SkillResponse")
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    CodeConstants.RequestVariableName));

            var items = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(CodeConstants.RequestVariableName), "Items");
            var speech = new CodePrimitiveExpression("speech");

            method.Statements.Add(
                new CodeVariableDeclarationStatement(
                    CodeConstants.Var,
                    "fullSpeech", 
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(typeof(string)),
                        "Concat",
                        new CodeMethodInvokeExpression(
                            new CodeCastExpression("List<string>", new CodeIndexerExpression(items, speech)),
                            "ToArray"))));

            method.Statements.Add(CodeGeneration_Instructions.SetVariable(
                new CodePrimitiveExpression("scene_lastSpeech"),new CodeVariableReferenceExpression("fullSpeech")));


            method.Statements.Add(new CodeVariableDeclarationStatement(
                new CodeTypeReference("var"),
                "speech",
                new CodeMethodInvokeExpression(_refOutput, "CreateOutput",new CodeVariableReferenceExpression("fullSpeech"))
            ));

            var checkForCandidates = new CodeConditionStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Navigation"),
                    "HasCandidates", new CodeVariableReferenceExpression(CodeConstants.RequestVariableName)));

            checkForCandidates.TrueStatements.Add(new CodeVariableDeclarationStatement(
                new CodeTypeReference("var"),
                "reprompt",
                new CodeMethodInvokeExpression(_refOutput,"CreateOutput",
                    new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression(CodeConstants.RequestVariableName),"GetValue<string>",
                        new CodePrimitiveExpression("scene_reprompt")))));
            checkForCandidates.TrueStatements.Add(new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("ResponseBuilder"), "Ask", new CodeVariableReferenceExpression("speech"),new CodeSnippetExpression("new Reprompt{OutputSpeech=reprompt == null ? speech : reprompt}"))));
            checkForCandidates.FalseStatements.Add(new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("ResponseBuilder"), "Tell", new CodeVariableReferenceExpression("speech"))));

            method.Statements.Add(checkForCandidates);

            return method;
        }

        private static CodeMemberMethod CreateAddSpeech()
        {
            var method = new CodeMemberMethod
            {
                Name = "AddSpeech",
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    CodeConstants.RequestVariableName));
            method.Parameters.Add(new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(string)), "speech"));

            var items = new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(CodeConstants.RequestVariableName), "Items");
            var speech = new CodePrimitiveExpression("speech");
            method.Statements.Add(new CodeConditionStatement(
                new CodeBinaryOperatorExpression(new CodeMethodInvokeExpression(
                    items, "ContainsKey", speech),
                CodeBinaryOperatorType.ValueEquality,
                new CodePrimitiveExpression(false)),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(
                    items,
                    "Add",
                    speech,
                    new CodeObjectCreateExpression(new CodeTypeReference("List<string>"))
                    ))
                ));

            method.Statements.Add(new CodeVariableDeclarationStatement(CodeConstants.Var, "list",
                new CodeCastExpression(new CodeTypeReference("List<string>"), new CodeIndexerExpression(items, speech))));

            method.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("list"), "Add",
                new CodeVariableReferenceExpression("speech")));

            return method;
        }

        private static CodeMemberMethod CreateSetDataProperty()
        {
            var method = new CodeMemberMethod
            {
                Name = "SetDataProperty",
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                    CodeConstants.RequestVariableName));
            method.Parameters.Add(new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(string)), "property"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(string)), "value"));

            method.Statements.Add(CodeGeneration_Instructions.SetVariable(
                new CodeBinaryOperatorExpression(new CodePrimitiveExpression("scene_"), CodeBinaryOperatorType.Add, new CodeVariableReferenceExpression("property")), new CodeVariableReferenceExpression("value")));

            return method;
        }

        private static CodeMemberMethod CreateSetTemplate()
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

        private static CodeCompileUnit CreateOutputFile(CodeGeneratorContext context, CodeTypeDeclaration navigationClass)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("System"));
            ns.Imports.Add(new CodeNamespaceImport("System.Linq"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.APL.DataSources"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response.APL"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response.Ssml"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            ns.Types.Add(navigationClass);
            return code;
        }
    }
}
