using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public class CodeGeneratorTests
    {
        [Fact]
        public async Task SceneCreatesNewCompilationUnit()
        {
            var story = new Story();
            story.Scenes.Add("test", new Scene("test"));

            var generator = new CodeGenerator();
            var context = new CodeGeneratorContext();
            await generator.Generate(story, context);
            Assert.Single(context.Project.Documents);
        }
    }
}
