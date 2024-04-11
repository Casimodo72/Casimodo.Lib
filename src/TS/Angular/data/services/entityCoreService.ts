import { Injectable, inject } from "@angular/core"

import { AuthService } from "@lib/auth"
import { IDeletableEntityCore, IEntityCore } from "../entityBase"
import { DateTime } from "luxon"
import { UserNotifiableError } from "@lib/errors"

@Injectable({ providedIn: "root" })
export class EntityCoreService {
    readonly #authService = inject(AuthService)

    copyAsNewEntity<T extends Partial<IEntityCore>>(entityToCopy: T, now?: DateTime): T {
        const copy = { ...entityToCopy }
        copy.Id = crypto.randomUUID()

        return this.initNewEntity(copy, now)
    }

    initNewEntity<T extends Partial<IEntityCore>>(entity: T, now?: DateTime): T {
        now ??= DateTime.now()

        const user = this.#authService.user()
        if (!user) {
            throw new UserNotifiableError("Kein aktueller Benutzer.", "no-current-user")
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
            throw new UserNotifiableError("Kein aktueller Benutzer.", "no-current-user")
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
            throw new UserNotifiableError("Kein aktueller Benutzer.", "no-current-user")
        }

        entity.IsDeleted = true
        entity.DeletedOn = now.toJSDate()
        // TODO: Avoid storing the username in the client.
        entity.DeletedBy = user.Username!
        entity.DeletedByUserId = user.Id

        return entity
    }
}
