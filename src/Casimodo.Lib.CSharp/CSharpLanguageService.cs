using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Casimodo.Lib.CSharp
{
    public class CSharpCompiledToAssemblyWrapper
    {
        public Assembly Assembly { get; set; }
        public bool IsSuccess { get; set; }
        public List<string> ErrorMessages { get; set; }
    }

    public class CSharpScriptOptionsWrapper
    {
        internal ScriptOptions Options { get; set; }
    }

    public class CSharpScriptWrapper
    {
        public string Code { get; set; }
        internal Script<object> _script;
        public bool IsSuccess { get; set; }
        public List<string> ErrorMessages { get; set; }
        public Func<object, Task<object>> RunAsync { get; set; }
    }

    public class CSharpLanguageServiceWrapper
    {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof(LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();

        public CSharpScriptWrapper CompileScript(string code, CSharpScriptOptionsWrapper options, Type globalsType)
        {
            Script<object> script = CSharpScript.Create(code, options: options.Options, globalsType: globalsType);

            var diagnostics = script.Compile();

            var result = new CSharpScriptWrapper();
            result.Code = code;
            result._script = script;

            var errors = diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error)
                .ToList();

            result.IsSuccess = errors.Count == 0;
            result.ErrorMessages = errors.Select(x => x.GetMessage()).ToList();
            result.RunAsync = async (globals) =>
            {
                var state = await result._script.RunAsync(globals);
                return state.ReturnValue;
            };

            return result;
        }

        public SyntaxTree Parse(string code, SourceCodeKind kind = SourceCodeKind.Regular)
        {
            var options = new CSharpParseOptions(kind: kind, languageVersion: MaxLanguageVersion);

            // Return a syntax tree of our source code
            return CSharpSyntaxTree.ParseText(code, options);
        }

        public CSharpScriptOptionsWrapper CreateScriptOptions(Type[] types, params string[] namespaces)
        {
            return new CSharpScriptOptionsWrapper
            {
                Options = CreateScriptOptionsCore(types, namespaces)
            };
        }

        public ScriptOptions CreateScriptOptionsCore(Type[] types, params string[] namespaces)
        {
            var options = ScriptOptions.Default;
            var referenceTypes = new List<Type>
            {
                typeof(Enumerable)
            };

            if (types != null)
                foreach (var type in types)
                    if (!referenceTypes.Contains(type))
                        referenceTypes.Add(type);

            options = options
                .AddReferences(referenceTypes.Select(x => GetAssembly(x)))
                .AddImports("System", "System.Linq", "System.Collections.Generic");

            if (namespaces != null)
                options = options.AddImports(namespaces);

            return options;
        }

        Assembly GetAssembly(Type type)
        {
            return type.GetTypeInfo().Assembly;
        }

        public CSharpCompilation CompileCore(Type[] types, params SyntaxTree[] syntaxTrees)
        {
            string assemblyName = Path.GetRandomFileName();
            var referenceTypes = new List<Type>
            {
                typeof(object),
                typeof(Enumerable)
            };
            if (types != null)
                foreach (var type in types)
                    if (!referenceTypes.Contains(type))
                        referenceTypes.Add(type);

            var references = referenceTypes.Select(x => MetadataReference.CreateFromFile(x.Assembly.Location)).ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: syntaxTrees,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }
    }
}