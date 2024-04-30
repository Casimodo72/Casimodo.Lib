import { WritableSignal } from "@angular/core"
import { DateTime, Duration } from "luxon"

export type AsyncVoidFunction = () => Promise<void>

// Source: https://github.com/dsherret/ts-nameof/issues/121
export function nameof<TObject>(obj: TObject, key: keyof TObject): string
export function nameof<TObject>(key: keyof TObject): string
export function nameof(key1: any, key2?: any): any {
    return key2 ?? key1
}

export function isEmptyString(text: string | null | undefined): boolean {
    return isNullOrWhiteSpace(text)
}

export function isNullOrWhiteSpace(value: string | null | undefined): boolean {
    return !value || value.length === 0 || /^\s*$/.test(value)
}

export function getNowDateDiff(date: DateTime): number {
    return DateTime.now().startOf("day")
        .diff(
            date.startOf("day"),
            "days")
        .toObject().days!
}

export function getDateDiff(date1: Date, date2: Date): number {
    const diff = DateTime.fromJSDate(date1).startOf("day")
        .diff(
            DateTime.fromJSDate(date2).startOf("day"),
            "days")

    return diff.days
}

type TimeDiffResolution = "minute" | "second"

// TODO: REMOVE:
export function getTimeDiff(date1: Date, date2: Date, resolution: TimeDiffResolution): Duration {
    const normalizedDate1 = DateTime.fromJSDate(date1).set({ year: 2023, month: 1, day: 1 }).startOf(resolution)
    const normalizedDate2 = DateTime.fromJSDate(date2).set({ year: 2023, month: 1, day: 1 }).startOf(resolution)

    return normalizedDate1.diff(normalizedDate2, [resolution])
}

export function isDateEqual(date1: Date, date2: Date): boolean {
    return DateTime.fromJSDate(date1).startOf("day") === DateTime.fromJSDate(date2).startOf("day")
}

export function base64toBlob(b64Data: string, contentType = "", sliceSize = 512) {
    const byteCharacters = atob(b64Data)
    const byteArrays = []

    for (let offset = 0; offset < byteCharacters.length; offset += sliceSize) {
        const slice = byteCharacters.slice(offset, offset + sliceSize)

        const byteNumbers = new Array(slice.length)
        for (let i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i)
        }

        const byteArray = new Uint8Array(byteNumbers)
        byteArrays.push(byteArray)
    }

    const blob = new Blob(byteArrays, { type: contentType })

    return blob
}

export class SignalHelper {
    static patch<T>(signal: WritableSignal<T>, partialState: Partial<T>) {
        signal.update((state) => ({
            ...state,
            ...partialState,
        }))
    }
    // NOTE: In Angular 17 the "mutate" function was removed from WritableSignal; we need to use "update" instead.
    //   This might produce performance/memory issues, since we now always need to return a new array.

    static push<T>(arraySignal: WritableSignal<T[] | null>, itemToPush: T, onlyIfNotExists: boolean = false): boolean {
        const currentArray = arraySignal()

        if (onlyIfNotExists &&
            currentArray !== null &&
            currentArray.indexOf(itemToPush) !== -1
        ) {
            return false
        }

        arraySignal.update(
            array => array === null
                ? [itemToPush]
                : [...array, itemToPush])

        return true
    }

    static insertFirst<T>(arraySignal: WritableSignal<T[] | null>, itemToInsert: T, onlyIfNotExists: boolean = false): boolean {
        const currentArray = arraySignal()

        if (onlyIfNotExists &&
            currentArray !== null &&
            currentArray.indexOf(itemToInsert) !== -1
        ) {
            return false
        }

        arraySignal.update(
            array => array === null
                ? [itemToInsert]
                : [itemToInsert, ...array])

        return true
    }

    static pushRange<T>(arraySignal: WritableSignal<T[]>, ...itemsToPush: T[]): void {
        arraySignal.update(array => [...array, ...itemsToPush])
    }

    static remove<T>(arraySignal: WritableSignal<T[] | null>, itemToRemove: T): boolean {
        const currentArray = arraySignal()
        if (!currentArray?.length) {
            return false
        }

        const index = currentArray.indexOf(itemToRemove)
        if (index === -1) {
            return false
        }

        arraySignal.update(array => {
            return [...array!.slice(0, index), ...array!.slice(index + 1)]
        })

        return true
    }

    static replace<T>(arraySignal: WritableSignal<T[]>, oldItem: T, newItem: T): boolean {
        const index = arraySignal().indexOf(oldItem)
        if (index === -1) {
            return false
        }

        arraySignal.update(array => {
            array.splice(index, 1, newItem)

            return [...array]
        })

        return true
    }

    static splice<T>(arraySignal: WritableSignal<T[] | null>, start: number, deleteCount: number, ...items: T[]): T[] {
        const currentArray = arraySignal()
        if (!currentArray?.length) return []

        let spliceResult: T[] = []

        arraySignal.update(array => {
            spliceResult = array!.splice(start, deleteCount, ...items)

            return [...array!]
        })

        return spliceResult
    }
}
