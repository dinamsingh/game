using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class SyntaxCheck
{
    public static int Main(string[] args)
    {
        int errors = 0, files = 0;
        foreach (string path in Directory.GetFiles(args[0], "*.cs", SearchOption.AllDirectories))
        {
            files++;
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9).WithPreprocessorSymbols("UNITY_EDITOR");
            foreach (Diagnostic diagnostic in CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path: path).GetDiagnostics())
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error) continue;
                Console.WriteLine(diagnostic); errors++;
            }
        }
        Console.WriteLine($"C# syntax: {files} files, {errors} errors. Unity API/type checks still require the Editor.");
        return errors == 0 ? 0 : 1;
    }
}
