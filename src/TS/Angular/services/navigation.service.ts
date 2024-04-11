import { DestroyRef, Injectable, inject } from "@angular/core"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { Router, NavigationEnd, ActivatedRouteSnapshot } from "@angular/router"
import { BehaviorSubject, distinctUntilChanged, filter, tap } from "rxjs"
import linq from "linq"

import { AppRoute } from "@lib/routing"
import { Breadcrumb } from "@lib/navigation"

interface BreadcrumbsContainerGetItemsOptions {
    inclusiveBounderyRouteId: string | null | undefined, max: number | null | undefined
}

export class BreadcrumbsContainer {
    #breadcrumbs: Breadcrumb[] = []

    constructor(breadcrumbs?: Breadcrumb[]) {
        this.#breadcrumbs = breadcrumbs ?? []
    }

    items(options?: BreadcrumbsContainerGetItemsOptions): Breadcrumb[] {
        if (!options?.inclusiveBounderyRouteId && !options?.max) return [...this.#breadcrumbs]

        let reversedResultCrumbs: Breadcrumb[] = []
        const reversedCrumbs = this.#breadcrumbs.slice().reverse()

        if (options.inclusiveBounderyRouteId) {
            const boudaryCrumb = this.#findBoundaryCrumb(options.inclusiveBounderyRouteId)

            // Boundary crumb must be in scope; return nothing otherwise.
            if (!boudaryCrumb) {
                return []
            }

            for (const crumb of reversedCrumbs) {
                reversedResultCrumbs.push(crumb)

                if (crumb === boudaryCrumb) {
                    break
                }
            }
        } else {
            reversedResultCrumbs = reversedCrumbs
        }

        if (options.max && options.max < reversedResultCrumbs.length) {
            reversedResultCrumbs = reversedResultCrumbs.slice(0, options.max)
        }

        return reversedResultCrumbs.reverse()
    }

    #findBoundaryCrumb(bounderyRouteId: string): Breadcrumb | undefined {
        return this.#breadcrumbs.findLast(x => x.id === bounderyRouteId)
    }

    hasCrumbById(routeId: string): boolean {
        return this.#breadcrumbs.findLast(x => x.id === routeId) !== undefined
    }

    findLastNavigatableCrumb(bounderyRouteId: string | null | undefined): Breadcrumb | undefined {
        const crumbs = this.#breadcrumbs

        if (!bounderyRouteId) {
            return crumbs.findLast(x => x.canNavigate)
        }

        let boundaryFound = false
        let navigatableCrumb: Breadcrumb | undefined

        for (const crumb of linq.from(crumbs).reverse()) {
            if (!navigatableCrumb && crumb.canNavigate) {
                navigatableCrumb = crumb
            }

            if (crumb.id === bounderyRouteId) {
                boundaryFound = true

                break
            }
        }

        // Boundary must be is scope.
        if (!boundaryFound || !navigatableCrumb) {
            return undefined
        }

        return navigatableCrumb
    }
}

export interface AppNavigationState {
    canNavigateBack: boolean
    breadcrumbs: Breadcrumb[]
    breadcrumbsContainer: BreadcrumbsContainer
}

@Injectable({
    providedIn: "root"
})
export class AppNavigationService {
    readonly #destroyRef = inject(DestroyRef)
    readonly #router = inject(Router)
    readonly state = new BehaviorSubject<AppNavigationState>({
        canNavigateBack: false,
        breadcrumbs: [],
        breadcrumbsContainer: new BreadcrumbsContainer()
    })

    constructor() {
        let prevUrl = ""
        this.#router.events
            .pipe(
                takeUntilDestroyed(this.#destroyRef),
                filter((e) => e instanceof NavigationEnd),
                distinctUntilChanged(() => this.#router.url === prevUrl),
                tap(() => prevUrl = this.#router.url))
            .subscribe(ev => {
                if (ev instanceof NavigationEnd) {
                    this.#updateState()
                }
            })
    }

    navigateToBreadcrumb(crumb: Breadcrumb): void {
        this.#router.navigateByUrl(crumb.path)
    }

    canNavigateBack(inclusiveBounderyRouteId?: string): boolean {
        return this.#findLastNavigatableCrumb(inclusiveBounderyRouteId) !== undefined
    }

    navigateBack(inclusiveBounderyRouteId?: string): void {
        const lastNavigatableCrumb = this.#findLastNavigatableCrumb(inclusiveBounderyRouteId)
        if (lastNavigatableCrumb) {
            this.#router.navigateByUrl(lastNavigatableCrumb.path)
        }
    }

    #updateState() {
        const routes: ActivatedRouteSnapshot[] = []

        // Collect all activated routes from root to current route.
        let route: ActivatedRouteSnapshot | null = this.#router.routerState.root.snapshot.root
        while (route) {
            routes.push(route)
            route = route.firstChild
        }

        const newBreadcrumbs: Breadcrumb[] = []
        let path = "/"
        for (const route of routes) {
            const appRoute = route.routeConfig as AppRoute | null
            if (!appRoute) continue

            const routePath = route.url.map(segment => segment.toString()).join("/")

            if (routePath) {
                if (path !== "/") {
                    path += "/"
                }
                path += routePath
            }

            // Exclude routes without a label from breadcrumbs.
            if (!appRoute.label) continue

            const breadcrumb: Breadcrumb = {
                id: appRoute.id ?? null,
                label: appRoute.label,
                path: path,
                canNavigate: appRoute.canNavigate ?? true
            }

            newBreadcrumbs.push(breadcrumb)
        }

        // Last breadcrumb cannot be navigated to.
        if (newBreadcrumbs.length) {
            newBreadcrumbs[newBreadcrumbs.length - 1].canNavigate = false
        }

        const newState = {
            canNavigateBack: newBreadcrumbs.findLast(x => x.canNavigate) !== undefined,
            breadcrumbs: newBreadcrumbs,
            breadcrumbsContainer: new BreadcrumbsContainer(newBreadcrumbs)
        }

        this.state.next(newState)
    }

    #findLastNavigatableCrumb(inclusiveBounderyRouteId?: string): Breadcrumb | undefined {
        if (!this.state.value.canNavigateBack) {
            return undefined
        }

        return this.state.value.breadcrumbsContainer.findLastNavigatableCrumb(inclusiveBounderyRouteId)
    }
}
