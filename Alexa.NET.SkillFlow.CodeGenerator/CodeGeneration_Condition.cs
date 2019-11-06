using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using Alexa.NET.SkillFlow.Conditions;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Condition
    {
        public static CodeExpression Generate(Value condition)
        {
            switch (condition)
            {
                case Variable variable:
                    return new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request.State"),"Get",new CodePrimitiveExpression(variable.Name));
                case LiteralValue literal:
                    return new CodePrimitiveExpression(literal.Value);
                case Equal equal:
                    return Binary(equal,CodeBinaryOperatorType.ValueEquality);
                case GreaterThan gt:
                    return Binary(gt, CodeBinaryOperatorType.GreaterThan);
                case LessThan lt:
                    return Binary(lt, CodeBinaryOperatorType.LessThan);
                case GreaterThanEqual gte:
                    return Binary(gte, CodeBinaryOperatorType.GreaterThanOrEqual);
                case LessThanEqual lte:
                    return Binary(lte, CodeBinaryOperatorType.LessThanOrEqual);
            }
            return new CodePrimitiveExpression("Unable to convert " + condition.GetType().Name);
        }

        private static CodeBinaryOperatorExpression Binary(BinaryCondition condition, CodeBinaryOperatorType type)
        {
            return new CodeBinaryOperatorExpression(
                Generate(condition.Left),
                type,
                Generate(condition.Right));
        }
    }
}
