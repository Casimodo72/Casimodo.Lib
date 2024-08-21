
export type JsonPatchOperationType = "add" | "remove" | "replace"

export interface JsonPatchOperation {
    readonly op: JsonPatchOperationType
    path: string
    value: any
}

export function convertDeltaToPatches(delta: object): JsonPatchOperation[] {
    const patches: JsonPatchOperation[] = []
    for (const key in delta) {
        patches.push({
            op: "replace",
            path: `/${key}`,
            value: (delta as any)[key]
        })
    }

    return patches
}
