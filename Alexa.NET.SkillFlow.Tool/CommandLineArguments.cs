using Alexa.NET.SkillFlow.CodeGenerator;
using CommandLine;

namespace Alexa.NET.SkillFlow.Tool
{
    internal class CommandLineArguments
    {
        [Option('i',"input",HelpText="The skill flow story file",Required = true)]
        public string Input { get; set; }

        [Option('o',"output",HelpText = "The directory to place the code into")]
        public string Output { get; set; }

        [Option('s',"skill",HelpText="The invocation name for the skill")]
        public string SkillName { get; set; }

        [Option('r',"root",HelpText="The root namespace")]
        public string RootNamespace { get; set; }

        public CodeGeneratorOptions ToCodeGenerator()
        {
            var options = new CodeGeneratorOptions();
            if (!string.IsNullOrWhiteSpace(RootNamespace))
            {
                options.RootNamespace = RootNamespace;
            }

            if (!string.IsNullOrWhiteSpace(SkillName))
            {
                options.SkillName = SkillName;
            }

            return options;
        }
    }
}