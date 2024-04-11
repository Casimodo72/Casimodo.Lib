import { inject, signal } from "@angular/core"
import { HttpClient } from "@angular/common/http"
import Dexie from "dexie"

import { AuthService, AuthenticatedAppUser } from "@lib/auth"
import { IEntityCore } from "@lib/data/entityBase"
import { TypeKeys } from "@lib/data/entityTypeKeys"
import { UserNotifiableError } from "@lib/errors"

import { DexieDatabase, DexieSchema } from "./dexieDatabase"
import {
    DataRepository, RepositoriesContainer,
    EntityTypeStateRepository, EntityStateRepository, EntityIssuesRepository,
} from "./repositories"
import { AppDataEntry, AppStates } from "./data"
import { EntityCoreService } from "@lib/data/services/entityCoreService"
import { DateTime } from "luxon"

type AppStateChangeFn<TAppStates extends AppStates> = (
    // TODO: Can use "delta: Partial<TAppStates>" because TypeScript doesn't support partial generics in this scenario.
    delta: any,
    states: TAppStates
) => Promise<void>

interface AppDataEntryFactory {
    id: string
    create: (id: string) => any
}

type AppDataEntryContainer = { [index: string]: AppDataEntryFactory }

class AppDataEntryFactories {
    readonly #container: AppDataEntryContainer = {}

    set(factory: AppDataEntryFactory) {
        this.#container[factory.id] = factory
    }

    create<T>(factoryId: string): T {
        for (const factoryId in this.#container) {
            if (factoryId === factoryId) {
                return this.#container[factoryId].create(factoryId) as T
            }
        }

        throw new Error(`App data entry not found ('${factoryId}').`)
    }
}

export interface IDatabaseCore {
    getRequiredCurrentUser(): AuthenticatedAppUser
    getRequiredCurrentUserId(): string
    initNewEntity<T extends IEntityCore>(entity: T, now?: Date): Promise<T>
    initModifiedEntity<T extends IEntityCore>(entity: T, now?: Date): Promise<T>
    entityStates: EntityStateRepository
    entityTypeStates: EntityTypeStateRepository
    http: HttpClient
}

