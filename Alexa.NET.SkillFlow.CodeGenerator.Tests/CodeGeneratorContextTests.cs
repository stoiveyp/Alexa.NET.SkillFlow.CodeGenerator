using System;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public class CodeGeneratorContextTests
    {
        [Fact]
        public void ContextConstructorSetsOptions()
        {
            var options = new CodeGeneratorOptions();
            var context = new CodeGeneratorContext(options);
            Assert.Equal(options, context.Options);
        }

        [Fact]
        public void ContextSetsDefaultOutput()
        {
            var context = new CodeGeneratorContext();
            Assert.NotNull(context.SceneFiles);
            Assert.NotNull(context.Language);
        }
    }
}
