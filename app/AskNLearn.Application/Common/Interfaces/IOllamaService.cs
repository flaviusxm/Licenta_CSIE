using System.Threading.Tasks;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface IOllamaService
    {
        Task<(bool IsSafe, string Reason)> AnalyzeContentAsync(string content, string? title = null);
    }
}
