using System;
using System.CodeDom;
using System.Security.Cryptography;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Randomiser
    {
        public static CodeTypeDeclaration Generate()
        {
            var classDef = new CodeTypeDeclaration("Randomiser");

            var rndGenerator = new CodeMemberField(typeof(RandomNumberGenerator).AsSimpleName(), "_generator")
            {
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                InitExpression = new CodeMethodInvokeExpression(typeof(RandomNumberGenerator).AsSimpleExpression(), "Create")
            };

            classDef.Members.Add(rndGenerator);
            classDef.Members.Add(CreateNextDouble());
            classDef.Members.Add(CreateNext());
            classDef.Members.Add(CreatePickRandom());

            return classDef;
        }

        private static CodeTypeMember CreatePickRandom()
        {
            var pickRandom = new CodeMemberMethod
            {
                Name = "PickRandom",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                TypeParameters = { new CodeTypeParameter("T")},
                ReturnType = new CodeTypeReference("T")
            };

            var optionParam = new CodeParameterDeclarationExpression(new CodeTypeReference("T[]"), "options");
            optionParam.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.ParamArrayAttribute))));
            pickRandom.Parameters.Add(optionParam);

            var randomiserCall = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("Randomiser"),
                "Next",
                new CodePrimitiveExpression(0),
                new CodeBinaryOperatorExpression(
                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("options"), "Length"),
                    CodeBinaryOperatorType.Subtract,
                    new CodePrimitiveExpression(1)));

            var index = new CodeIndexerExpression(new CodeVariableReferenceExpression("options"),randomiserCall);
            pickRandom.Statements.Add(new CodeMethodReturnStatement(index));

            //public static string PickRandom(params string[] options)
            //{
            //    return options[Randomiser.Next(0, options.Length - 1)];
            //}

            return pickRandom;
        }

        private static CodeTypeMember CreateNext()
        {
            var next = new CodeMemberMethod
            {
                Name = "Next",
                Attributes = MemberAttributes.Static,
                ReturnType =new CodeTypeReference(typeof(int))
            };
            next.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "min"));
            next.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "max"));

            var subtractions = new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression("max"),
                CodeBinaryOperatorType.Subtract,
                new CodeBinaryOperatorExpression(
                    new CodeVariableReferenceExpression("min"),
                    CodeBinaryOperatorType.Subtract,
                    new CodePrimitiveExpression(1)));

            var multiply = new CodeBinaryOperatorExpression(
                new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Randomiser"), "NextDouble"),
                CodeBinaryOperatorType.Multiply,
                subtractions);
            var round = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(typeof(Math)), "Round", multiply); 
            var addition = new CodeBinaryOperatorExpression(round,CodeBinaryOperatorType.Add,new CodeVariableReferenceExpression("min"));
            next.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), "final", new CodeCastExpression(typeof(int), addition)));
            next.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("final")));

            return next;
        }

        private static CodeTypeMember CreateNextDouble()
        {
            var nextDouble = new CodeMemberMethod
            {
                Name = "NextDouble",
                Attributes = MemberAttributes.Static,
                ReturnType = new CodeTypeReference(typeof(double))
            };
            nextDouble.Statements.Add(new CodeVariableDeclarationStatement(typeof(byte[]), "b",
                new CodeArrayCreateExpression(typeof(byte), 4)));
            nextDouble.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_generator"),
                "GetBytes", new CodeVariableReferenceExpression("b")));
            var mathOp = CreateMathOp();
            nextDouble.Statements.Add(new CodeMethodReturnStatement(mathOp));
            return nextDouble;
        }

        private static CodeExpression CreateMathOp()
        {
            var lhs = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(BitConverter)),"ToUInt32",new CodeVariableReferenceExpression("b"),new CodePrimitiveExpression(0));
            var rhs = new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(typeof(UInt32)),"MaxValue");
            return new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.Divide, rhs);
        }

        public static void Ensure(CodeGeneratorContext context)
        {
            if (context.SceneFiles.ContainsKey("Randomiser"))
            {
                return;
            }

            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("System.Security.Cryptography"));
            code.Namespaces.Add(ns);

            var randomiserClass = CodeGeneration_Randomiser.Generate();
            ns.Types.Add(randomiserClass);

            context.SceneFiles.Add("Randomiser", code);
        }
    }
}
