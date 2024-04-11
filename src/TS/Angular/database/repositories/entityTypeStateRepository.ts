import { AuthenticatedAppUser } from "@lib/auth"
import { IEntityCore } from "@lib/data"
import { Database, EntityTypeState, Repository } from "@lib/database"
import { DateTime } from "luxon"

interface EntityTypeStateChanges {
    delta: Partial<EntityTypeState>
}

export class EntityTypeStateRepository extends Repository<EntityTypeState, [string, string, string]> {
    override readonly tableName = "entityTypeStates"
    override readonly tableKeys = "[id+companyId+userId]"
    readonly #db: Database<any, any>

    constructor(db: Database<any, any>) {
        super()
        this.dbcore = db
        this.#db = db
    }

    override _setTable(table: any) {
        this.table = table
    }

    /**
     * Computes and saves the youngest modified-on date-time of the specified entities.
     * The modified-on date-time is used for incremental downloads.
     */
    async updateOnDownloaded(repository: Repository, downloadedOn: DateTime, entities: Partial<IEntityCore>[]) {
        let maxModifiedOn: Date | null = null
        if (entities?.length) {
            const maxModifiedDateNumber = Math.max(...entities.map(x => x.ModifiedOn!).map(Number))
            if (!maxModifiedDateNumber) return

            maxModifiedOn = new Date(maxModifiedDateNumber)
        }

        await this.modify(repository,
            async (changes: EntityTypeStateChanges, entityTypeState: EntityTypeState) => {
                changes.delta = {
                    lastDownloadedOn: downloadedOn.toJSDate()
                }
                if (maxModifiedOn != null &&
                    (entityTypeState.lastModifiedOn == null || entityTypeState.lastModifiedOn < maxModifiedOn)
                ) {
                    changes.delta.lastModifiedOn = maxModifiedOn
                }
            })
    }

    /**
     * Gets the entity type state of the specified repository.
     */
    async getOrCreate(repository: Repository): Promise<EntityTypeState> {
        let entityTypeState = await this.table.get(this.#toTableKey(repository))
        if (!entityTypeState) {
            const entityTypeName = this.#db.getEntityTypeNameById(repository.entityTypeId)
            if (!entityTypeName) {
                throw new Error(`Unknown entity type ID: ${repository.entityTypeId}.`)
            }

            let companyId = ""
            let userId = ""
            let authUser: AuthenticatedAppUser | null = null
            if (repository.hasCompanyScope || repository.hasUserScope) {
                authUser = this.dbcore.getRequiredCurrentUser()
                if (repository.hasCompanyScope) {
                    companyId = authUser.CompanyId
                }
                if (repository.hasUserScope) {
                    userId = authUser.Id
                }
            }

            entityTypeState = new EntityTypeState(
                repository.entityTypeId,
                companyId,
                userId,
                entityTypeName
            )

            await this.table.add(entityTypeState)
        }

        return entityTypeState
    }

    // TODO: REMOVE?
    // async update(entityTypeState: EntityTypeState): Promise<void> {
    //     await this.table.update(entityTypeState.id, entityTypeState)
    // }

    // TODO: REMOVE?
    // async setLowerLastModifiedOn(repository: Repository, lastModifiedOn: Date) {
    //     await this.modify(repository,
    //         async (changes: EntityTypeStateChanges, entityTypeState: EntityTypeState) => {
    //             if (entityTypeState.lastModifiedOn === null || entityTypeState.lastModifiedOn > lastModifiedOn) {
    //                 changes.delta = { lastModifiedOn: lastModifiedOn }
    //             }
    //         })
    // }

    async modify(
        repository: Repository,
        action: (changes: EntityTypeStateChanges, entityTypeState: EntityTypeState) => Promise<void>
    ): Promise<void> {
        const entityTypeState = await this.getOrCreate(repository)
        const changes = { delta: {} }

        await action(changes, entityTypeState)

        if (!changes.delta || !Object.keys(changes.delta).length) {
            return
        }

        await this.table.update(this.#toTableKey(repository), changes.delta)
    }

    // TODO: REMOVE?
    // async _resetRemoteDeletionInfo() {
    //     const items = await this.table.toArray()
    //     const delta: Partial<EntityTypeState> = {
    //         remoteLastDeletedOn: null
    //     }
    //     for (const item of items) {
    //         await this.table.update([item.id, item.companyId, item.userId], delta)
    //     }
    // }

    delete(repository: Repository): Promise<void> {
        return this.table.delete(this.#toTableKey(repository))
    }

    override async _deleteTable(): Promise<void> {
        await this.table.clear()
    }

    #toTableKey(repository: Repository): [string, string, string] {
        let companyId = ""
        let userId = ""
        let authUser: AuthenticatedAppUser | null = null
        if (repository.hasCompanyScope || repository.hasUserScope) {
            authUser = this.dbcore.getRequiredCurrentUser()
            if (repository.hasCompanyScope) {
                companyId = authUser.CompanyId
            }
            if (repository.hasUserScope) {
                userId = authUser.Id
            }
        }

        return [repository.entityTypeId, companyId, userId]
    }
}
