using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using Alexa.NET.Management.Skills;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeOutput
    {
        public static Task CreateIn(CodeGeneratorContext context, string directoryFullName)
        {
            var json = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.Indented });

            using (var csharp = CodeDomProvider.CreateProvider(CodeDomProvider.GetLanguageFromExtension(".cs")))
            {
                var sceneFileDirectory = Path.Combine(directoryFullName, "Scenes");
                var handlerFileDirectory = Path.Combine(directoryFullName, "RequestHandlers");
                Directory.CreateDirectory(sceneFileDirectory);
                Directory.CreateDirectory(handlerFileDirectory);

                return Task.WhenAll(
                    OutputRootFiles(context.OtherFiles, csharp, json, directoryFullName),
                    OutputSkillManifest(context, json, directoryFullName),
                    OutputSceneFiles(context.SceneFiles, csharp, sceneFileDirectory),
                    OutputRequestHandlers(context.RequestHandlers, csharp, handlerFileDirectory)
                );
            }
        }

        private static async Task OutputSkillManifest(CodeGeneratorContext context, JsonSerializer json, string directoryFullName)
        {
            var language = context.Language;
            var oldName = language.InvocationName;
            language.InvocationName = context.InvocationName;
            using (var manifestStream = File.Open(Path.Combine(directoryFullName, "skillManifest.json"), FileMode.Create, FileAccess.Write))
            {
                using (var jsonWriter = new JsonTextWriter(new StreamWriter(manifestStream)))
                {
                    var interactionModel = new SkillInteraction
                    {
                        Language = language
                    };
                    json.Serialize(jsonWriter, interactionModel);
                    await jsonWriter.FlushAsync();
                }
            }

            language.InvocationName = oldName;
        }

        private static Task OutputRootFiles(Dictionary<string, object> otherFiles, CodeDomProvider csharp, JsonSerializer json, string directoryFullName)
        {
            return Task.WhenAll(otherFiles.Select(async supplemental =>
            {
                var writer = File.Open(Path.Combine(directoryFullName, supplemental.Key), FileMode.Create,
                    FileAccess.Write);
                if (supplemental.Value is Stream suppStream)
                {
                    using (writer)
                    {
                        suppStream.Seek(0, SeekOrigin.Begin);
                        await suppStream.CopyToAsync(writer);
                    }
                }
                else if (supplemental.Value is CodeCompileUnit unit)
                {
                    using (var sw = new StreamWriter(writer))
                    {
                        csharp.GenerateCodeFromCompileUnit(
                            unit,
                            sw,
                            new System.CodeDom.Compiler.CodeGeneratorOptions());
                        await sw.FlushAsync();
                    }
                }
                else
                {
                    using (var jsonWriter = new JsonTextWriter(new StreamWriter(writer)))
                    {
                        json.Serialize(jsonWriter, supplemental.Value);
                    }
                }
            }));
        }

        private static Task OutputRequestHandlers(Dictionary<string, CodeCompileUnit> handlers, CodeDomProvider csharp, string directoryFullName)
        {
            //Need to wire up prepend and append into scene navigation
            return Task.WhenAll(handlers.Select(async c =>
            {
                using (var textWriter =
                    new StreamWriter(File.Open(Path.Combine(directoryFullName, c.Key.Safe()) + ".cs", FileMode.Create, FileAccess.Write)))
                {
                    csharp.GenerateCodeFromCompileUnit(
                        c.Value,
                        textWriter,
                        new System.CodeDom.Compiler.CodeGeneratorOptions());
                    await textWriter.FlushAsync();
                }

            }));
        }

        private static Task OutputSceneFiles(Dictionary<string, CodeCompileUnit> scenes, CodeDomProvider csharp, string directoryFullName)
        {
            var prependScene = CodeGeneration_Scene.SceneClassName("Global Prepend");
            var appendScene = CodeGeneration_Scene.SceneClassName("Global Append");
            var prepend = scenes.Any(kvp => kvp.Key.Equals(prependScene, System.StringComparison.OrdinalIgnoreCase));
            var append = scenes.Any(kvp => kvp.Key.Equals(appendScene, System.StringComparison.OrdinalIgnoreCase));

            return Task.WhenAll(scenes.Select(async c =>
            {
                var type = c.Value.FirstType();
                if (!type.Name.Equals(prependScene,StringComparison.OrdinalIgnoreCase) && 
                    !type.Name.Equals(appendScene, StringComparison.OrdinalIgnoreCase))
                {
                    var interaction = c.Value.FirstType().MethodStatements(CodeConstants.SceneInteractionMethod);
                    if (interaction?.OfType<CodeSnippetStatement>().Any() ?? false)
                    {
                        var mainCall = interaction.OfType<CodeSnippetStatement>()
                            .First(ss => ss.Value.EndsWith(CodeConstants.ScenePrimaryMethod.ToLower() + "\":"));
                        var mainInteract = interaction[interaction.IndexOf(mainCall) + 1];
                        if (prepend)
                        {
                            interaction.Insert(interaction.IndexOf(mainInteract),
                                new CodeExpressionStatement(CodeGeneration_Navigation.GoToScene("global prepend")));
                        }

                        if (append)
                        {
                            interaction.Insert(interaction.IndexOf(mainInteract) + 1,
                                new CodeExpressionStatement(CodeGeneration_Navigation.GoToScene("global append")));
                        }
                    }
                }

                using (var textWriter =
                    new StreamWriter(File.Open(Path.Combine(directoryFullName, c.Key.Safe()) + ".cs", FileMode.Create, FileAccess.Write)))
                {
                    csharp.GenerateCodeFromCompileUnit(
                        c.Value,
                        textWriter,
                        new System.CodeDom.Compiler.CodeGeneratorOptions());
                    await textWriter.FlushAsync();
                }

            }));
        }
    }
}