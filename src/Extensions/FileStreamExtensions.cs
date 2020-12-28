using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Celeste.GameFiles.Tools.Extensions
{
    public static class FileStreamExtensions
    {
        private static readonly ArrayPool<byte> bufferPool = ArrayPool<byte>.Shared;

        public static async Task BufferedCopyToAsync(this FileStream source, FileStream destination)
        {
            var buffer = bufferPool.Rent(81920);
            try
            {
                int bytesRead;

                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    await destination.WriteAsync(buffer, 0, bytesRead);
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }
    }
}
