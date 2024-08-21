import { Injector, runInInjectionContext } from "@angular/core"
import { Observable, Subject, lastValueFrom } from "rxjs"

import { AsyncVoidFunction } from "@lib/utils"

type HookFn = AsyncVoidFunction | VoidFunction
type HookName = "ngOnInit" | "ngAfterViewInit"
type HookDefinition = {
    hasDescendants?: boolean,
    /*
    * Ancestor hook names - from nearest to farthest.
    */
    ancestors?: HookName[]
}
type HookDefinitions = {
    [key in HookName]: HookDefinition
}

type AsyncComponentLivecycleOptions = {
    /**
     * Optional injector, in case one wants to inject stuff in ngOnInit/ngAfterViewInit.
     */
    injector?: Injector
}
/**
 * Lifecycle hooks: https://angular.io/guide/lifecycle-hooks
 */
export class AsyncComponentLivecycle {
    static readonly #dependencies: HookDefinitions = {
        ngOnInit: {
            hasDescendants: true
        },
        ngAfterViewInit: {
            ancestors: ["ngOnInit"]
        },
    }

    readonly #options?: AsyncComponentLivecycleOptions

    readonly #hookObservables: {
        [key in HookName]?: Observable<unknown>
    } = {}

    constructor(options?: AsyncComponentLivecycleOptions) {
        this.#options = options
    }

    onInit(hookFn: HookFn): void {
        this.#onHook("ngOnInit", hookFn, AsyncComponentLivecycle.#dependencies.ngOnInit, false)
    }

    afterViewInit(hookFn: HookFn, withTimeout: boolean = true): void {
        this.#onHook("ngAfterViewInit", hookFn, AsyncComponentLivecycle.#dependencies.ngAfterViewInit, withTimeout)
    }

    async #onHook(
        hookName: HookName,
        hookFn: HookFn,
        hookDefinition: HookDefinition,
        withTimeout: boolean
    ): Promise<void> {
        // console.debug(`## on-hook: ${hookName}`)

        const behaviourSubject = new Subject<unknown>()
        if (hookDefinition.hasDescendants) {
            this.#hookObservables[hookName] = behaviourSubject
        }

        if (hookDefinition.ancestors?.length) {
            for (const ancestorHookName of hookDefinition.ancestors) {
                const ancestorHookObservable$ = this.#hookObservables[ancestorHookName]
                if (!ancestorHookObservable$) continue

                await lastValueFrom(ancestorHookObservable$, { defaultValue: undefined })

                // TODO: Does it suffice to wait for just the nearest ancestor hook observable to complete?
                break
            }
        }

        try {
            // console.debug(`# ${hookName}: before call`)
            if (withTimeout) {
                setTimeout(async () => {
                    await this.#executeHookHandler(hookFn)
                })
            } else {
                await this.#executeHookHandler(hookFn)
            }
        } finally {
            // console.debug(`# ${hookName}: completed`)
            // TODO: Do we need the next() or would complete() suffice?
            behaviourSubject.next(undefined)
            behaviourSubject.complete()
        }
    }

    async #executeHookHandler(hookFn: HookFn): Promise<void> {
        if (this.#options?.injector) {
            await runInInjectionContext(this.#options?.injector, async () => await hookFn())
        } else {
            await hookFn()
        }
    }
}
