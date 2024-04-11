import { Database, EntityState, Repository, createEntityStateDeltaOnPut } from "@lib/database"
import { IEntityCore, ValidationResult, convertDeltaToPatches } from "@lib/data"

interface EntityStateChanges {
    delta: Partial<EntityState>
}

export class EntityStateRepository extends Repository<EntityState, [string, string, string]> {
    override readonly tableName = "entityStates"
    override readonly tableKeys = "[typeId+userId+entityId], [typeId+userId+dirty], [typeId+dirty]"
    readonly #db: Database<any, any>

    constructor(db: Database<any, any>) {
        super()
        this.dbcore = db
        this.#db = db
    }

    override _setTable(table: any) {
        this.table = table
    }

    /** Returns an existing entity state or creates a new one and adds it. */
    async getOrAddNew(entityId: string, repository: Repository): Promise<EntityState> {
        return await this.table.get(this.#toTableKey(entityId, repository))
            ?? await this.#addNew(entityId, repository)
    }

    /** Returns user-scoped entity-states with dirty === 1. */
    async getDirties(repository: Repository, ofUser: boolean = true): Promise<EntityState[]> {
        const query: Partial<EntityState> = {
            typeId: repository.entityTypeId,
            userId: ofUser
                ? this.dbcore.getRequiredCurrentUserId()
                : undefined,
            dirty: 1
        }

        return await this.table.where(query).toArray()
    }

    /** Returns user-scoped entity-states with dirty === 1. */
    async items(repository: Repository): Promise<EntityState[]> {
        const query: Partial<EntityState> = {
            typeId: repository.entityTypeId,
            userId: this.dbcore.getRequiredCurrentUserId()
        }

        return await this.table.where(query).toArray()
    }

    async find(entityId: string, repository: Repository): Promise<EntityState | undefined> {
        return await this.table.get(this.#toTableKey(entityId, repository))
    }

    async #addNew(entityId: string, repository: Repository): Promise<EntityState> {
        const entityTypeName = this.#db.getEntityTypeNameById(repository.entityTypeId)
        const userId = this.#db.getRequiredCurrentUserId()
        const entityState = new EntityState(repository.entityTypeId, userId, entityId, entityTypeName)

        await this.table.add(entityState)

        return entityState
    }

    async setValidationResult(entityId: string, validationResult: ValidationResult, repository: Repository): Promise<void> {
        await this.modify(entityId, repository, async (changes, currentState) => {
            const valid = currentState.validity === "valid"
                ? true
                : currentState.validity === "invalid"
                    ? false
                    : null

            if (valid !== validationResult.valid) {
                changes.delta = {
                    validity: validationResult.valid ? "valid" : "invalid"
                }

                await this.#db.entityIssues.modify(entityId, repository, async (changes) => {
                    changes.delta = {
                        issues: validationResult.valid
                            ? undefined
                            : validationResult.issues
                    }
                })
            }
        })
    }

    async getValidationResult(entityId: string, repository: Repository): Promise<ValidationResult> {
        const entityState = await this.getOrAddNew(entityId, repository)

        // TODO: Reconsider returning true if the validity is unknown.
        if (!entityState.validity || entityState.validity === "valid") {
            return {
                valid: true
            }
        } else {
            const entityIssue = await this.#db.entityIssues.find(entityId, repository)

            return {
                valid: false,
                issues: entityIssue?.issues
            }
        }
    }

    async markAsRemotelyPut(entities: IEntityCore[], repository: Repository) {
        for (const entity of entities) {
            await this.modify(entity.Id!, repository, async (changes, _currentState) => {
                changes.delta = createEntityStateDeltaOnPut(entity)
            })
            // TODO: We need to remove entity._isSyncPending here and update it in the DB.
        }
    }

    async addPatch(entity: IEntityCore, repository: Repository, entityDelta: object) {
        const currentState = await this.getOrAddNew(entity.Id!, repository)

        const patches = convertDeltaToPatches(entityDelta)

        const delta: Partial<EntityState> = {
            // TODO: REMOVE: downloadedOn: undefined,
            // remotelyCreatedOn: entity.CreatedOn ?? undefined,
            // remotelyModifiedOn: entity.ModifiedOn ?? undefined,
            locallyModifiedOn: entity.ModifiedOn!
        }

        if (!currentState.dirty) {
            delta.dirty = 1
        }

        delta.patches = currentState.patches ?? []
        delta.patches.push(...patches)

        await this.table.update(this.#toTableKey(entity.Id!, repository), delta)
    }

    async markAsRemotelyPatched(entities: IEntityCore[], repository: Repository) {
        const now = new Date()
        for (const entity of entities) {
            await this.modify(entity.Id, repository, async (changes, _currentState) => {
                changes.delta = {
                    remotelyPatchedOn: now,
                    dirty: 0,
                    patches: undefined
                }
            })
            // TODO: We need to remove entity._isSyncPending here and update it in the DB.
        }
    }

    async markAsDirty(entityId: string, modifiedOn: Date, repository: Repository): Promise<void> {
        await this.modify(entityId, repository, async (changes, currentState) => {
            if (currentState.dirty === 1 &&
                (currentState.locallyModifiedOn != null && currentState.locallyModifiedOn >= modifiedOn)
            ) {
                return
            }

            changes.delta.dirty = 1
            changes.delta.locallyModifiedOn = modifiedOn
        })
    }

    async modify(
        entityId: string,
        repository: Repository,
        action: (changes: EntityStateChanges, entityState: EntityState) => Promise<void>
    ): Promise<void> {

        const entityState = await this.getOrAddNew(entityId, repository)
        const changes = { delta: {} }

        await action(changes, entityState)

        if (!changes.delta || !Object.keys(changes.delta).length)
            return

        await this.table.update(this.#toTableKey(entityId, repository), changes.delta)
    }

    async delete(id: string, repository: Repository): Promise<void> {
        await this.#db.entityIssues.delete(id, repository)
        await this.table.delete(this.#toTableKey(id, repository))
    }

    async deleteRange(ids: string[], repository: Repository): Promise<void> {
        if (!ids.length) return

        await this.#db.entityIssues.deleteRange(ids, repository)
        await this.table.bulkDelete(ids.map(id => this.#toTableKey(id, repository)))
    }

    override async _deleteTable() {
        await this.table.clear()
    }

    #toTableKey(entityId: string, repository: Repository): [string, string, string] {
        return [repository.entityTypeId, this.dbcore.getRequiredCurrentUserId(), entityId]
    }
}
