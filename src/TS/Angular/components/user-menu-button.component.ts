import { ChangeDetectionStrategy, Component, Input } from "@angular/core"

import { MatButtonModule } from "@angular/material/button"

import { AuthenticatedAppUser } from "@lib/auth/auth.service"

@Component({
    selector: "app-user-menu-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButtonModule],
    template: `
 @if (user) {
    <button mat-stroked-button>
        <span>{{user.Initials}}</span><span class="mat-small">&#x00B7;{{user.CompanyInitials}}</span>
    </button>
 }
    `
})
export class AppUserMenuButtonComponent {
    @Input({ required: true }) user: AuthenticatedAppUser | null | undefined
}
