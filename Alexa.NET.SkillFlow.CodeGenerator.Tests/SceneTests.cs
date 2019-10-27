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
            Assert.Equal("SkillFlow",codeDom.Value.Namespaces[0].Name);
        }

        [Fact]
        public async Task SceneCreatesCorrectClass()
        {
            var classType = await GetClass();
            Assert.Equal("Scene_Test", classType.Name);
            Assert.True(classType.IsClass);
        }

        [Fact]
        public async Task SceneCreatesGenerateMethod()
        {
            var classType = await GetClass();
            var method = classType.Members.OfType<CodeMemberMethod>().First();
            Assert.Equal("Generate",method.Name);
        }

        [Fact]
        public async Task GenerateMethodContainsNotImplementedException()
        {
            var classType = await GetClass();
            var throwstatement = classType.Members.OfType<CodeMemberMethod>().First().Statements.OfType<CodeThrowExceptionStatement>().First();
            var exceptionCreation = Assert.IsType<CodeObjectCreateExpression>(throwstatement.ToThrow);
            Assert.Equal(typeof(NotImplementedException).ToString(),exceptionCreation.CreateType.BaseType);
            
        }

        private CodeGenerator _generator;

        private async Task<CodeTypeDeclaration> GetClass()
        {
            var context = await GenerateTestStory();
            var codeDom = Assert.Single(context.CodeFiles);
            var type = Assert.Single(codeDom.Value.Namespaces[0].Types);
            var classType = Assert.IsType<CodeTypeDeclaration>(type);
            return classType;
        }

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
