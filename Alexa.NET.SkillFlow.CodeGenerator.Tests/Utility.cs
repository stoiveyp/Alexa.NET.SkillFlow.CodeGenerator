using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Alexa.NET.SkillFlow.CodeGenerator.Tests
{
    public static class Utility
    {
        public static CodeTypeDeclaration GetClass(this CodeGeneratorContext context, string filenameWithoutExtension)
        {
            if (!context.SceneFiles.ContainsKey(filenameWithoutExtension))
            {
                throw new KeyNotFoundException("Unable to find code with the name " + filenameWithoutExtension);
            }

            var codeDom = context.SceneFiles[filenameWithoutExtension];
            var type = Assert.Single(codeDom.Namespaces[0].Types);
            var classType = Assert.IsType<CodeTypeDeclaration>(type);
            return classType;
        }

        public static CodeMemberMethod GenerateMethod(this CodeTypeDeclaration classType)
        {
            return classType.Members.OfType<CodeMemberMethod>().First(cmm => cmm.Name == "Generate");
        }
    }
}
