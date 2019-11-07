using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using Alexa.NET.SkillFlow.Conditions;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Condition
    {
        public static CodeExpression Generate(Value condition, Value comparisonType = null)
        {
            switch (condition)
            {
                case Variable variable:
                    var typeArg = typeof(object);
                    if (comparisonType is LiteralValue lit)
                    {
                        typeArg = lit.Value.GetType();
                    }
                    var method = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("await request.State"),"Get",new CodePrimitiveExpression(variable.Name));
                    method.Method.TypeArguments.Add(typeArg);
                    return method;
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
                case Not not:
                    return new CodeBinaryOperatorExpression(
                        Generate(not.Condition),
                        CodeBinaryOperatorType.ValueEquality,
                        new CodePrimitiveExpression(false));
                case And and:
                    return Binary(and, CodeBinaryOperatorType.BooleanAnd);
                case Or or:
                    return Binary(or, CodeBinaryOperatorType.BooleanOr);

            }
            return new CodePrimitiveExpression("Unable to convert " + condition.GetType().Name);
        }

        private static CodeBinaryOperatorExpression Binary(BinaryCondition condition, CodeBinaryOperatorType type)
        {
            return new CodeBinaryOperatorExpression(
                Generate(condition.Left,condition.Right),
                type,
                Generate(condition.Right, condition.Left));
        }
    }
}
