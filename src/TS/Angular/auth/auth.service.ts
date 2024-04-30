import { Injectable, computed, inject, signal } from "@angular/core"
import { HttpClient, HttpContext } from "@angular/common/http"
import { toObservable } from "@angular/core/rxjs-interop"
import { Router } from "@angular/router"
import { Subject, lastValueFrom } from "rxjs"

import { REQUEST_TYPE } from "@lib/transport"
import { SystemService } from "@lib/system"
import { UserNotifiableError } from "@lib/errors"

export interface AuthenticatedAppUser {
    readonly Id: string
    readonly Username: string
    readonly Initials: string
    readonly Roles: string[]
    readonly CompanyId: string
    readonly CompanyInitials: string
}

interface SignInResult {
    readonly ID: string
    readonly UN: string
    readonly UI: string
    readonly UR: string[]
    readonly CID: string
    readonly CI: string
}

@Injectable({
    providedIn: "root"
})
export class AuthService {
    readonly #userStoreKey = "a4p-current-user"
    readonly #router = inject(Router)
    readonly #http = inject(HttpClient)
    readonly #systemService = inject(SystemService)
    readonly initialized$ = new Subject()
    readonly #user = signal<AuthenticatedAppUser | null>(null)
    readonly userChanged = toObservable(this.#user)
    readonly user = this.#user.asReadonly()
    readonly isPossiblySignedIn = computed(() => this.user() !== null)
    #signInPagePath = "/sign-in"

    setSignInPagePath(signInPageRoute: string) {
        this.#signInPagePath = signInPageRoute
    }

    async initialize() {
        // Try to read last authenticated user from session storage.
        const userJson = sessionStorage.getItem(this.#userStoreKey)
        const user = userJson ? JSON.parse(userJson) : null

        if (user) {
            // Check whether the user is still authenticated;
            // i.e. if the HTTP only token cookie is still set and the token still valid.
            if (!await this.#systemService.isServerReachableAndUserAuthenticated()) {
                return
            }
        }

        this.#user.set(user)

        this.initialized$.complete()
    }

    get requiredUser() {
        const user = this.user()
        if (!user) {
            throw new UserNotifiableError("Sie sind nicht angemeldet.")
        }

        return user
    }

    async evaluateAuthentication(): Promise<boolean> {
        // Wait for initialization to complete.
        // This is needed because even if call initialize() in the app component,
        // it might not finish before any subsequent component (e.g. the login component)
        // call evaluateAuthentication().
        await lastValueFrom(this.initialized$, { defaultValue: undefined })

        if (!this.user()) {
            return false
        }

        const isAuthenticated = await this.#systemService.isServerReachableAndUserAuthenticated()
        if (!isAuthenticated) {
            this.#clearUser()
        }

        return isAuthenticated
    }

    async signIn(username: string, pw: string): Promise<AuthenticatedAppUser> {
        await this.clearUser()

        const signInResult = await lastValueFrom(this.#http.post<SignInResult>(
            "api/auth/si",
            {
                u: username,
                p: pw
            },
            {
                context: new HttpContext().set(REQUEST_TYPE, "sign-in")
            }
        ))

        const user: AuthenticatedAppUser = {
            Id: signInResult.ID,
            Username: signInResult.UN,
            Initials: signInResult.UI,
            Roles: signInResult.UR,
            CompanyId: signInResult.CID,
            CompanyInitials: signInResult.CI
        }

        sessionStorage.setItem(this.#userStoreKey, JSON.stringify(user))

        this.#user.set(user)

        return user
    }

    /**
    * Also called by the HTTP interceptor when a 401 is received.
    */
    clearUser() {
        this.#clearUser()

        return Promise.resolve()
    }

    signOut() {
        this.#clearUser()

        // We don't await on purpose.
        this.#http.post("api/auth/so", null)

        this.navigateToSignIn()

        return Promise.resolve()
    }

    navigateToSignIn() {
        this.#router.navigate([this.#signInPagePath])
    }

    #clearUser() {
        sessionStorage.removeItem(this.#userStoreKey)
        this.#user.set(null)
    }
}
