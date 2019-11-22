using System.CodeDom;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public class TextTests
    {
        [Fact]
        public async Task MultipleSayAddsRandomMethodSelection()
        {
            var story = TestStory();
            var testText = story.Scenes["test"].Say.Content;
            testText.Add("this is another test");

            var context = await GenerateTestStory(story);
            var className = context.GetClass("Scene_Test");
            var generate = className.GenerateMethod();
            var setSayText = generate.Statements.OfType<CodeVariableDeclarationStatement>().LastOrDefault();

            Assert.NotNull(setSayText);
            var speechSelection = setSayText.InitExpression as CodeMethodInvokeExpression;
            Assert.NotNull(speechSelection);
            Assert.Equal("PickRandom",speechSelection.Method.MethodName);
            Assert.Equal(testText.Count, speechSelection.Parameters.Count);
        }

        private readonly CodeGenerator _generator = new CodeGenerator();

        private async Task<CodeGeneratorContext> GenerateTestStory(Story story = null)
        {
            var context = new CodeGeneratorContext();
            var genStory = story ?? TestStory();
            await _generator.Generate(genStory, context);
            return context;
        }

        private Story TestStory()
        {
            var say = new Text("say");
            say.Add(new TextLine("this is a test"));

            var scene = new Scene("test");
            scene.Add(say);

            var story = new Story();
            story.Scenes.Add("test", scene);

            return story;
        }
    }
}
