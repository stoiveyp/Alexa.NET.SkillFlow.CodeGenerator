﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Alexa.NET.Request;
using Alexa.NET.RequestHandlers.Handlers;
using Alexa.NET.Response;

namespace Alexa.NET.SkillFlow.CodeGenerator
{
    public static class CodeGeneration_Scene
    {
        public static CodeCompileUnit Generate(Scene scene, CodeGeneratorContext context)
        {
            var code = new CodeCompileUnit();
            var ns = new CodeNamespace(context.Options.SafeRootNamespace);
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Request"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.Response"));
            ns.Imports.Add(new CodeNamespaceImport("Alexa.NET.RequestHandlers"));
            ns.Imports.Add(new CodeNamespaceImport("System.Threading.Tasks"));
            code.Namespaces.Add(ns);

            var mainClass = GenerateSceneClass(scene);
            ns.Types.Add(mainClass);
            return code;
        }

        private static CodeTypeDeclaration GenerateSceneClass(Scene scene)
        {
            var mainClass = new CodeTypeDeclaration(SceneClassName(scene.Name))
            {
                IsClass = true,
                Attributes = MemberAttributes.Public
            };

            var method = new CodeMemberMethod
            {
                Name = "Generate",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("async Task")
            };

            method.AddResponseParams(true);

            mainClass.Members.Add(method);
            return mainClass;
        }

        public static string SceneClassName(string sceneName)
        {
            return "Scene_" + char.ToUpper(sceneName[0]) + sceneName.Substring(1).Replace(" ", "_");
        }
    }
}
