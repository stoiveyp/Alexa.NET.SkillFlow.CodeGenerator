using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Instructions
    {
        public static void SetMarker(this CodeGeneratorContext context, CodeStatementCollection statements, int skip = 0)
        {
            SetVariable(statements, "_marker", context.GenerateMarker(skip));
        }

        public static void SetVariable(CodeStatementCollection statements, string variableName, object value)
        {
            var setVariable = new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression("request"), "State"), "SetSession",
                new CodePrimitiveExpression(variableName),
                new CodePrimitiveExpression(value));

            statements.Add(setVariable);
        }
    }
}
