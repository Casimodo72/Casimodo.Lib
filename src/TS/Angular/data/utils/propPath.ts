import { StringKeys } from "./utils"

export type PropPathSelection<T> = StringKeys<T> | ((b: PropPathBuilder<T>) => PropPath)

export class PropPath {
    static fromSelection<T>(selection: PropPathSelection<T>): PropPath {
        if (typeof selection === "string" && selection !== "") {
            return new PropPath([selection])
        }
        else if (typeof selection === "function") {
            return selection(new PropPathBuilder<T>())
        }

        return new PropPath([])
    }

    readonly segments: string[]
    readonly path: string

    constructor(nameOrSegments: string | string[]) {
        if (typeof nameOrSegments === "string") {
            nameOrSegments = [nameOrSegments]
        }
        this.segments = nameOrSegments
        this.path = nameOrSegments.reduce((acc, next) => acc + "." + next)
    }

    getValue(data: any): any | null | undefined {
        if (data == null || !this.segments.length) return undefined

        for (let i = 0; i < this.segments.length; i++) {
            if (data == null || typeof data !== "object") {
                return undefined
            }

            data = data[this.segments[i]]
        }

        return data
    }

    getValueAtIndex(data: any, index: number): any | null | undefined {
        if (index < 0 ||
            index >= this.segments.length ||
            !this.segments.length
        ) {
            return undefined
        }

        for (let i = 0; i <= index; i++) {
            if (data == null || typeof data !== "object") {
                return undefined
            }

            data = data[this.segments[i]]
        }

        return data
    }
}

export abstract class PropPathBuilderBase {
    protected _depth = 0
    protected segments: string[] = []

    protected _select(name: string): void {
        if (this._depth >= this.segments.length) {
            this.segments.push(name)
        }
        else {
            this.segments[this._depth] = name
        }
    }

    protected _expand(prop: string) {
        this.segments.push(prop)
        this._depth++
    }
}

export class PropPathBuilder<T = any> extends PropPathBuilderBase {
    select(name: StringKeys<T>): PropPath {
        this._select(name)

        return new PropPath(this.segments)
    }

    expand<TExpandType>(name: StringKeys<T>): PropPathBuilder<TExpandType> {
        this._expand(name)

        return this as any as PropPathBuilder<TExpandType>
    }
}
