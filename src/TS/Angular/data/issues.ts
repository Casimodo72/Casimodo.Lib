import { IssueEntity, IssueEntityNode } from "./misc"

export type Severity = "error" | "warning" | "info"

export class Issue {
    readonly severity: Severity
    readonly message: string
    readonly messageId: string | null
    forNames: string[] | null | undefined

    constructor(severity: Severity, message: string, messageId: string | null) {
        if (!message) {
            throw new Error("The message is required.")
        }

        this.severity = severity
        this.message = message
        this.messageId = messageId
    }

    for(name: string): Issue {
        if (!name) {
            throw new Error("Name is required.")
        }
        this.forNames ??= []
        this.forNames.push(name)

        return this
    }

    toTreeEntry(): IssueEntity {
        const entry: IssueEntity = {
            severity: this.severity,
            message: this.message
        }

        if (this.forNames?.length) {
            entry.forNames = this.forNames
        }

        return entry
    }
}

export class IssuesContainer {
    readonly #items: Issue[]

    constructor(source?: Issue[] | IssuesContainer) {
        if (source) {
            if (Array.isArray(source)) {
                this.#items = [...source]
            } else {
                this.#items = [...source.#items]
            }
        }

        this.#items ??= []
    }

    *[Symbol.iterator](): Iterator<Issue> {
        yield* this.#items
    }

    get items(): Issue[] {
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

    toErrorIssueTreeEntries(): IssueEntity[] {
        return this.#items.filter(x => x.severity === "error").map(x => x.toTreeEntry())
    }

    add(severity: Severity, message: string): Issue {
        return this.addCore(severity, message, null, true)
    }

    addById(severity: Severity, messageId: string): Issue {
        return this.addCore(severity, null, messageId, true)
    }

    protected addCore(severity: Severity, message: string | null, messageId: string | null, noDuplicates: boolean): Issue {
        let issue: Issue | null = null
        if (noDuplicates) {
            issue = this.#findIssue(severity, message, messageId)

            if (issue != null) return issue
        }

        issue = this.createIssue(severity, message, messageId)

        this.#items.push(issue)

        return issue
    }

    #findIssue(severity: string, message: string | null, messageId: string | null): Issue | null {
        for (const existingIssue of this.#items) {
            if (existingIssue.severity === severity &&
                ((messageId !== null && existingIssue.messageId === messageId) || (message !== null && existingIssue.message === message))) {
                return existingIssue
            }
        }

        return null
    }

    protected createIssue(severity: Severity, message: string | null, messageId: string | null): Issue {
        if (!message) {
            throw new Error("The message is required.")
        }

        return new Issue(
            severity,
            message ?? "",
            messageId
        )
    }
}

export class IssuesState {
    hasErrors = false
    HasWarnings = false

    applyStates(issueStates: IssuesState[]) {
        for (const issueState of issueStates) {
            this.hasErrors ||= issueState.hasErrors
            this.HasWarnings ||= issueState.HasWarnings
        }
    }

    applyIssues(issue: IssuesContainer | IssuesContainer[]) {
        const effectiveIssues = Array.isArray(issue) ? issue : [issue]

        for (const issue of effectiveIssues) {
            this.hasErrors ||= issue.hasErrors
            this.HasWarnings ||= issue.hasWarnings
        }
    }
}

export interface ValidationResult {
    readonly valid: boolean
    readonly issues?: IssueEntityNode
}
