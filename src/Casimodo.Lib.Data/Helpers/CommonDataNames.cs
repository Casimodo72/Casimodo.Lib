namespace Casimodo.Lib.Data
{
    public static class CommonDataNames
    {
        public const string Id = "Id";

        public const string IsNested = "IsNested";

        public const string CreatedOn = "CreatedOn";
        public const string CreatedBy = "CreatedBy";
        public const string CreatedByUserId = "CreatedByUserId";
        public const string CreatedByDeviceId = "CreatedByDeviceId";

        public const string ModifiedOn = "ModifiedOn";
        public const string ModifiedBy = "ModifiedBy";
        public const string ModifiedByUserId = "ModifiedByUserId";
        public const string ModifiedByDeviceId = "ModifiedByDeviceId";

        public const string IsDeleted = "IsDeleted";
        public const string DeletedOn = "DeletedOn";
        public const string DeletedBy = "DeletedBy";
        public const string DeletedByUserId = "DeletedByUserId";
        public const string DeletedByDeviceId = "DeletedByDeviceId";

        public const string IsCascadeDeleted = "IsCascadeDeleted";
        public const string CascadeDeletedOn = "CascadeDeletedOn";
        public const string CascadeDeletedByOriginTypeId = "CascadeDeletedByOriginTypeId";
        public const string CascadeDeletedByOriginId = "CascadeDeletedByOriginId";
        public const string CascadeDeletedBy = "CascadeDeletedBy";
        public const string CascadeDeletedByUserId = "CascadeDeletedByUserId";
        public const string CascadeDeletedByDeviceId = "CascadeDeletedByDeviceId";

        public const string IsSelfDeleted = "IsSelfDeleted";
        public const string SelfDeletedOn = "SelfDeletedOn";
        public const string SelfDeletedBy = "SelfDeletedBy";
        public const string SelfDeletedByUserId = "SelfDeletedByUserId";
        public const string SelfDeletedByDeviceId = "SelfDeletedByDeviceId";

        public const string IsRecyclableDeleted = "IsRecyclableDeleted";
        public const string RecyclableDeletedOn = "RecyclableDeletedOn";
        public const string RecyclableDeletedBy = "RecyclableDeletedBy";
        public const string RecyclableDeletedByUserId = "RecyclableDeletedByUserId";
        public const string RecyclableDeletedByDeviceId = "RecyclableDeletedByDeviceId";

        public const string CloudUpOn = "CloudUpOn";

        public const string FileStoreNameSuffix = "StoreFileName";
        public const string FileIdSuffix = "Id";
        public const string FileUriSuffix = "Uri";
    }

    public static class MoAttachmentProp
    {
        public const string Attachments = "Attachments";
        public const string OwnerId = "OwnerId";
        public const string OwnerPropertyName = "OwnerPropertyName";
        public const string ModifiedOn = "ModifiedOn";
        public const string Operation = "Operation";

        public const string ContentType = "Content-Type";
        public const string ContentId = "Content-ID";
        public const string ContentTransferEncoding = "Content-Transfer-Encoding"; // E.g. "base64"

        /// <summary>
        /// See http://www.iana.org/assignments/cont-disp/cont-disp.xhtml
        /// See http://tools.ietf.org/html/rfc2183
        /// </summary>
        public const string ContentDisposition = "Content-Disposition";
    }
}