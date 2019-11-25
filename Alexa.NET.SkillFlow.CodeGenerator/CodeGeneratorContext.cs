using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Management.InteractionModel;
using Alexa.NET.Management.Skills;
using Alexa.NET.SkillFlow.Generator;
using Newtonsoft.Json;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneratorContext : SkillFlowContext
    {
        public CodeGeneratorContext() : this(CodeGeneratorOptions.Default)
        {

        }

        public CodeGeneratorContext(CodeGeneratorOptions options)
        {
            Options = options ?? CodeGeneratorOptions.Default;
        }

        public string InvocationName { get; set; } = "Skill Flow";

        public Language Language { get; set; } = new Language();

        public Dictionary<string, object> OtherFiles { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, CodeCompileUnit> RequestHandlers { get; set; } = new Dictionary<string, CodeCompileUnit>();

        public Dictionary<string, CodeCompileUnit> SceneFiles { get; } = new Dictionary<string, CodeCompileUnit>();

        public CodeGeneratorOptions Options { get; protected set; }
        public Stack<CodeObject> CodeScope { get; set; } = new Stack<CodeObject>();
        public string Marker => GenerateMarker(0);

        public string GenerateMarker(int pop, int ignore = 0)
        {
            return string.Join("_", CodeScope.Skip(pop).Reverse().Skip(ignore).Select(GetName));
        }

        private string GetName(CodeObject codeScope)
        {
            switch (codeScope)
            {
                case CodeMemberMethod method:
                    return method.Name;
                case CodeTypeDeclaration type:
                    return type.Name;
                default:
                    return "unknown";
            }
        }

        public Task Output(string directoryFullName)
        {
            return CodeOutput.CreateIn(this, directoryFullName);
        }



        public Dictionary<string, string> Slots { get; } = new Dictionary<string, string>();

        public void SetSlotType(string name, string type)
        {
            if (Slots.ContainsKey(name))
            {
                Slots[name] = type;
            }
            else
            {
                Slots.Add(name, type);
            }
        }
    }
}
