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
            var count = NumberAsWord(type.Members.OfType<CodeMemberMethod>().Count(m => m.Name.StartsWith("Hear")));
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
            interactions.AddInteraction(type.Name,context.Marker,invoke, true);
        }

        private static string NumberAsWord(int count)
        {
            return count switch
            {
                0 => "zero",
                1 => "one",
                2 => "two",
                3 => "three",
                4 => "four",
                5 => "five",
                6 => "six",
                7 => "seven",
                8 => "eight",
                9 => "nine",
                _ => "biggerthannine"
            };
        }

        public static void AddIntent(CodeGeneratorContext context, List<string> hearPhrases, CodeStatementCollection statements)
        {
            var fallback = hearPhrases.Any(p => p == "*");
            if (fallback)
            {
                hearPhrases.Remove("*");
            }

            var dictionary = hearPhrases.Select(hp => new PhraseLink(hp, context.Marker)).ToDictionary(hp => hp, hp => context.Language.IntentTypes?.FirstOrDefault(hp.IsMatch));

            var nullPhrases = dictionary.Where(kvp => kvp.Value == null).ToArray();

            foreach (var nulls in nullPhrases.GroupBy(np => np.Key.IntentName).Select(g => g.ToArray()))
            {
                if (!nulls.Any()) continue;
                var nullItem = nulls.First();
                var intent = new IntentType
                {
                    Name = nullItem.Key.IntentName,
                    Samples = nulls.Where(n => n.Key.Phrase != null).Select(n => n.Key.Phrase).ToArray(),
                    Slots = nulls.Where(n => n.Key.Phrase != null).SelectMany(n => n.Key.Slots(context)).GroupBy(s => s.Name).Select(g => g.First()).ToArray()
                };

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);


                var unit = context.CreateIntentRequestHandler(nullItem.Key.ClassName);
                AddHandlerCheck(unit.FirstType().MethodStatements(CodeConstants.HandlerPrimaryMethod), context);
                dictionary[nullItem.Key] = intent;
            }

            foreach (var item in dictionary.Where(kvp => nullPhrases.All(np => np.Key != kvp.Key)))
            {
                UpdateSharedIntent(context, item);
            }

            if (fallback)
            {
                statements.Add(CodeGeneration_Navigation.EnableCandidate(CodeConstants.FallbackMarker));
            }
        }

        private static void UpdateSharedIntent(
            CodeGeneratorContext context,
            KeyValuePair<PhraseLink, IntentType> item)
        {
            var sharedHandlerClass = context.RequestHandlers[item.Key.ClassName].FirstType();

            if (item.Value.Samples.Length > 1)
            {
                item.Value.Samples = item.Value.Samples.Except(new[] { item.Key.Phrase }).ToArray();
                var intent = new IntentType
                {
                    Name = item.Key.IntentName,
                    Samples = item.Key.Phrase == null ? new string[] { } : new[] { item.Key.Phrase }
                };
                item = new KeyValuePair<PhraseLink, IntentType>(item.Key, intent);

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);
            }

            var newHandler = context.CreateIntentRequestHandler(item.Key.ClassName);
            var sharedStatements = sharedHandlerClass.MethodStatements(CodeConstants.HandlerPrimaryMethod);
            var newStatements = newHandler.FirstType().MethodStatements(CodeConstants.HandlerPrimaryMethod);

            if (sharedStatements != newStatements)
            {
                for (var stmtIndex = 1; stmtIndex < sharedStatements.Count - 2; stmtIndex++)
                {
                    if (sharedStatements[stmtIndex] is CodeVariableDeclarationStatement)
                    {
                        continue;
                    }

                    newStatements.AddBeforeReturn(sharedStatements[stmtIndex]);
                }
            }

            AddHandlerCheck(newStatements, context);


            var rh = context.RequestHandlers[item.Key.ClassName];

            context.RequestHandlers.Remove(item.Key.ClassName);
            context.RequestHandlers.Add(item.Key.ClassName, rh);

            var handlerType = rh.FirstType();
            handlerType.Name = item.Key.ClassName;
            var constructor = handlerType.Members.OfType<CodeConstructor>().First();
            constructor.BaseConstructorArgs.Clear();
            constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(item.Value.Name));
        }

        public static void AddHandlerCheck(CodeStatementCollection newStatements, CodeGeneratorContext context, string marker = null)
        {
            newStatements.AddBeforeReturn(new CodeConditionStatement(
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("!handled"), CodeBinaryOperatorType.BooleanAnd,new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression("Navigation"),
                        CodeConstants.IsCandidateMethodName,
                        new CodeVariableReferenceExpression("request"),
                        new CodePrimitiveExpression(marker ?? context.Marker))),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("await Navigation"),
                    CodeConstants.NavigationMethodName,
                    new CodePrimitiveExpression(context.Marker),
                    new CodeVariableReferenceExpression("request"))), new CodeAssignStatement(new CodeVariableReferenceExpression("handled"), new CodePrimitiveExpression(true))));
        }
    }
}