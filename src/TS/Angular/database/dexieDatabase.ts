import Dexie from "dexie"
import { RepositoryBase } from "./repositories"

export type DexieSchema = { [tableName: string]: string }

export interface DexieDatabaseSettings {
    readonly indexDbName: string
    readonly schema?: DexieSchema
}

export class DexieDatabase extends Dexie {
    #schema: DexieSchema

    constructor(databaseName: string, schema: DexieSchema) {
        super(databaseName)

        this.#schema = schema
    }

    initialize(schema: DexieSchema | null, repositoryContainers: any[]) {
        const effectiveSchema = Object.assign(
            this.#schema ?? {},
            schema ?? {},)

        const repositories: RepositoryBase[] = []

        for (const container of repositoryContainers) {
            for (const key in container) {
                const repo = container[key]
                if (repo instanceof RepositoryBase) {
                    repositories.push(repo)
                }
            }
        }

        // Initialize schema for tables of repositories.
        for (const repo of repositories) {
            effectiveSchema[repo.tableName] = repo.tableKeys
        }

        // Define tables and indexes
        // (Here's where the implicit table props are dynamically created)
        // TODO: Make DB version setable by consumer.
        this.version(1).stores(effectiveSchema)

        for (const repo of repositories) {
            repo._setTable(this.table(repo.tableName))
        }
    }
}
