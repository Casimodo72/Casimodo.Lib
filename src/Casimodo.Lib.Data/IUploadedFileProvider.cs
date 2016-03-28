using System;

namespace Casimodo.Lib.Data
{
    public interface IUploadedFileProvider
    {
        object Find(Guid guid);

        byte[] ReadData(Guid guid);

        string GetValidUploadRootDirPath();

        string GetUri(string storeFileName);

        // TODO: Delete(Guid guid);
    }
}