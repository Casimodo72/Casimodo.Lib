import { Injectable, Signal, computed } from "@angular/core"
import { Observable, Subject } from "rxjs"

export interface AuthenticatedAppUser {
    readonly Id: string
    readonly Username: string
    readonly Initials: string
    readonly Roles: string[]
    readonly CompanyId: string
    readonly CompanyInitials: string
    readonly CompanyName?: string
}

@Injectable()
export abstract class AuthService {
    readonly abstract userChanged: Observable<AuthenticatedAppUser | null>
    readonly abstract user: Signal<AuthenticatedAppUser | null>

    readonly initialized$ = new Subject()
    readonly isPossiblySignedIn = computed(() => this.user() !== null)

    abstract initialize(): Promise<void>

    abstract signIn(username: string, pw: string): Promise<AuthenticatedAppUser>

    abstract signOut(): Promise<void>

    abstract get requiredUser(): AuthenticatedAppUser

    abstract evaluateAuthentication(): Promise<boolean>

    /**
     * Clears the currently authenticated user (also in e.g. session storage).
     * Also called by the HTTP interceptor when a 401 is received.
     */
    abstract clearUser(): Promise<void>

    abstract setSignInPageRoute(signInPageRoute: string): void

    abstract navigateToSignInPage(): Promise<boolean>
}
