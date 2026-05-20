using AskNLearn.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AskNLearn.Infrastructure.Services
{
    public class LocalFileService : IFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LocalFileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder)
        {
            if (string.IsNullOrEmpty(_webHostEnvironment.WebRootPath))
            {
                throw new InvalidOperationException("WebRootPath is not configured.");
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Sanitize filename to prevent path traversal or invalid path characters
            var safeFileName = Path.GetFileName(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var destinationStream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(destinationStream);
            }

            // Return relative URL for web access
            return $"/uploads/{folder}/{uniqueFileName}";
        }

        public void DeleteFile(string filePath)
        {
            var absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/'));
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }

        public async Task<byte[]> ReadFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) 
                throw new FileNotFoundException($"[FileService] Empty file path provided.");

            // Determine root (main web app's wwwroot)
            var root = _webHostEnvironment?.WebRootPath;
            
            if (string.IsNullOrEmpty(root))
            {
                var baseDir = Directory.GetCurrentDirectory();
                root = Path.GetFullPath(Path.Combine(baseDir, "wwwroot"));
            }

            var absolutePath = Path.Combine(root, filePath.TrimStart('/'));
            if (!File.Exists(absolutePath)) 
                throw new FileNotFoundException($"[FileService] File not found at: {absolutePath} (relative: {filePath})");
            
            return await File.ReadAllBytesAsync(absolutePath);
        }
    }
}
