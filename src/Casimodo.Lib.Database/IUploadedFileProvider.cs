using System;

namespace Casimodo.Lib.Data
{
    public interface IUploadedFileProvider
    {
        string UploadPath { get; }

        object GetFileInfo(Guid guid, bool required = true);

        byte[] ReadData(Guid guid);

        byte[] ReadDataAndRemove(Guid guid);

        string GetValidUploadRootDirPath();

        void Add(Guid userId, IDbFileInfo item, string storeFilePath);

        bool Remove(Guid fileId);

        // KABU TODO: REMOVE? Not used. This was intended to be called only by the KendoUpload widget.
        //void Remove(string userId, string kind, string[] fileNames);
    }
}