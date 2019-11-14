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

            var statements = type.HandleStatements();
            if (statements.OfType<CodeSnippetStatement>().Any())
            {
                return type;
            }

            statements.Add(new CodeSnippetStatement("switch(information.State.Get<string>(\"_marker\")){"));
            statements.Add(new CodeSnippetStatement("}"));

            return type;
        }

        public static void AddToFallback(CodeGeneratorContext context)
        {
            var compileUnit = Ensure(context).HandleStatements();
            

        }
    }
}
