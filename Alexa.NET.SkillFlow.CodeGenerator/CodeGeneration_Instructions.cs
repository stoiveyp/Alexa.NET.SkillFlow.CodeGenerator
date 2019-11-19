using System;
using System.CodeDom;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Instructions
    {
        public static void SetMarker(this CodeGeneratorContext context, CodeStatementCollection statements, int skip = 0)
        {
            EnsureStateMaintenance(context);
            SetVariable(statements, "_marker", context.GenerateMarker(skip), false);
        }

        public static void SetVariable(this CodeStatementCollection statements, string variableName, object value, bool gameVariable = true)
        {
            var setVariable = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "SetValue",
                new CodePrimitiveExpression((gameVariable ? "game_" : string.Empty) + variableName),
                new CodePrimitiveExpression(value));

            statements.Add(setVariable);
        }

        public static void Decrease(this CodeStatementCollection statements, string variableName, int amount)
        {
            var setVariable = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "Decrease",
                new CodePrimitiveExpression(variableName),
                new CodePrimitiveExpression(amount));

            statements.Add(setVariable);
        }

        public static void Increase(this CodeStatementCollection statements, string variableName, int amount)
        {
            var setVariable = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "Increase",
                new CodePrimitiveExpression(variableName),
                new CodePrimitiveExpression(amount));

            statements.Add(setVariable);
        }

        public static void Clear(this CodeStatementCollection statements, string name)
        {
            var clearCall = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "Clear",
                new CodePrimitiveExpression("game_" + name));

            statements.Add(clearCall);
        }

        public static void ClearAll(this CodeStatementCollection statements)
        {
            var clearCall = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "ClearAll");
            statements.Add(clearCall);
        }

        public static CodeMethodInvokeExpression GetVariable(string variableName, Type type)
        {
            var getStmt = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue",
                new CodePrimitiveExpression("game_" + variableName));
            getStmt.Method.TypeArguments.Add(type);
            return getStmt;
        }

        public static void GenerateGoTo(this CodeStatementCollection statements, string sceneName)
        {
            statements.Add(new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("await " + CodeGeneration_Scene.SceneClassName(sceneName)),
                "Generate",
                new CodeVariableReferenceExpression("request"),
                new CodeVariableReferenceExpression("responseBody")));
        }

        public static void EnsureStateMaintenance(CodeGeneratorContext context)
        {
            if (context.OtherFiles.ContainsKey("State.cs"))
            {
                return;
            }

            context.OtherFiles.Add("State.cs", CreateStateFile(context, CreateStateClass()));
        }

        private static CodeTypeDeclaration CreateStateClass()
        {
            var type = new CodeTypeDeclaration
            {
                Name = "State",
                Attributes = MemberAttributes.Public
            };


            type.StartDirectives.Add(new CodeRegionDirective(
                CodeRegionMode.Start, Environment.NewLine + "\tstatic"));

            type.EndDirectives.Add(new CodeRegionDirective(
                CodeRegionMode.End, string.Empty));


            type.Members.Add(CreateSetMethod());
            type.Members.Add(CreateGetMethod());
            type.Members.Add(CreateIncreaseMethod());
            type.Members.Add(CreateDecreaseMethod());
            type.Members.Add(CreateClearMethod());
            type.Members.Add(CreateClearAllMethod());
            return type;
        }

        private static CodeTypeMember CreateGetMethod()
        {
            var getVariableMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Name = "GetValue",
                ReturnType = new CodeTypeReference("T")
            };
            getVariableMethod.TypeParameters.Add(new CodeTypeParameter("T"));
            getVariableMethod.Parameters.Add(
                new CodeParameterDeclarationExpression("this AlexaRequestInformation<APLSkillRequest>", "request"));
            getVariableMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "key"));

            var getVariable = new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression("request"), "State"), "GetSession<T>",
                new CodeVariableReferenceExpression("key"));

            getVariableMethod.Statements.Add(new CodeMethodReturnStatement(getVariable));
            return getVariableMethod;
        }

        private static CodeTypeMember CreateIncreaseMethod()
        {
            var method = new CodeMemberMethod
            {
                Name = "Increase",
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };
            method.Parameters.Add(
                new CodeParameterDeclarationExpression("this AlexaRequestInformation<APLSkillRequest>", "request"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "amount"));

            method.Statements.Add(
                new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "target",
                    new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<int>"
                    , new CodeVariableReferenceExpression("name"))));
            method.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"),
                "SetValue",
                new CodeVariableReferenceExpression("name"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("target"),
                    CodeBinaryOperatorType.Add, new CodeVariableReferenceExpression("amount"))));

            return method;
        }

        private static CodeTypeMember CreateDecreaseMethod()
        {
            var method = new CodeMemberMethod
            {
                Name = "Decrease",
                Attributes = MemberAttributes.Public | MemberAttributes.Static
            };
            method.Parameters.Add(
                new CodeParameterDeclarationExpression("this AlexaRequestInformation<APLSkillRequest>", "request"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "amount"));

            method.Statements.Add(
                new CodeVariableDeclarationStatement(new CodeTypeReference("var"), "target",
                    new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"), "GetValue<int>"
                        , new CodeVariableReferenceExpression("name"))));
            method.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("request"),
                "SetValue",
                new CodeVariableReferenceExpression("name"),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("target"),
                    CodeBinaryOperatorType.Subtract, new CodeVariableReferenceExpression("amount"))));

            return method;
        }

        private static CodeTypeMember CreateClearMethod()
        {
            var method = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                Name = "Clear"
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression("this AlexaRequestInformation<APLSkillRequest>", "request"));
            method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            method.Statements.Add(new CodeSnippetStatement("request.State.Session.Attributes.Remove(name);"));
            return method;
        }

        private static CodeTypeMember CreateClearAllMethod()
        {
            var method = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                Name = "ClearAll"
            };

            method.Parameters.Add(
                new CodeParameterDeclarationExpression("this AlexaRequestInformation<APLSkillRequest>", "request"));

            method.Statements.Add(new CodeSnippetStatement(
                "var attributes = request.State.Session.Attributes;"));
            method.Statements.Add(new CodeSnippetStatement("foreach(var key in attributes.Keys.Where(k => k.StartsWith(\"game_\")).ToArray()){"));
            method.Statements.Add(new CodeSnippetStatement("attributes.Remove(key);"));
            method.Statements.Add(new CodeSnippetStatement("}"));
            return method;
        }

        private static CodeTypeMember CreateSetMethod()
        {
            var setVariableMethod = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Name = "SetValue",
            };
            setVariableMethod.Parameters.Add(
                new CodeParameterDeclarationExpression("this AlexaRequestInformation<APLSkillRequest>", "request"));
            setVariableMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "key"));
            setVariableMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(typeof(object), "value"));
            var setVariable = new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression("request"), "State"), "SetSession",
                new CodeVariableReferenceExpression("key"),
                new CodeVariableReferenceExpression("value"));

            setVariableMethod.Statements.Add(setVariable);
            return setVariableMethod;
        }

        private static CodeCompileUnit CreateStateFile(CodeGeneratorContext context, CodeTypeDeclaration mainClass)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Linq"));
            code.Namespaces.Add(ns);

            ns.Types.Add(mainClass);
            return code;
        }
    }
}
