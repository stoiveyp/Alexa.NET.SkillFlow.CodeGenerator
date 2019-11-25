using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexa.NET.Request.Type;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Fallback
    {
        public static CodeTypeDeclaration Ensure(CodeGeneratorContext context)
        {
            var type = context.CreateIntentRequestHandler(BuiltInIntent.Fallback, false).FirstType();

            var handle = type.MethodStatements(CodeConstants.HandlerPrimaryMethod);
            var returnStmt = handle.OfType<CodeMethodReturnStatement>().First();
            if (!(returnStmt.Expression is CodeMethodInvokeExpression))
            {
                handle.Remove(returnStmt);
                handle.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("await " + BuiltInIntent.Fallback.Safe()),
                    "Fallback"
                ).AddFlowParameters()));
            }

            var statements = type.MethodStatements("Fallback");

            if (statements == null)
            {
                var method = new CodeMemberMethod
                {
                    Name = "Fallback",
                    Attributes = MemberAttributes.Public | MemberAttributes.Static,
                    ReturnType = new CodeTypeReference("async Task<SkillResponse>")
                };

                method.AddFlowParams();
                type.Members.Add(method);
                statements = method.Statements;

                statements.Add(CodeGeneration_Navigation.InvokeInteraction(CodeConstants.FallbackMarker));
                statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("response")));
            }

            return type;
        }

        public static void AddToFallback(CodeGeneratorContext context, CodeMethodInvokeExpression methodInvoke)
        {
            //Fix this to add default to navigation marker
            var methodStmt = Ensure(context).MethodStatements("Fallback");

            //var label = context.GenerateMarker(1);
            //var currentMarkerCase = $"case \"{label}\":";


            //var caseStmt = methodStmt.OfType<CodeSnippetStatement>().FirstOrDefault(ccs => ccs.Value == currentMarkerCase);
            //if (caseStmt == null)
            //{
            //    caseStmt = new CodeSnippetStatement(currentMarkerCase);
            //    methodStmt.Insert(1, caseStmt);
            //    methodStmt.Insert(2, new CodeSnippetStatement("break;"));
            //}

            //var stmtPos = methodStmt.IndexOf(caseStmt);
            //while (!(methodStmt[stmtPos + 1] is CodeSnippetStatement snippet) || snippet.Value != "break;")
            //{
            //    methodStmt.RemoveAt(stmtPos + 1);
            //}

            //var varName = "var_" + label.ToLower();
            //methodStmt.Insert(stmtPos + 1, new CodeVariableDeclarationStatement(
            //    new CodeTypeReference("var"),
            //    varName, methodInvoke));
            //methodStmt.Insert(stmtPos + 2, new CodeSnippetStatement($"await {varName};"));
        }
    }
}
