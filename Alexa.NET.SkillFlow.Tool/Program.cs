﻿using System;
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

            var story = new Story();
            story.Scenes.Add("test", new Scene("test"));

            var context = new CodeGeneratorContext();
            var generator = new CodeGenerator.CodeGenerator();
            await generator.Generate(story, context);
            await context.Output(directory.FullName);
        }
    }
}
