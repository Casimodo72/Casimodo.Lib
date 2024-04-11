import { Injectable, inject, signal } from "@angular/core"
import { SwUpdate, VersionDetectedEvent, VersionEvent, VersionReadyEvent } from "@angular/service-worker"
import { Database } from "@lib/database"
import { DialogService } from "@lib/dialogs"
import { AppInfoService, NotificationService } from "@lib/services"

interface IInMigrationConfig {
}

interface IUpMigrationConfig {
    deleteDatabase?: boolean
    deleteData?: boolean
}

interface IVersionMigrationConfig {
    up?: IUpMigrationConfig
    in?: IInMigrationConfig
}

interface IVersionConfig {
    migration?: IVersionMigrationConfig
}

interface IVersionConfigList {
    [key: string]: IVersionConfig | null
}

interface IAppData {
    versions?: IVersionConfigList
}

type DatabaseMigrationInstruction = "" | "delete-database" | "delete-data"

export type AppUpdateResult = "notAvailable" | "cancelled" | "failed" | "updated"

@Injectable({
    providedIn: "root"
})
export class AppUpdateService {
    readonly #appInfoService = inject(AppInfoService)
    readonly #swUpdate: SwUpdate
    readonly #dialogService = inject(DialogService)
    readonly #notificationService = inject(NotificationService)
    readonly #databases: Database<any>[] = []
    readonly #isUpdateAvailable = signal(false)
    readonly #isUpdateReady = signal(false)
    #versionDetectedEvent?: VersionDetectedEvent
    // TODO: Do we need to listen for this?
    //#versionReadyEvent?: VersionReadyEvent

    constructor(swUpdate: SwUpdate) {
        this.#swUpdate = swUpdate

        this.#swUpdate.versionUpdates.subscribe((event: VersionEvent) => {
            //console.log(`VersionEvent (type: ${event.type})`)

            // TODO: ? VERSION_INSTALLATION_FAILED, VERSION_READY
            if (event.type === "VERSION_DETECTED") {
                this.#versionDetectedEvent = event as VersionDetectedEvent
                // console.log("VERSION_DETECTED")
                this.#isUpdateAvailable.set(true)
            }
            else if (event.type === "VERSION_READY") {
                //this.#versionReadyEvent = event as VersionReadyEvent
                // console.log("VERSION_READY")
                this.#isUpdateReady.set(true)
            }
        })
    }

    addDatabases(dbs: Database<any>[]) {
        this.#databases.push(...dbs)
    }

    async updateIfAvailable(): Promise<AppUpdateResult> {
        if (!await this.#checkForUpdate()) {
            return "notAvailable"
        }

        let message = "Eine neue Version der App ist verfügbar."

        const databaseInstruction = this.#getDatabaseUpMigrationInstruction()

        if (databaseInstruction === "delete-database") {
            message += "\nWARNUNG: Die Datenbank muss diesmal bei der App-Aktualisierung gelöscht werden. Alle nicht gesendeten Daten gehen hierbei verloren."
        }
        else if (databaseInstruction === "delete-data") {
            message += "\nWARNUNG: Die Datenbank-Daten müssen diesmal bei der App-Aktualisierung gelöscht werden. Alle nicht gesendeten Daten gehen hierbei verloren."
        }
        else {
            message += "\nINFO: Diesmal wird die Datenbank hierbei nicht gelöscht."
        }

        message += "\nMöchten Sie die App jetzt aktualisieren?"

        const isAppUpdateConfirmed = await this.#dialogService.confirm({
            title: "App aktualisieren",
            message: message
        })

        if (!isAppUpdateConfirmed) {
            return "cancelled"
        }

        try {
            if (!await this.#update()) {
                return "failed"
            }
        }
        catch (error: any) {
            this.#notificationService.showError("Bei der App-Aktualisierung ist ein Fehler aufgetreten. " + (error.message ?? ""))

            return "failed"
        }

        return "updated"
    }

    #getDatabaseUpMigrationInstruction(): DatabaseMigrationInstruction {
        const appData: IAppData | undefined = this.#versionDetectedEvent?.version?.appData
        if (!appData) {
            return ""
        }

        const currentAppVersion = this.#appInfoService.jobAppVersion()

        let databaseInstruction: DatabaseMigrationInstruction = ""

        if (appData.versions) {
            for (const [versionKey, version] of Object.entries(appData.versions)) {
                if (!version || versionKey < currentAppVersion) {
                    continue
                }

                console.log(`# App version config '${versionKey}':`, version)

                const upMigration = version.migration?.up
                if (upMigration) {
                    if (upMigration.deleteDatabase) {
                        databaseInstruction = "delete-database"
                    }
                    else if (upMigration.deleteData) {
                        databaseInstruction = "delete-data"
                    }
                }

                if (databaseInstruction === "delete-database") break
            }
        }

        return databaseInstruction
    }

    async #checkForUpdate(): Promise<boolean> {
        if (!this.#swUpdate.isEnabled) {
            console.error("checkForUpdate: Update service is disabled")

            return false
        }

        if (this.#isUpdateAvailable()) {
            return true
        }

        const canUpdate = await this.#swUpdate.checkForUpdate()
        if (!canUpdate) {
            if (this.#isUpdateAvailable()) {
                return true
            }

            console.info("checkForUpdate: negative")

            return false
        }

        return true
    }

    async #update(): Promise<boolean> {
        if (!this.#swUpdate.isEnabled) {
            console.error("performUpdate: Update service is disabled")

            return false
        }

        if (!this.#versionDetectedEvent) {
            console.error("performUpdate: No VersionDetectedEvent")

            return false
        }

        let isUpdateReady = this.#isUpdateReady()
        if (!isUpdateReady) {
            isUpdateReady = await this.#swUpdate.checkForUpdate()
        }

        if (!isUpdateReady) {
            console.error("performUpdate: Update is not ready")

            return false
        }

        const appData = this.#versionDetectedEvent.version.appData as IAppData
        if (!appData) {
            console.error("performUpdate: No VersionDetectedEvent.appData")

            return false
        }

        const databaseInstruction = this.#getDatabaseUpMigrationInstruction()
        if (databaseInstruction === "delete-database") {
            for (const db of this.#databases) {
                await db._deleteDatabase()
            }
        }
        else if (databaseInstruction === "delete-data") {
            for (const db of this.#databases) {
                await db._deleteAllTables()
            }
        }

        // NOTE: From the docs of SwUpdate.activateUpdate():
        // "In most cases, you should not use this method and instead should update a client by reloading the page".

        document.location.reload()

        return true
    }
}
