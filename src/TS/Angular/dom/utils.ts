import { ElementRef } from "@angular/core"

export class DomEventManager implements EventListenerObject {
    readonly #owner: object
    readonly elementRef: ElementRef
    readonly #handlers: {
        [index: string]: (ev: any) => any
    } = {}

    constructor(owner: object, elementRef: ElementRef) {
        this.#owner = owner
        this.elementRef = elementRef
    }

    add<K extends keyof HTMLElementEventMap>(eventName: K, listener: (ev: HTMLElementEventMap[K]) => any) {
        this.elementRef.nativeElement.addEventListener(eventName, this)
        this.#handlers[eventName] = listener.bind(this.#owner)
    }

    addInput(listener: (ev: InputEvent) => any) {
        this.elementRef.nativeElement.addEventListener("input", this)
        this.#handlers["input"] = listener.bind(this.#owner)
    }

    removeAll() {
        const eventNames = Object.keys(this.#handlers)
        for (const eventName in eventNames) {
            this.elementRef.nativeElement.removeEventListener(eventName, this)
            delete this.#handlers[eventName]
        }
    }

    handleEvent(ev: Event): void {
        this.#handlers[ev.type]?.(ev)
    }
}
