using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.SkillFlow.CodeGenerator;
using Microsoft.Build.Locator;

namespace Alexa.NET.SkillFlow.Tool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();

            var directory = new DirectoryInfo("./output");
            if (!directory.Exists)
            {
                directory.Create();
            }

            Story story;
            var file = new FileInfo("story.abc");
            using (var reader = file.OpenRead())
            {
                story = await new SkillFlowInterpreter().Interpret(reader);
            }


            var context = new CodeGeneratorContext();
            var generator = new CodeGenerator.CodeGenerator();
            await generator.Generate(story, context);

            var newStoryStream = new MemoryStream { Capacity = (int)file.Length };
            using (var tempStream = file.OpenRead())
            {
                tempStream.CopyTo(newStoryStream);
            }
            context.OtherFiles.Add(file.Name, newStoryStream);
            await context.Output(directory.FullName);
        }
    }
}
