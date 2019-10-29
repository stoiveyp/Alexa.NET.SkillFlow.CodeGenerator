using System.CodeDom;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public class SayTests
    {
        [Fact]
        public async Task SingleSayAddsDirectToResponse()
        {
            var story = TestStory();
            var context = await GenerateTestStory(story);
            var className = context.GetClass("Scene_Test");
            var generate = className.GenerateMethod();
            var setSayText = generate.Statements.OfType<CodeAssignStatement>().Skip(1).FirstOrDefault();

            Assert.NotNull(setSayText);
            var leftHandSide = Assert.IsType<CodePropertyReferenceExpression>(setSayText.Left);
            var rightHandSide = Assert.IsType<CodeVariableReferenceExpression>(setSayText.Right);
            Assert.Equal(rightHandSide.VariableName, "say");
        }

        [Fact]
        public async Task MultipleSayAddsRandomMethodSelection()
        {
            var story = TestStory();
            var testText = story.Scenes["test"].Say.Content;
            testText.Add("this is another test");

            var context = await GenerateTestStory(story);
            var className = context.GetClass("Scene_Test");
            var generate = className.GenerateMethod();
            var setSayText = generate.Statements.OfType<CodeVariableDeclarationStatement>().FirstOrDefault();

            Assert.NotNull(setSayText);
            var speechSelection = setSayText.InitExpression as CodeMethodInvokeExpression;
            Assert.NotNull(speechSelection);
            Assert.Equal(testText.Count, speechSelection.Parameters.Count);
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
            scene.Add(say);

            var story = new Story();
            story.Scenes.Add("test", scene);

            return story;
        }
    }
}
