import { Injectable, inject } from "@angular/core"
import { ActivatedRouteSnapshot, CanActivateFn, Route, RouterStateSnapshot } from "@angular/router"
import { lastValueFrom } from "rxjs"

import { AppRoute } from "@lib/routing/appRoute"
import { AuthService, AuthenticatedAppUser } from "./authService"

function getUserRolesFromAppRoute(appRoute: AppRoute): string[] | undefined {
    return typeof appRoute.allowRoles === "function"
        ? appRoute.allowRoles()
        : appRoute.allowRoles
}

export function canUserActivateRoute(user?: AuthenticatedAppUser | null, route?: Route | null) {
    const appRoute = route as AppRoute
    if (!appRoute) return false

    if (appRoute.allowAnonymous) return true

    if (!user) return false

    const userRoles = user.Roles

    const allowRoles = getUserRolesFromAppRoute(appRoute)
    //  If allowRoles is undefined then any user can activate this route.
    if (!allowRoles) {
        return true
    }

    if (allowRoles.length) {
        for (const allowRole of allowRoles) {
            if (userRoles?.includes(allowRole) === true) {
                return true
            }
        }

        return false
    }

    return false
}

@Injectable({
    providedIn: "root"
})
class AuthGuardService {
    readonly #authService: AuthService

    constructor(authService: AuthService) {
        this.#authService = authService
    }

    canActivate = async (next: ActivatedRouteSnapshot, _state: RouterStateSnapshot): Promise<boolean> => {
        // Wait for initialization to complete.
        // This is needed because even if call initialize() in the app component,
        // it might not finish before any subsequent component (e.g. the auth guard) executes.
        await lastValueFrom(this.#authService.initialized$, { defaultValue: undefined })

        const user = this.#authService.user()
        if (!user) {
            this.#authService.navigateToSignInPage()

            return false
        }

        if (!canUserActivateRoute(user, next.routeConfig)) {
            return false
        }

        return true
    }
}

export const AuthGuard: CanActivateFn = (next: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> => {
    return inject(AuthGuardService).canActivate(next, state)
}
