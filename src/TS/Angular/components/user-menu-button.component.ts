import { ChangeDetectionStrategy, Component, input } from "@angular/core"

import { MatButtonModule } from "@angular/material/button"

import { AuthenticatedAppUser } from "@lib/auth/auth.service"

@Component({
    selector: "app-user-menu-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButtonModule],
    template: `
 @if (user(); as user) {
    <button mat-stroked-button>
        <span>{{user.Initials}}</span><span class="mat-small">&#x00B7;{{user.CompanyInitials}}</span>
    </button>
 }
 `
})
/**
 * Intended to be displayed in the app's title bar in order to
 * display the currently logged in user and to open the user's context menu.
*/
export class AppUserMenuButtonComponent {
    readonly user = input.required<AuthenticatedAppUser | null | undefined>()
}
