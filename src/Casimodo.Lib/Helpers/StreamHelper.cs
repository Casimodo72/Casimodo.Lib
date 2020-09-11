using System;
using System.IO;

namespace Casimodo.Lib
{
    // KABU: TODO: Use a dedicated foreign open-source lib.
    public static class StreamHelper
    {
        public static void CopyStream(Stream source, Stream destination)
        {
            CopyStream(source, destination, 4096);
        }

        public static void CopyStream(Stream source, Stream destination, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");

            int bytesRead = 0;
            byte[] buffer = new byte[bufferSize];

            while ((bytesRead = source.Read(buffer, 0, bufferSize)) != 0)
                destination.Write(buffer, 0, bytesRead);
        }

        public static void CopyStream(StreamReader source, StreamWriter destination)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");

            int bytesRead = 0;
            const int bufferSize = 4096;
            char[] buffer = new char[bufferSize];

            while ((bytesRead = source.Read(buffer, 0, bufferSize)) != 0)
                destination.Write(buffer, 0, bytesRead);
        }

        private static void StreamToFile(Stream inputStream, string outputFile, FileMode fileMode)
        {
            if (inputStream == null)
                throw new ArgumentNullException("inputStream");

            if (string.IsNullOrEmpty(outputFile))
                throw new ArgumentNullException("outputFile");

            using (FileStream outputStream = new FileStream(outputFile, fileMode, FileAccess.Write))
            {
                int bytesRead = 0;
                const int bufferSize = 4096;
                byte[] buffer = new byte[bufferSize];

                while ((bytesRead = inputStream.Read(buffer, 0, bufferSize)) != 0)
                    outputStream.Write(buffer, 0, bytesRead);
            }
        }
    }
}