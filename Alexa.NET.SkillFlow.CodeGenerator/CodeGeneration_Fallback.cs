using System.CodeDom;
using System.Linq;
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

            return type;
        }
    }
}
