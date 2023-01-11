using Microsoft.CodeAnalysis;

namespace WebCSC
{
    public record CompileResult(bool Success,string? Message=null,IEnumerable<SimpleDiagnostic>? Diagnostics = null);

    public record SimpleDiagnostic(string id,string message, string severity,int startLineNumber)
    {
        public static SimpleDiagnostic Create(Diagnostic d)
        {
            string message = d.GetMessage();
            string severity = d.Severity.ToString();
            int startLineNumber = d.Location.GetLineSpan().StartLinePosition.Line;
            return new SimpleDiagnostic(d.Id,message,severity,startLineNumber);
        }
        public static IEnumerable<SimpleDiagnostic> Create(IEnumerable<Diagnostic> items)
        {
            foreach(var e in items)
            {
                yield return Create(e);
            }
        }
    }
}
