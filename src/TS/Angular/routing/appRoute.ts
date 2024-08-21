import { Route } from "@angular/router"

export interface AppRoute extends Route {
    id?: string
    label?: string
    allowAnonymous?: boolean
    /** If allowRoles is undefined then any user can activate this route.  */
    allowRoles?: string[] | (() => string[])
    canNavigate?: boolean
    children?: AppRoute[]
}

export type AppRoutes = AppRoute[];