export abstract class Database<
    TRepositoryContainer extends RepositoriesContainer,
    TAppStates extends AppStates = AppStates> implements IDatabaseCore {
    protected readonly dexieDb: DexieDatabase
    readonly #authService = inject(AuthService)
    readonly #entityCoreService = inject(EntityCoreService)
    readonly #appStates = signal<TAppStates | null>(null)
    readonly currentUser = this.#authService.user
    readonly entityTypeStates: EntityTypeStateRepository
    readonly entityStates: EntityStateRepository
    readonly entityIssues: EntityIssuesRepository
    readonly http = inject(HttpClient)

    readonly repos: TRepositoryContainer
    protected readonly appEntryFactories = new AppDataEntryFactories()
    appEntries!: Dexie.Table<AppDataEntry, string>

    constructor(indexDbName: string, schema: DexieSchema, repos: TRepositoryContainer) {
        this.dexieDb = new DexieDatabase(indexDbName, Object.assign(schema ?? {}, { appEntries: "id" }))
        this.repos = repos

        this.entityTypeStates = new EntityTypeStateRepository(this)
        this.entityStates = new EntityStateRepository(this)
        this.entityIssues = new EntityIssuesRepository(this)

        this.appEntryFactories.set({
            id: AppStates.ID,
            create: (_id: string) => new AppStates()
        })
    }

    _deleteDatabase(): Promise<void> {
        return this.dexieDb.delete()
    }

    async _resetEntityTypeStates() {
        await this.entityTypeStates._deleteTable()
    }

    #initializeDatabase() {
        this.dexieDb.initialize(null, [this, this.repos])

        this.appEntries = this.dexieDb.table("appEntries")
    }

    async initialize() {
        this.#initializeDatabase()
        await this.getAppStates()
    }

    protected _addRepository<TRepo extends DataRepository>(repository: TRepo): TRepo {
        repository.dbcore = this

        this.repos.add(repository)

        return repository
    }

    // TODO: REMOVE?
    // async setCurrentUser(user: AuthenticatedAppUser) {
    //     await this.modifyAppStates(async (changes) => {
    //         changes.delta = { currentUser: user }
    //         await Promise.resolve()
    //     })
    // }

    async setJobAppVersion(jobAppVersion: string) {
        await this.modifyAppStates(async (changes) => {
            changes.delta = { jobAppVersion: jobAppVersion }
        })
    }

    getRequiredCurrentUser(): AuthenticatedAppUser {
        const currentUser = this.currentUser()
        if (!currentUser) {
            throw new UserNotifiableError("Kein aktueller Benutzer.", "no-current-user")
        }

        return currentUser
    }

    // TODO: REMOVE
    // async getCurrentUser(): Promise<AuthenticatedAppUser | null> {
    //     let currentUser = this.#currentUser()
    //     if (!currentUser) {
    //         currentUser = this.#authService.user()
    //         if (currentUser) {
    //             this.#currentUser.set(currentUser)
    //         }
    //     }

    //     return currentUser
    // }

    getRequiredCurrentUserId(): string {
        return this.getRequiredCurrentUser().Id
    }

    async getAppStates(): Promise<TAppStates> {
        return await this.getOrCreateAppDataEntry<TAppStates>(AppStates.ID)
    }

    initNewEntity<T extends IEntityCore>(entity: T, now?: Date): Promise<T> {
        return Promise.resolve(
            this.#entityCoreService.initNewEntity(
                entity,
                now ? DateTime.fromJSDate(now) : DateTime.now()))
    }

    initModifiedEntity<T extends IEntityCore>(
        entity: T,
        now: Date | null | undefined = null
        // TODO: REMOV, user: AuthenticatedAppUser | null | undefined = null
    ): Promise<T> {
        return Promise.resolve(
            this.#entityCoreService.initModifiedEntity(
                entity,
                now ? DateTime.fromJSDate(now) : DateTime.now()))
    }

    protected async modifyAppStates(action: AppStateChangeFn<TAppStates>) {
        await this.modifyAppDataEntry<TAppStates>(AppStates.ID, action)
    }

    protected async getOrCreateAppDataEntry<T extends AppDataEntry>(id: string): Promise<T> {
        const anyEntry = await this.appEntries.get(id)
        const existingEntry = anyEntry as T
        if (existingEntry) {
            return existingEntry
        }

        const entry: T = this.appEntryFactories.create(id)

        await this.appEntries.add(entry)

        return entry
    }

    protected async modifyAppDataEntry<T extends AppDataEntry>(
        id: string,
        // TODO: Can use "delta: Partial<T>" because TypeScript doesn't support partial generics in this scenario.
        action: (changes: { delta: any }, entry: T) => Promise<void>
    ): Promise<void> {

        const entry = await this.getOrCreateAppDataEntry<T>(id)
        const changes: any = { delta: null }

        await action(changes, entry)

        if (!changes.delta || !Object.keys(changes.delta).length) {
            return
        }

        await this.appEntries.update(id, changes.delta)
    }

    /*
    * NOTE that the entity type ID must be one of the TypeKeys IDs (coming from server-side entities)
    * or a custom entity type ID represented by a repository.
    */
    getEntityTypeNameById(entityTypeId: string): string {
        let entityTypeName = TypeKeys.getNameById(entityTypeId)

        if (!entityTypeName) {
            // Might be a custom entity type (i.e. not represented by an entity on the server (yet)).
            for (const repo of this.repos.items()) {
                if (repo.entityTypeId === entityTypeId) {
                    entityTypeName = repo.entityTypeName
                    break
                }
            }
        }

        if (!entityTypeName) {
            throw new Error(`Unknown entity type ID: ${entityTypeId}.`)
        }

        return entityTypeName
    }

    async _deleteAllTables() {
        await this.entityIssues._deleteTable()
        await this.entityStates._deleteTable()
        await this.entityTypeStates._deleteTable()
        await this.appEntries.clear()
        this.#appStates.set(null)
    }
}
