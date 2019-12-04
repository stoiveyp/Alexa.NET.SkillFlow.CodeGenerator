using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Alexa.NET.Management.InteractionModel;
using Alexa.NET.Request.Type;
using Alexa.NET.Response.Directive;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public class CodeGeneration_Interaction
    {
        public static void AddHearMarker(CodeGeneratorContext context, CodeStatementCollection statements)
        {
            while (context.CodeScope.Peek().GetType() != typeof(CodeTypeDeclaration))
            {
                context.CodeScope.Pop();
            }

            var type = context.CodeScope.Peek() as CodeTypeDeclaration;
            var count = type.Members.OfType<CodeMemberMethod>().Count(m => m.Name.StartsWith("Hear"));
            var newMethod = new CodeMemberMethod
            {
                Name = "Hear_" + count,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = CodeConstants.AsyncTask
            };
            newMethod.AddFlowParams();
            type.Members.Add(newMethod);
            context.CodeScope.Push(newMethod);

            var interactions = type.MethodStatements(CodeConstants.SceneInteractionMethod);

            var invoke = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await " + type.Name),
                newMethod.Name);
            invoke.AddFlowParameters();

            statements.Add(CodeGeneration_Navigation.EnableCandidate(context.Marker));
            interactions.AddInteraction(context.Marker, invoke, true);
        }

        public static void AddIntent(CodeGeneratorContext context, List<string> hearPhrases, CodeStatementCollection statements)
        {
            var fallback = hearPhrases.Any(p => p == "*");
            if (fallback)
            {
                hearPhrases.Remove("*");
            }

            string CheckForDefaults(string key)
            {
                if (key.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    return "AMAZON.YesIntent";
                }

                if (key.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    return "AMAZON.NoIntent";
                }

                if (key.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    return "AMAZON.HelpIntent";
                }

                return key;
            }

            var dictionary = hearPhrases.Select(CheckForDefaults).ToDictionary(hp => hp, hp => context.Language.IntentTypes?.FirstOrDefault(i => i.Samples.Contains(hp)));

            var nullPhrases = dictionary.Where(kvp => kvp.Value == null).Select(k => k.Key).ToArray();

            var intentName = context.Marker;
            foreach (var nulls in nullPhrases.GroupBy(n => n.StartsWith("AMAZON.") ? n : string.Empty)
                .Select(g => g.ToArray()))
            {
                if (!nulls.Any()) continue;

                var azType = nulls.Length == 1 && nulls.First().StartsWith("AMAZON.");
                if (azType)
                {
                    intentName = nulls.Single();
                }

                var intent = new IntentType
                {
                    Name = intentName,
                    Samples = azType ? new string[] { } : nulls
                };

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);

                foreach (var it in nulls.ToArray())
                {
                    var unit = context.CreateIntentRequestHandler(intentName);
                    AddHandlerCheck(unit.FirstType().MethodStatements(CodeConstants.HandlerPrimaryMethod), context);
                    dictionary[it] = intent;
                }
            }

            foreach (var item in dictionary.Keys.ToArray().Except(nullPhrases))
            {
                UpdateSharedIntent(context, dictionary, item);
            }

            if (fallback)
            {
                statements.Add(CodeGeneration_Navigation.EnableCandidate(CodeConstants.FallbackMarker));
            }
        }

        private static void UpdateSharedIntent(
            CodeGeneratorContext context,
            Dictionary<string, IntentType> dictionary,
            string item)
        {
            string AmazonSafeName(string original)
            {
                return original.StartsWith("AMAZON_") ? "AMAZON." + original.Substring(7) : original;
            }

            var sharedIntent = dictionary[item];
            var sharedHandlerClass = context.RequestHandlers[AmazonSafeName(sharedIntent.Name)].FirstType();

            var safeItemName = item.Safe();
            if (sharedIntent.Samples.Length > 1)
            {
                //Move phrase to its own intent
                sharedIntent.Samples = sharedIntent.Samples.Except(new[] { item }).ToArray();
                var intent = new IntentType
                {
                    Name = item,
                    Samples = item.StartsWith("AMAZON.") ? new string[] { } : new[] { item }
                };
                sharedIntent = intent;

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);
                dictionary[AmazonSafeName(item)] = intent;
                var newHandler = context.CreateIntentRequestHandler(safeItemName);
                var sharedStatements = sharedHandlerClass.MethodStatements(CodeConstants.HandlerPrimaryMethod);
                var newStatements = newHandler.FirstType().MethodStatements(CodeConstants.HandlerPrimaryMethod);

                for (var stmtIndex = 1; stmtIndex < sharedStatements.Count - 2; stmtIndex++)
                {
                    if (sharedStatements[stmtIndex] is CodeVariableDeclarationStatement)
                    {
                        continue;
                    }
                    newStatements.AddBeforeReturn(sharedStatements[stmtIndex]);
                }


                AddHandlerCheck(newStatements, context);
            }


            var originalName = sharedIntent.Name;
            sharedIntent.Name = item;
            var rh = context.RequestHandlers[AmazonSafeName(originalName.Safe())];

            context.RequestHandlers.Remove(AmazonSafeName(originalName.Safe()));
            context.RequestHandlers.Add(AmazonSafeName(safeItemName), rh);

            var handlerType = rh.FirstType();
            handlerType.Name = safeItemName;
            var constructor = handlerType.Members.OfType<CodeConstructor>().First();
            constructor.BaseConstructorArgs.Clear();
            constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(AmazonSafeName(safeItemName)));
        }

        public static void AddHandlerCheck(CodeStatementCollection newStatements, CodeGeneratorContext context, string marker = null)
        {
            newStatements.AddBeforeReturn(new CodeConditionStatement(
                new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression("Navigation"),
                        CodeConstants.IsCandidateMethodName,
                        new CodeVariableReferenceExpression("request"),
                        new CodePrimitiveExpression(marker ?? context.Marker)),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("await Navigation"),
                    CodeConstants.NavigationMethodName,
                    new CodePrimitiveExpression(context.Marker),
                    new CodeVariableReferenceExpression("request"))), new CodeAssignStatement(new CodeVariableReferenceExpression("handled"), new CodePrimitiveExpression(true))));
        }
    }
}