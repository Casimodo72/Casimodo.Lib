import { Issue, IssueContainerEntity, IssueEntity } from "@lib/data"
import { ItemModel } from "./items"

type TypedNode<T> = T extends boolean
    ? BooleanIssueModel
    : T extends IssueEntity[]
    ? IssueEntryListModel
    : T extends IssueContainerEntity
    ? IssueContainerModel<T>
    : T

export type IssueContainerModel<TContainer extends IssueContainerEntity = IssueContainerEntity> = {
    [K in keyof TContainer]: TypedNode<TContainer[K]>
}

class IssueModelBase extends ItemModel {
    readonly name: string

    constructor(name: string) {
        super()
        this.name = name
    }
}

class IssueContainerModelImpl extends IssueModelBase {
    constructor(name: string) {
        super(name)
    }
}

export class BooleanIssueModel extends IssueModelBase {
    readonly #value: boolean

    constructor(name: string, value: boolean) {
        super(name)
        this.#value = value
    }

    get hasErrors(): boolean {
        return this.#value
    }
}

export class IssueModel extends ItemModel {
    static CreateFromIssue(issue: Issue): IssueModel {
        return new IssueModel(issue.toTreeEntry())
    }

    readonly #value: IssueEntity

    constructor(value: IssueEntity) {
        super()

        this.#value = value
    }

    get hasErrors(): boolean {
        return this.#value.severity === "error"
    }

    get message(): string {
        return this.#value.message
    }

    isFor(name: string): boolean {
        return !!this.#value.forNames?.includes(name)
    }

    get isForNone() {
        return !this.#value.forNames?.length
    }
}

export class IssueEntryListModel extends IssueModelBase {
    private readonly value: IssueModel[]
    #hasErrors: boolean

    constructor(name: string, entries: IssueEntity[]) {
        super(name)
        this.value = entries.map(x => new IssueModel(x))
        this.#hasErrors = this.value.some(x => x.hasErrors)
    }

    get hasErrors(): boolean {
        return this.#hasErrors
    }

    for(name: string): IssueModel[] {
        return this.value.filter(x => x.isFor(name))
    }

    forNone(): IssueModel[] {
        return this.value.filter(x => x.isForNone)
    }
}

export function createIssueTreeModel<T extends IssueContainerEntity>(tree: T): IssueContainerModel<T> {
    return createIssueTreeModelCore(tree) as IssueContainerModel<T>
}

function createIssueTreeModelCore(tree: IssueContainerEntity): IssueContainerModelImpl {
    const treeModel: any = new IssueContainerModelImpl("")

    for (const key in tree) {
        const value = tree[key]
        let childModel: IssueModelBase | undefined

        if (typeof value === "boolean") {
            childModel = new BooleanIssueModel(key, value)
        } else if (Array.isArray(value)) {
            childModel = new IssueEntryListModel(key, value)
        } else if (typeof value === "object") {
            childModel = createIssueTreeModelCore(value)
        }

        if (childModel) {
            treeModel[key] = childModel
        }
    }

    return treeModel
}
