using System;

namespace Casimodo.Lib.Data
{
    public interface IUploadedFileProvider
    {
        string UploadPath { get; }

        object Find(Guid guid);

        byte[] ReadData(Guid guid);

        string GetValidUploadRootDirPath();

        string GetUri(string storeFileName);

        void Add(string userId, IDbFileInfo item, string storeFilePath);

        void Remove(string userId, string kind, string[] fileNames);

        // TODO: Delete(Guid guid);
    }
}