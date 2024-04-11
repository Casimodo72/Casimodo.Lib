import { inject } from "@angular/core"
import { NotificationService } from "@lib/services"

export abstract class UserNotifiableComponent {
    readonly #notifier = inject(NotificationService)

    protected async performOperation<T>(operation: () => T | Promise<T> | void) {
        try {
            return await operation()
        }
        catch (error) {
            // TODO: Show messages of UserNottifiableErrors only.
            if (error instanceof Error) {
                this.#notifier.showError(error.message)
            } else {
                this.#notifier.showError("Ein unbekannter Fehler ist aufgetreten.")
            }

            throw error
        }
    }
}
