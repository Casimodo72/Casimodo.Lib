import { Injectable, signal } from "@angular/core"

// TODO: Move to applib.

@Injectable({
    providedIn: "root"
})
export class AppInfoService {
    readonly jobAppVersion = signal("")
    readonly isPortalApp = signal(false)
    readonly isMobileApp = signal(false)
    readonly isJobApp = signal(false)

    setJobClientAppVersion(appVersion: string) {
        this.jobAppVersion.set(appVersion)
    }

    setIsPortalApp(isPortalApp: boolean) {
        this.isPortalApp.set(isPortalApp)
    }

    setIsMobileApp(isMobileApp: boolean) {
        this.isMobileApp.set(isMobileApp)
    }
}
