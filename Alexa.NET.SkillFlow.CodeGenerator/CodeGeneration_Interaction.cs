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
                ReturnType = new CodeTypeReference("async Task")
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
            interactions.AddInteraction(context.Marker,invoke,true);
        }

        public static void AddIntent(CodeGeneratorContext context, List<string> hearPhrases, CodeStatementCollection statements)
        {
            var fallback = hearPhrases.Any(p => p == "*");
            if (fallback)
            {
                hearPhrases.Remove("*");
            }

            var dictionary = hearPhrases.ToDictionary(hp => hp, hp => context.Language.IntentTypes?.FirstOrDefault(i => i.Samples.Contains(hp)));

            var nulls = dictionary.Where(kvp => kvp.Value == null).Select(k => k.Key).ToArray();

            var intentName = context.Marker;
            if (nulls.Any())
            {
                var intent = new IntentType
                {
                    Name = intentName,
                    Samples = nulls
                };

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);

                foreach (var it in nulls.ToArray())
                {
                    var unit = context.CreateIntentRequestHandler(intentName);
                    AddHandlerCheck(unit.FirstType().MethodStatements(CodeConstants.HandlerPrimaryMethod), context);
                    dictionary[it] = intent;
                }
            }

            foreach (var item in dictionary.Keys.ToArray().Except(nulls))
            {
                UpdateSharedIntent(context,dictionary,item);
            }

            if (fallback)
            {
                statements.Add(CodeGeneration_Navigation.EnableCandidate(CodeConstants.FallbackMarker));
            }

        }

        private static void UpdateSharedIntent(
            CodeGeneratorContext context, 
            Dictionary<string,IntentType> dictionary,
            string item)
        {
            var sharedIntent = dictionary[item];
            var sharedHandlerClass = context.RequestHandlers[sharedIntent.Name].FirstType();

            var safeItemName = item.Safe();
            if (sharedIntent.Samples.Length > 1)
            {
                //Move phrase to its own intent
                sharedIntent.Samples = sharedIntent.Samples.Except(new[] {item}).ToArray();
                var intent = new IntentType
                {
                    Name = safeItemName,
                    Samples = new[] {item}
                };
                sharedIntent = intent;

                context.Language.IntentTypes = context.Language.IntentTypes.Add(intent);
                dictionary[item] = intent;
                var newHandler = context.CreateIntentRequestHandler(safeItemName);
                var sharedStatements = sharedHandlerClass.MethodStatements(CodeConstants.HandlerPrimaryMethod);
                var newStatements = newHandler.FirstType().MethodStatements(CodeConstants.HandlerPrimaryMethod);

                for (var stmtIndex = 1; stmtIndex < sharedStatements.Count - 2;stmtIndex++)
                {
                    newStatements.AddBeforeReturn(sharedStatements[stmtIndex]);
                }

                
                AddHandlerCheck(newStatements, context);
            }
        

            var originalName = sharedIntent.Name;
            sharedIntent.Name = safeItemName;
            var rh = context.RequestHandlers[originalName];

            context.RequestHandlers.Remove(originalName);
            context.RequestHandlers.Add(safeItemName, rh);

            var handlerType = rh.FirstType();
            handlerType.Name = safeItemName;
            var constructor = handlerType.Members.OfType<CodeConstructor>().First();
            constructor.BaseConstructorArgs.Clear();
            constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(safeItemName));
        }

        private static void AddHandlerCheck(CodeStatementCollection newStatements, CodeGeneratorContext context)
        {
            newStatements.AddBeforeReturn(new CodeConditionStatement(
                new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression("Navigation"), 
                        CodeConstants.IsCandidateMethodName,
                        new CodeVariableReferenceExpression("request"),
                        new CodePrimitiveExpression(context.Marker)),
                new CodeExpressionStatement(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression("await Navigation"),
                    CodeConstants.NavigationMethodName,
                    new CodePrimitiveExpression(context.Marker),
                    new CodeVariableReferenceExpression("request")))));
        }
    }
}