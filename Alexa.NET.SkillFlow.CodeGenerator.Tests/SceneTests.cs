using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public class SceneTests
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
            Assert.Equal("SkillFlowGenerated",codeDom.Value.Namespaces[0].Name);
        }

        [Fact]
        public async Task SceneCreatesCorrectClass()
        {
            var context = await GenerateTestStory();
            var classType = context.GetClass("Scene_Test");
            Assert.Equal("Scene_Test", classType.Name);
            Assert.True(classType.IsClass);
        }

        [Fact]
        public async Task SceneCreatesGenerateMethod()
        {
            var context = await GenerateTestStory();
            var classType = context.GetClass("Scene_Test");
            var method = classType.Members.OfType<CodeMemberMethod>().First();
            Assert.Equal("Generate",method.Name);
        }

        [Fact]
        public async Task GenerateMethodContainsNotImplementedException()
        {
            var context = await GenerateTestStory();
            var classType = context.GetClass("Scene_Test");
            var throwstatement = classType.GenerateMethod().Statements.OfType<CodeThrowExceptionStatement>().First();
            var exceptionCreation = Assert.IsType<CodeObjectCreateExpression>(throwstatement.ToThrow);
            Assert.Equal(typeof(NotImplementedException).ToString(),exceptionCreation.CreateType.BaseType);
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
