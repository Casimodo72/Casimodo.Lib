export interface Issue {
    readonly severity: string
    readonly severityId: string | null
    readonly message: string
    readonly issueId: string | null
}

export interface IssuesContainer extends Iterable<Issue> {
    readonly items: Issue[]

    get isEmpty(): boolean

    get hasErrors(): boolean

    get hasWarnings(): boolean
}

export interface IssuesState {
    hasErrors: boolean
    hasWarnings: boolean
}
