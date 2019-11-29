using System.CodeDom;
using System.Linq;
using Alexa.NET.Request.Type;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Fallback
    {
        public static CodeTypeDeclaration Ensure(CodeGeneratorContext context)
        {
            var type = context.CreateIntentRequestHandler(BuiltInIntent.Fallback).FirstType();
            var handle = type.MethodStatements(CodeConstants.HandlerPrimaryMethod);
            CodeGeneration_Interaction.AddHandlerCheck(handle, context,CodeConstants.FallbackMarker);
            var returnStmt = handle.OfType<CodeMethodReturnStatement>().First();
            if (!(returnStmt.Expression is CodeMethodInvokeExpression))
            {
                handle.Remove(returnStmt);
                handle.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("await " + BuiltInIntent.Fallback.Safe()),
                    "Fallback"
                ).AddFlowParameters()));
            }

            var invoke = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await Navigation"),
                CodeConstants.NavigationMethodName);
            invoke.AddFlowParameters();

            return type;
        }
    }
}
