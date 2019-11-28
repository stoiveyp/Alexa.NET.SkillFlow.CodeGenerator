using System;
using System.IO;
using System.Threading.Tasks;
using Alexa.NET.SkillFlow.CodeGenerator;
using CommandLine;

namespace Alexa.NET.SkillFlow.Tool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Task task = null;
            Parser.Default.ParseArguments<CommandLineArguments>(args).WithParsed(cla =>
                {
                    Console.Out.WriteLine($"Processing {Path.GetFileName(cla.Input)}. Output in {cla.Output}");
                    task = ProcessArguments(cla);
                }).WithNotParsed(e => { task = Task.FromResult((object) null); });
            if (task != null)
            {
                await task;
                Console.Out.WriteLine("Processing Complete");
            }
        }

        private static async Task ProcessArguments(CommandLineArguments args)
        {
            var directory = new DirectoryInfo(args.Output);
            if (!directory.Exists)
            {
                directory.Create();
            }

            Story story;
            var file = new FileInfo(args.Input);
            using (var reader = file.OpenRead())
            {
                story = await new SkillFlowInterpreter().Interpret(reader);
            }


            var context = new CodeGeneratorContext(args.ToCodeGenerator());
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
