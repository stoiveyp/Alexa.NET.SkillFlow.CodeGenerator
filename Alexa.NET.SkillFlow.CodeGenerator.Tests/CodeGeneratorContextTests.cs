using System;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public class CodeGeneratorContextTests
    {
        [Fact]
        public void EmptyConstructorGeneratesDefaultOptions()
        {
            var context = new CodeGeneratorContext();
            Assert.Equal(CodeGeneratorOptions.Default, context.Options);
        }

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
            Assert.NotNull(context.CodeFiles);
            Assert.NotNull(context.Language);
        }
    }
}
