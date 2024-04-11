import { Injectable, signal } from "@angular/core"

@Injectable({
    providedIn: "root"
})
export class BusyStateService {
    readonly isBusy = signal(false)
    #busyCounter = 0

    startProgress(): void {
        const previousBusyCounter = this.#busyCounter

        this.#busyCounter++

        if (previousBusyCounter === 0) {
            this.isBusy.set(true)
        }
    }

    endProgress(): void {
        this.#busyCounter--

        if (this.#busyCounter < 0) {
            console.debug("ProgressService: busyCounter is negative. Resetting to 0.")
            this.#busyCounter = 0
        }

        if (this.#busyCounter === 0) {
            this.isBusy.set(false)
        }
    }
}
