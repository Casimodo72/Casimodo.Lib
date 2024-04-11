export interface IEntityCore {
    Id: string
    _isSyncPending?: boolean
    CreatedOn: Date
    CreatedBy: string
    CreatedByUserId: string
    ModifiedOn: Date
    ModifiedBy: string
    ModifiedByUserId: string
    IsDisabled?: boolean
}

export abstract class EntityCore implements IEntityCore {
    Id!: string
    CreatedOn!: Date
    CreatedBy!: string
    CreatedByUserId!: string
    ModifiedOn!: Date
    ModifiedBy!: string
    ModifiedByUserId!: string
}

export interface IDeletableEntityCore extends IEntityCore {
    IsDeleted?: boolean
    DeletedOn?: Date | null
    DeletedBy?: string | null
    DeletedByUserId?: string | null
    DeletedByDeviceId?: string | null
}

export class DeletableEntityCore extends EntityCore implements IDeletableEntityCore {
    IsDeleted?: boolean
    DeletedOn?: Date | null
    DeletedBy?: string | null
    DeletedByUserId?: string | null
    DeletedByDeviceId?: string | null
}

export interface IEntityBase extends IDeletableEntityCore {
}

export abstract class EntityBase extends DeletableEntityCore implements IEntityBase {
}

// The backend's full entity base - just for a quick dev look.
// eslint-disable-next-line @typescript-eslint/no-unused-vars
interface IBackendEntityBase {
    /**
     * Is Key
     */
    Id: string | null;
    IsNotDeletable: boolean;
    IsReadOnly: boolean;
    IsDisabled: boolean;
    IsInvalid: boolean;
    IsNested: boolean;
    CreatedOn: Date | null;
    CreatedBy: string | null;
    CreatedByUserId: string | null;
    CreatedByDeviceId: string | null;
    ModifiedOn: Date | null;
    ModifiedBy: string | null;
    ModifiedByUserId: string | null;
    ModifiedByDeviceId: string | null;
    TouchedOn: Date | null;
    IsDeleted: boolean;
    DeletedOn: Date | null;
    DeletedBy: string | null;
    DeletedByUserId: string | null;
    DeletedByDeviceId: string | null;
    IsSelfDeleted: boolean;
    SelfDeletedOn: Date | null;
    SelfDeletedBy: string | null;
    SelfDeletedByUserId: string | null;
    SelfDeletedByDeviceId: string | null;
    IsCascadeDeleted: boolean;
    CascadeDeletedOn: Date | null;
    CascadeDeletedByOriginTypeId: string | null;
    CascadeDeletedByOriginId: string | null;
    CascadeDeletedBy: string | null;
    CascadeDeletedByUserId: string | null;
    CascadeDeletedByDeviceId: string | null;
    IsRecyclableDeleted: boolean;
    RecyclableDeletedOn: Date | null;
    RecyclableDeletedByDeviceId: string | null;
    RecyclableDeletedBy: string | null;
    RecyclableDeletedByUserId: string | null;
    CloudUpOn: Date | null;
}
