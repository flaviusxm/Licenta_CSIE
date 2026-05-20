using System.Threading.Tasks;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface ILocalAiEngine
    {
        Task<(bool IsSafe, string Reason)> AnalyzeTextAsync(string content, string prompt, string model = "qwen2.5:0.5b");
        Task<(bool IsValid, string Details, string Recommendation)> AnalyzeImageAsync(byte[] imageBytes, string prompt, string model = "llama3.2-vision");
    }
}
