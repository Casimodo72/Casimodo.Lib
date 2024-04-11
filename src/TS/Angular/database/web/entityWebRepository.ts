import { lastValueFrom } from "rxjs"

import { fixupReceivedDataDeep } from "@lib/data/utils"
import { JsonPatchOperation } from "@lib/data"
import { IEntityCore } from "@lib/data/entityBase"

import { EntityRepository } from "../repositories/entityRepository"

export interface IEntityWebApiResult<TEntity> {
    readonly hasSucceeded: boolean
    readonly data?: TEntity | TEntity[]
}

export interface EntityWebRepositoryOptions {
    isPutDisabled?: boolean
}

export class EntityWebRepository<TEntity extends Partial<IEntityCore>>{
    readonly #repository: EntityRepository<TEntity>
    readonly url: string
    readonly options: EntityWebRepositoryOptions

    constructor(repository: EntityRepository<TEntity>, url: string, options?: EntityWebRepositoryOptions) {
        this.#repository = repository
        this.url = url
        this.options = options ?? {}
    }

    get #db() {
        return this.#repository.dbcore
    }

    protected get http() {
        return this.#db.http
    }

    protected async _performQuery<TData>(query: () => Promise<TData>): Promise<IEntityWebApiResult<TData>> {
        try {
            const data = await query()

            return {
                hasSucceeded: true,
                data: data as TData
            }
        } catch (error) {
            return {
                hasSucceeded: false
            }
        }
    }

    async trySend(entity: Partial<TEntity>) {
        const entityState = await this.#db.entityStates.getOrAddNew(entity.Id!, this.#repository)

        if (!entityState.dirty) {
            return
        }

        if (!entityState.remotelyCreatedOn || !entityState.patches?.length) {
            // The new entity was not sent yet to the server. Perform a put.
            const result = await this._tryPutRange([entity])
            if (result.hasSucceeded) {
                // TODO: We need to remove entity._isSyncPending here and update it in the DB.
                await this.#db.entityStates.markAsRemotelyPut([entity as IEntityCore], this.#repository)
            }
        } else if (entityState.patches?.length) {
            // Patch.
            const result = await this.#tryPatch(entity.Id!, entityState.patches)
            if (result.hasSucceeded) {

                await this.#db.entityStates.markAsRemotelyPatched([entity as IEntityCore], this.#repository)
            }
        }
    }

    async tryPutAllDirties(ofUser: boolean = true): Promise<boolean> {
        const dirtyEntityStates = await this.#repository.dbcore.entityStates.getDirties(this.#repository, ofUser)
        if (!dirtyEntityStates.length) {
            return true
        }

        const entityIds = dirtyEntityStates.map(x => x.entityId)
        const dirtyEntities = await this.#repository.itemsById(entityIds)
        if (!dirtyEntities.length) {
            // TODO: Ensure stale entity states are deleted.
            return true
        }

        const result = await this._tryPutRange(dirtyEntities)

        if (result.hasSucceeded) {
            await this.#db.entityStates.markAsRemotelyPut(dirtyEntities as IEntityCore[], this.#repository)
        }

        return result.hasSucceeded
    }

    protected async _tryPutRange(items: Partial<TEntity>[]): Promise<IEntityWebApiResult<undefined>> {
        try {
            const clones = []
            for (const item of items) {
                const clone = { ...item }
                delete (clone as any)._isSyncPending
                clones.push(clone)
            }

            await lastValueFrom(this.http.put<TEntity>(`api/${this.url}/putRange`, clones))

            for (const item of items) {
                delete (item as any)._isSyncPending
            }

            return {
                hasSucceeded: true
            }
        } catch (error) {
            return {
                hasSucceeded: false
            }
        }
    }

    async #tryPatch(id: string, patches: JsonPatchOperation[]): Promise<IEntityWebApiResult<TEntity>> {
        try {
            const result = await lastValueFrom(this.http.patch<TEntity>(`api/${this.url}/${id}`, patches))

            fixupReceivedDataDeep(result)

            return {
                hasSucceeded: true,
                data: result
            }
        } catch (error) {
            return {
                hasSucceeded: false
            }
        }
    }
}
