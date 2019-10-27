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
            var context = await GenerateTestStory();
            Assert.Single(context.CodeFiles);
        }

        [Fact]
        public async Task SceneCreatesCorrectNamespace()
        {
            var context = await GenerateTestStory();
            var codeDom = Assert.Single(context.CodeFiles);
            Assert.Equal("SkillFlow",codeDom.Value.Namespaces[0].Name);
        }

        [Fact]
        public async Task SceneCreatesCorrectClass()
        {
            var context = await GenerateTestStory();
            var codeDom = Assert.Single(context.CodeFiles);
            Assert.Equal("Scene_Test",codeDom.Value.Namespaces[0].Types[0].Name);
        }

        private CodeGenerator _generator;

        private async Task<CodeGeneratorContext> GenerateTestStory()
        {
            var context = new CodeGeneratorContext();
            var story = TestStory();
            _generator = new CodeGenerator();
            await _generator.Generate(story, context);
            return context;
        }

        private Story TestStory()
        {
            var story = new Story();
            story.Scenes.Add("test", new Scene("test"));
            return story;
        }
    }
}
