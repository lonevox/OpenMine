using System.IO;

namespace OpenMine.Extensions;

public static class StreamExtensions
{
    public static byte[] ReadAllBytes(this Stream stream)
    {
        if (stream is MemoryStream memoryStream)
            return memoryStream.ToArray();

        using (memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
