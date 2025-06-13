using Microsoft.AspNetCore.Components.Forms;
using System;
using System.IO;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Web.Services
{
    public class FileService
    {
        public async Task<byte[]> ProcessScreenshotAsync(IBrowserFile file)
        {
            if (file == null)
                return null;

            // Limit file size to 5MB
            var maxFileSize = 5 * 1024 * 1024;
            var buffer = new byte[file.Size > maxFileSize ? maxFileSize : file.Size];

            using (var stream = file.OpenReadStream(maxFileSize))
            {
                await stream.ReadAsync(buffer);
            }

            return buffer;
        }

        public string GetImageDataUrl(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            return $"data:image/png;base64,{Convert.ToBase64String(imageData)}";
        }
    }
}