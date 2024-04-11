import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Input } from "@angular/core"

import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner"

import { LoginForm } from "./index"

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
            <mat-icon *ngIf="form.isBusy()"><mat-spinner diameter="16" color="accent" /></mat-icon>
        </button>
    `
})
export class LoginFormSubmitButtonComponent {
    @Input({ required: true }) form!: LoginForm
}
