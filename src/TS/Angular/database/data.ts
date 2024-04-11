import { IEntityCore, IssueEntityNode, JsonPatchOperation } from "@lib/data"

export abstract class AppDataEntry {
    readonly id: string

    constructor(id: string) {
        this.id = id
    }
}

export class AppStates extends AppDataEntry {
    static readonly ID = "03233d6f-7bac-4557-a40e-44441b429f48"

    constructor() {
        super(AppStates.ID)
    }

    jobAppVersion?: string | undefined

    // TODO: IMPORTANT: Reanimate because we can't use the app offline without it.
    // currentUser: AuthenticatedAppUser | null = null
}

/**
 * The synchronization state of all entities of a specific entity type.
 */
export class EntityTypeState {
    /** The entity type's ID. Part of the DB key. */
    readonly id: string
    readonly typeName: string
    /** The company-ID if in company scope.  Part of the DB key. */
    readonly companyId: string
    /** The user-ID if in user scope.  Part of the DB key. */
    readonly userId: string
    /** The time of last download of the entities. */
    lastDownloadedOn?: Date | null = null
    /** The maximum of ModifiedOn of downloaded entities. */
    lastModifiedOn?: Date | null = null
    /** The maximum of DeletedOn of backend entities. */
    remoteLastDeletedOn?: Date | null = null

    constructor(typeId: string, companyId: string, userId: string, entityTypeName: string) {
        this.id = typeId
        this.companyId = companyId
        this.userId = userId

        this.typeName = entityTypeName
    }
}

export type EntityValidityState = undefined | "valid" | "invalid"

export type EntityDirtyValue = 0 | 1

export type DeltaChangeKind = "added" | "modified" | "deleted"

export interface DeltaChangeOptions {
    ownedEntityProps?: string[]
    entityChanged?: (name: string, change: DeltaChangeKind, oldValue: any | undefined, newValue: any) => void
    valueChanged?: (name: string, oldValue: any | null | undefined, newValue: any | null | undefined) => void
}

/** The state of a single modifiable entity. */
export class EntityState {
    /** The entity type's ID. */
    readonly typeId: string
    readonly userId: string
    readonly entityId: string
    dirty: EntityDirtyValue = 0
    /**
     * The entity type's name.
     */
    readonly typeName: string
    /**
     * The entity's dirty state. Must be a number in order to allow for indexing.
     * Plus we have room to extend the dirty state.
     */
    patches?: JsonPatchOperation[]
    /** An undefined validity value means: validation is pending. */
    validity?: EntityValidityState
    // TODO: REMOVE
    // /**
    //  * Date-time of last download from server.
    //  */
    // downloadedOn?: Date
    /**
     * Date-time of last local modification.
     */
    locallyModifiedOn?: Date
    /**
     * Date-time of entity.ModifiedOn if the entity was downloaded from server; undefined otherwise.
     */
    remotelyModifiedOn?: Date
    /**
     * Date-time of last upload to server
     * or entity.CreatedOn if the entity was downloaded from server.
     */
    remotelyCreatedOn?: Date
    /**
     * Date-time of last HTTP PATCH to server.
     * TODO: Might not be needed.
     */
    remotelyPatchedOn?: Date

    constructor(entityTypeId: string, userId: string, entityId: string, entityTypeName: string) {
        this.typeId = entityTypeId
        this.userId = userId
        this.entityId = entityId
        this.typeName = entityTypeName
    }
}

export function createEntityStateDeltaOnDownload(entity: IEntityCore): Partial<EntityState> {
    return {
        dirty: 0,
        patches: undefined,
        validity: undefined,
        // TODO: REMOVE: downloadedOn: new Date(),
        remotelyCreatedOn: entity.CreatedOn,
        remotelyModifiedOn: entity.ModifiedOn,
        locallyModifiedOn: undefined,
    }
}

export function createEntityStateDeltaOnPut(entity: IEntityCore): Partial<EntityState> {
    return {
        dirty: 0,
        patches: undefined,
        //validity: undefined,
        // TODO: REMOVE: downloadedOn: undefined,
        remotelyCreatedOn: entity.CreatedOn,
        remotelyModifiedOn: entity.ModifiedOn,
        locallyModifiedOn: undefined,
    }
}

// TODO: REMOVE? Not used
/**
 * The validation issue (error/warning) of a single modifiable entity.
 */
export interface EntityIssue {
    /**
    * The entity's ID.
    */
    readonly id: string
    /**
     * The entity type's ID.
     */
    readonly typeId: string
    /**
     * The entity type's name.
     */
    readonly typeName: string
    issues: IssueEntityNode
}

export interface IRepositoryDescriptor {
    tableName: string
    keys: string
    entityTypeName: string
    entityTypeId: string
    displayName: string
    isCached?: boolean
}
