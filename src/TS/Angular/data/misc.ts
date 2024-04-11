import { Severity } from "./issues"

export interface IssueEntity {
    readonly severity: Severity
    readonly message: string
    forNames?: string[]
}

export type IssueEntityNode = IssueEntity[] | IssueContainerEntity | boolean

export interface IssueContainerEntity {
    readonly [key: string]: IssueEntityNode
}

export class RecentlyUsedCostCenter {
    CreatedOn!: Date
    CostCenterId?: string | null
    CostCenterNumber?: string | null
}

export interface IAppUserData {
    Id: string
    UserId: string
    TimeTrackerRecentlyUsedCostCenters?: RecentlyUsedCostCenter[]
}

export interface IUserTimeAffiliation {
    JobId?: string | null
    STaskId?: string | null
}

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
