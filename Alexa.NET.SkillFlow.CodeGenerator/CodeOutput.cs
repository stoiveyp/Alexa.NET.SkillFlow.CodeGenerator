﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using Alexa.NET.Management.InteractionModel;
using Alexa.NET.Management.Skills;
using Newtonsoft.Json.Linq;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeOutput
    {
        public static Task CreateIn(CodeGeneratorContext context, string directoryFullName)
        {
            CodeGeneration_Story.CreateProjectFile(context);
            if (!context.RequestHandlers.ContainsKey("AMAZON.StopIntent"))
            {
                CodeGeneration_Interaction.AddIntent(context, new List<string> {"stop"}, new CodeStatementCollection());
            }

            UpdatePipeline((CodeCompileUnit)context.OtherFiles["Pipeline.cs"], context.RequestHandlers.Keys.ToArray());
            CodeGeneration_Fallback.Ensure(context);

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

        private static void UpdatePipeline(CodeCompileUnit code, string[] requestHandlers)
        {
            var containsStop = requestHandlers.Contains("AMAZON.StopIntent".Safe());
            var array = new CodeArrayCreateExpression(new CodeTypeReference("IAlexaRequestHandler<APLSkillRequest>[]"));
            foreach (var requestHandler in requestHandlers.OrderBy(rh => rh.Length))
            {
                array.Initializers.Add(new CodeObjectCreateExpression(requestHandler.Safe()));
            }

            if (!containsStop)
            {
                array.Initializers.Add(new CodeObjectCreateExpression("AMAZON.StopIntent".Safe()));
            }

            array.Initializers.Add(new CodeObjectCreateExpression("AMAZON.FallbackIntent".Safe()));
            var pipeline = new CodeObjectCreateExpression(new CodeTypeReference("AlexaRequestPipeline<APLSkillRequest>"), array);


            var constructor = code.FirstType().Members.OfType<CodeTypeConstructor>().First();
            constructor.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("_pipeline"),
                pipeline));
        }

        private static async Task OutputSkillManifest(CodeGeneratorContext context, JsonSerializer json, string directoryFullName)
        {
            var language = context.Language;
            var oldName = language.InvocationName;
            language.InvocationName = context.Options.InvocationName;
            using (var manifestStream = File.Open(Path.Combine(directoryFullName, "skillManifest.json"), FileMode.Create, FileAccess.Write))
            {
                using (var jsonWriter = new JsonTextWriter(new StreamWriter(manifestStream)))
                {
                    var interactionModel = new SkillInteraction
                    {
                        Language = language
                    };
                    var jobject = new JObject {{"interactionModel", JObject.FromObject(interactionModel)}};
                    json.Serialize(jsonWriter, jobject);
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
                AddFallback(c.Value.FirstType().MethodStatements(CodeConstants.HandlerPrimaryMethod));
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

        private static void AddFallback(CodeStatementCollection newStatements)
        {
            newStatements.AddBeforeReturn(new CodeConditionStatement(
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("handled"), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(false)),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("await Output"),
                    "Fallback", new CodeVariableReferenceExpression("request")))));
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
                if (!type.Name.Equals(prependScene, StringComparison.OrdinalIgnoreCase) &&
                    !type.Name.Equals(appendScene, StringComparison.OrdinalIgnoreCase))
                {
                    var interaction = c.Value.FirstType().MethodStatements(CodeConstants.SceneInteractionMethod);
                    if (interaction?.OfType<CodeSnippetStatement>().Any() ?? false)
                    {
                        var mainCall = interaction.OfType<CodeSnippetStatement>()
                            .First(ss => ss.Value.EndsWith(CodeConstants.ScenePrimaryMethod.ToLower() + "\":"));
                        var mainInteract = interaction[interaction.IndexOf(mainCall) + 2];


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