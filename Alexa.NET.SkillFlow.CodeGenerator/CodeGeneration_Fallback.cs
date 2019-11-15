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
            var type = context.CreateIntentRequestHandler(BuiltInIntent.Fallback,false).FirstType();
            
            var handle = type.HandleStatements();
            var returnStmt = handle.OfType<CodeMethodReturnStatement>().First();
            if (!(returnStmt.Expression is CodeMethodInvokeExpression))
            {
                handle.Remove(returnStmt);
                handle.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("await " + BuiltInIntent.Fallback.Safe()),
                    "Fallback",
                    new CodeVariableReferenceExpression("information"),
                    new CodeVariableReferenceExpression("response")
                )));
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

                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(new CodeTypeReference("AlexaRequestInformation<Alexa.NET.Request.APLSkillRequest>"),
                        "information"));
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("SkillResponse"),"response"));
                type.Members.Add(method);
                statements = method.Statements;

                statements.Add(new CodeSnippetStatement("switch(information.State.Get<string>(\"_marker\")){"));
                statements.Add(new CodeSnippetStatement("}"));
                statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("response")));
            }

            return type;
        }

        public static void AddToFallback(CodeGeneratorContext context, IEnumerable<CodeStatement> statements)
        {
            var compileUnit = Ensure(context).HandleStatements();

            //TODO: Existing comment with marker in it - we're replacing at this point

            compileUnit.Add(new CodeCommentStatement(context.Marker));
            var ifMarker = new CodeConditionStatement();
            foreach (var statement in statements)
            {
                ifMarker.TrueStatements.Add(statement);
            }
            //TODO: Clean up all the comments
        }
    }
}
