
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

        // TODO: REMOVE? Not used
        public CSharpCompiledToAssemblyWrapper Compile(string code, params Type[] types)
        {
            var syntaxTree = Parse(code);
            var compilation = CompileCore(types, syntaxTree);
            var result = EmitToAssembly(compilation);

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
            var referenceTypes = new List<Type>();

            referenceTypes.Add(typeof(Enumerable));

            if (types != null)
                foreach (var type in types)
                    if (!referenceTypes.Contains(type))
                        referenceTypes.Add(type);

            options = options.AddReferences(referenceTypes.Select(x => GetAssembly(x)));

            // TODO: REMOVE?
            // var interactiveLoader = new InteractiveAssemblyLoader();
            // foreach (var ass in referenceTypes.Select(x => GetAssembly(x)))
            //    interactiveLoader.RegisterDependency(ass);

            // var references = referenceTypes.Select(x => MetadataReference.CreateFromFile(x.Assembly.Location)).ToList();
            // if (references.Count != 0)
            //    options = options.AddReferences(references);

            options = options.AddImports("System", "System.Linq", "System.Collections.Generic");;

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
            var referenceTypes = new List<Type>();

            referenceTypes.Add(typeof(object));
            referenceTypes.Add(typeof(Enumerable));
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

        // TODO: REMOVE? Not used and not fully implemented.
        public CSharpCompiledToAssemblyWrapper EmitToAssembly(CSharpCompilation compilation)
        {
            var result = new CSharpCompiledToAssemblyWrapper();

            using (var ms = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {

                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    result.ErrorMessages = failures.Select(x => x.GetMessage()).ToList();

                    // TODO: REMOVE
                    //foreach (Diagnostic diagnostic in failures)
                    //{
                    //    Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    //}
                }
                else
                {
                    result.IsSuccess = true;

                    ms.Seek(0, SeekOrigin.Begin);
                    //AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(ms);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                }
            }

            return result;
        }
    }
}