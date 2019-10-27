using System.CodeDom;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public class SayTests
    {
        [Fact]
        public async Task SayAddsToResponse()
        {
            var context = await GenerateTestStory();
            var className = context.GetClass("Scene_Test");
            var generate = className.GenerateMethod();
            var setSayText = generate.Statements.OfType<CodeAssignStatement>().FirstOrDefault();
            Assert.NotNull(setSayText);
        }

        private CodeGenerator _generator;

        private async Task<CodeGeneratorContext> GenerateTestStory(Story story = null)
        {
            var context = new CodeGeneratorContext();
            var genStory = story ?? TestStory();
            _generator = new CodeGenerator();
            await _generator.Generate(genStory, context);
            return context;
        }

        private Story TestStory()
        {
            var say = new Text("say");
            say.Add(new TextLine("this is a test"));

            var scene = new Scene("test");
            scene.Add(new Text("say"));

            var story = new Story();
            story.Scenes.Add("test", scene);

            return story;
        }
    }
}
