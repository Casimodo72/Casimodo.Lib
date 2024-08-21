import * as intf from "./validationIssues"

export class IssuesContainerImpl implements intf.IssuesContainer {
    readonly #items: intf.Issue[]

    constructor(container: intf.IssuesContainer) {
        this.#items = container?.items ?? []
    }

    *[Symbol.iterator](): Iterator<intf.Issue> {
        yield* this.#items
    }

    get items(): intf.Issue[] {
        return this.#items
    }

    get isEmpty() {
        return this.#items.length === 0
    }

    get hasErrors() {
        return this.#items.some(x => x.severity === "error")
    }

    get hasWarnings() {
        return this.#items.some(x => x.severity === "warning")
    }
}
