import { AuthGuard } from "@lib/auth"
import { NoopRouterOutletComponent } from "./noop-router-outlet.component"
import { AppRoute, AppRoutes } from "./appRoute"

export function addRouteWithChildren(route: AppRoute, children: AppRoute[]): AppRoute {
    const noopRoute: AppRoute = Object.assign({}, route, { component: NoopRouterOutletComponent })
    const mainChildRoute = Object.assign(route, { path: "", label: "" })
    noopRoute.children = [mainChildRoute, ...children]

    return noopRoute
}

export function setDefaultAnimation(animation: string, routes: AppRoutes): AppRoutes {
    return visitRoutes(routes, route => {
        const data = route.data ??= {}
        if (!data["animation"]) {
            data["animation"] = animation
        }

        return false
    })
}

export function findRouteById(routes: AppRoutes, routeId: string): AppRoute | undefined {
    let foundRoute: AppRoute | undefined

    visitRoutes(routes, currentRoute => {
        if (currentRoute.id == routeId) {
            foundRoute = currentRoute

            return true
        }

        return false
    })

    return foundRoute
}

export function allowAllAnonymous(routes: AppRoutes): AppRoutes {
    return visitRoutes(routes, route => {
        route.allowAnonymous = true

        return false
    })
}

export function initializeRoutes(routes: AppRoutes): AppRoutes {
    return visitRoutes(routes, route => {
        if (!route.allowAnonymous && !route.redirectTo) {
            if (!Array.isArray(route.canActivate)) {
                route.canActivate = []
            }

            route.canActivate.push(AuthGuard)
        }

        return false
    })
}

function visitRoutes(routes: AppRoutes, visitorFn: (route: AppRoute) => boolean): AppRoutes {
    for (const route of routes) {
        if (visitorFn(route)) {
            return routes
        }

        if (route.children) {
            visitRoutes(route.children, visitorFn)
        }
    }

    return routes
}
