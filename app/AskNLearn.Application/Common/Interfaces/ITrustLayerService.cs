using System.Threading.Tasks;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface ITrustLayerService
    {
        Task<(bool IsSafe, string Reason)> ModerateTextAsync(string content, string? title = null);
        Task<(bool IsValid, string Details, string Recommendation)> VerifyDocumentAsync(byte[] imageBytes);
    }
}
