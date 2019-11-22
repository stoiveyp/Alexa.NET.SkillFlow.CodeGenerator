using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public class VisualTests
    {
        [Fact]
        public async Task VisualGeneratesAPLHelper()
        {
            var story = TestStory();
            var context = await GenerateTestStory(story);
            Assert.True(context.SceneFiles.ContainsKey("APLHelper"));
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

            var visual = new Visual();
            visual.Add(new VisualProperty("template", "default"));
            visual.Add(new VisualProperty("background", "https://www.example.com/url/to/image.jpg"));
            visual.Add(new VisualProperty("title", "ACE Framework"));
            visual.Add(new VisualProperty("subtitle", ""));

            var scene = new Scene("test");
            scene.Add(say);
            scene.Add(visual);

            var story = new Story();
            story.Scenes.Add("test", scene);

            return story;
        }
    }
}
