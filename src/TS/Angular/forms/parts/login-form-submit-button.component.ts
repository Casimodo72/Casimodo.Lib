import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"

import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner"

import { LoginFormModel } from "./loginForm"

@Component({
    selector: "app-login-form-submit-button",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        MatButtonModule, MatIconModule, MatProgressSpinnerModule
    ],
    styles: [`
        :host { display: contents; }
    `],
    template: `
<button mat-raised-button color="primary" class="w-full">
    <span>Einloggen</span>
    @if (form().isBusy()) {
        <mat-icon><mat-spinner diameter="16" color="accent" /></mat-icon>
    }
</button>
`
})
export class LoginFormSubmitButtonComponent {
    readonly form = input.required<LoginFormModel>()
}
