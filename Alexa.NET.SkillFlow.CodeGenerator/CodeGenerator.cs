using System;
using System.Threading.Tasks;
using Alexa.NET.SkillFlow.Generator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGenerator:SkillFlowGenerator<CodeGeneratorContext>
    {
        protected override Task Begin(Scene scene, CodeGeneratorContext context)
        {
            context.Project = context.Project.AddDocument(scene.Name + ".cs", SyntaxFactory.ClassDeclaration(scene.Name)).Project;
            return base.Begin(scene, context);
        }
    }
}
