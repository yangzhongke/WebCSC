using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.JSInterop;
using System.Reflection;

namespace WebCSC;
public static class WebCSCMain
{
    public static IJSInProcessRuntime JSInProcRuntime;
    private static HttpClient httpClient;

    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        //BaseAddress is required in Web Assembly.
        builder.Services.AddScoped(sp =>
   new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        var host = builder.Build();
        JSInProcRuntime = (IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>();
        httpClient = host.Services.GetRequiredService<HttpClient>();
        await host.RunAsync();
    }

    private static MethodInfo GetEntryMethod(Assembly asm)
    {
        Type type = asm.GetType("Script");
        return type.GetMethod("<Factory>", BindingFlags.Static | BindingFlags.Public);
    }

    private static async Task<(CompileResult Result, Assembly? Assembly)> CompileAsync(string code, CSCOptions? options=null)
    {
        List<MetadataReference> references = new List<MetadataReference>();
        string[] defaultLibs = { "/_framework/System.dll","/_framework/System.Buffers.dll","/_framework/System.Collections.dll","/_framework/System.Core.dll","/_framework/System.Runtime.dll","/_framework/System.IO.dll","/_framework/System.Linq.dll","/_framework/System.Linq.Expressions.dll","/_framework/System.Linq.Parallel.dll","/_framework/mscorlib.dll","/_framework/System.Private.CoreLib.dll"};
        List<Stream> libSteams = new List<Stream>();
        try
        {
            List<string> libraries = new (defaultLibs);
            if (options!=null&& options.Libraries!=null)
            {
                libraries.AddRange(options.Libraries);
            }
            foreach (var libPath in libraries)
            {
                var referenceStream = await httpClient.GetStreamAsync(libPath);
                libSteams.Add(referenceStream);
                references.Add(MetadataReference.CreateFromStream(referenceStream));
            }
            //WebAssembly doesn't support concurrentBuild，so 'concurrentBuild:false' is required, else 'System.PlatformNotSupportedException: Cannot wait on monitors on this runtime' will be thrown.
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false)
                .WithUsings("System", "System.Text", "System.Collections.Generic", "System.IO", "System.Linq", "System.Threading", "System.Threading.Tasks");
            CSharpParseOptions parserOptions = CSharpParseOptions.Default.
                WithLanguageVersion(LanguageVersion.Latest).WithKind(SourceCodeKind.Script);
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code, parserOptions);
            var scriptCompilation = CSharpCompilation.CreateScriptCompilation(
            "main.dll", syntaxTree,
           options: compilationOptions).AddReferences(references);
            using MemoryStream stream = new MemoryStream();
            var emitResult = scriptCompilation.Emit(stream);
            stream.Position = 0;
            return ProcessResult(stream, emitResult);
        }
        finally
        {
            foreach(var stream in libSteams)
            {
                stream.Dispose();
            }
        }
    }

    private static (CompileResult Result, Assembly Assembly) ProcessResult(MemoryStream stream, EmitResult emitResult)
    {
        if (emitResult.Success)
        {
            Assembly asm = Assembly.Load(stream.ToArray());
            return (new CompileResult(true), asm);
        }
        else
        {
            var msgs = emitResult.Diagnostics.Select(d => d.ToString()); ;
            string msg = string.Join('\n', msgs);
            return (new CompileResult(false, msg, SimpleDiagnostic.Create(emitResult.Diagnostics)), null);
        }
    }

    [JSInvokable]
    public static async Task<CompileResult> Check(string code, CSCOptions? options = null)
    {
        (var result, var _) = await CompileAsync(code, options);
        return result;
    }

    [JSInvokable]
    public static async Task<CompileResult> Run(string code, CSCOptions? options = null)
    {
        (var result, var asm) = await CompileAsync(code, options);
        if (result.Success)
        {
            MethodInfo entryMethod = GetEntryMethod(asm!);
            var compileResult = (Task)entryMethod.Invoke(null,new object[] { new object[2] });
            await compileResult;
            return new CompileResult(true);
        }
        else
        {
            return result;
        }
    }
}