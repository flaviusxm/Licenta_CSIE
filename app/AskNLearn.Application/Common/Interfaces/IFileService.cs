using System.IO;
using System.Threading.Tasks;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder);
        void DeleteFile(string filePath);
    }
}
