using System;
using System.Threading.Tasks;

namespace Casimodo.Lib.Data
{
    public interface IUploadedFileProvider
    {
        string GetUploadPath();

        object GetFileInfo(Guid guid, bool required = true);

        Task<byte[]> ReadDataAsync(Guid guid);

        Task<byte[]> ReadDataAndRemoveAsync(Guid guid);

        string GetValidUploadRootDirPath();

        void Add(Guid userId, IDbFileInfo item, string storeFilePath);

        bool Remove(Guid fileId);
    }
}