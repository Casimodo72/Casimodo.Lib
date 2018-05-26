
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Casimodo.Lib.CSharp
{
    public class CSharpLanguageService
    {
        private static readonly LanguageVersion MaxLanguageVersion = Enum
            .GetValues(typeof(LanguageVersion))
            .Cast<LanguageVersion>()
            .Max();

        public CShardCompilationResult Compile(string code, params Type[] types)
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

        CSharpCompilation CompileCore(Type[] types, params SyntaxTree[] syntaxTrees)
        {
            string assemblyName = System.IO.Path.GetRandomFileName();
            var referenceTypes = new List<Type>();

            referenceTypes.Add(typeof(object));
            referenceTypes.Add(typeof(Enumerable));
            if (types != null)
                foreach (var type in types)
                    if (!referenceTypes.Contains(type))
                        referenceTypes.Add(type);

            var references = referenceTypes.Select(x => MetadataReference.CreateFromFile(x.Assembly.Location)).ToList();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: syntaxTrees,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }

        public class CShardCompilationResult
        {
            public Assembly Assembly { get; set; }
            public bool IsSuccess { get; set; }
            public List<string> ErrorMessages { get; set; }
        }

        public CShardCompilationResult EmitToAssembly(CSharpCompilation compilation)
        {
            var result = new CShardCompilationResult();

            using (var ms = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {

                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    result.ErrorMessages = failures.Select(x => x.GetMessage()).ToList();

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