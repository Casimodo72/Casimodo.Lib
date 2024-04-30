import { Injectable, inject } from "@angular/core"
import { DateTime } from "luxon"

import { AuthService } from "@lib/auth"
import { UserNotifiableError } from "@lib/errors"

import { IDeletableEntityCore, IEntityCore } from "../entityBase"

@Injectable({ providedIn: "root" })
export class EntityCoreService {
    readonly #authService = inject(AuthService)

    copyAsNewEntity<T extends Partial<IEntityCore>>(entityToCopy: T, now?: DateTime): T {
        // TODO: Use a real deep copy. Speading and Object.assign both
        // keep references to the original strings :-/
        const copy = { ...entityToCopy }
        copy.Id = crypto.randomUUID()

        return this.initNewEntity(copy, now)
    }

    initNewEntity<T extends Partial<IEntityCore>>(entity: T, now?: DateTime): T {
        now ??= DateTime.now()

        const user = this.#authService.user()
        if (!user) {
            throw this.#createNoCurrentUserError()
        }

        if (!entity.Id) {
            entity.Id = crypto.randomUUID()
        }

        entity.CreatedOn = now.toJSDate()
        // TODO: Avoid storing the username in the client.
        entity.CreatedBy = user.Username!
        entity.CreatedByUserId = user.Id

        this.initModifiedEntity(entity, now)

        return entity
    }

    initModifiedEntity<T extends Partial<IEntityCore>>(entity: T, now?: DateTime | undefined): T {
        now ??= DateTime.now()

        const user = this.#authService.user()
        if (!user) {
            throw this.#createNoCurrentUserError()
        }

        entity.ModifiedOn = now.toJSDate()
        // TODO: Avoid storing the username in the client.
        entity.ModifiedBy = user.Username!
        entity.ModifiedByUserId = user.Id

        return entity
    }

    initDeletedEntity<T extends Partial<IDeletableEntityCore>>(entity: T, now?: DateTime): T {
        now ??= DateTime.now()

        const user = this.#authService.user()
        if (!user) {
            throw this.#createNoCurrentUserError()
        }

        entity.IsDeleted = true
        entity.DeletedOn = now.toJSDate()
        // TODO: Avoid storing the username in the client.
        entity.DeletedBy = user.Username!
        entity.DeletedByUserId = user.Id

        return entity
    }

    #createNoCurrentUserError() {
        return new UserNotifiableError("Kein aktueller Benutzer.", "no-current-user")
    }
}
