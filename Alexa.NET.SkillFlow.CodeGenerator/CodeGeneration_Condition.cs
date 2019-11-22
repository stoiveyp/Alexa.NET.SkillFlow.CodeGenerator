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
                    var typeArg = ComparisonType(comparisonType);
                    return CodeGeneration_Instructions.GetVariable(variable.Name, typeArg);
                case LiteralValue literal:
                    return new CodePrimitiveExpression(literal.Value);
                case ValueWrapper wrapper:
                    return Generate(wrapper.Value);
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

        private static Type ComparisonType(Value comparisonType)
        {
            switch (comparisonType)
            {
                case LiteralValue lit:
                    return lit.Value.GetType();
                default:
                    return typeof(bool);
            }
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
