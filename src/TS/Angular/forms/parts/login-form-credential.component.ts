import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, input } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatIconModule } from "@angular/material/icon"
import { MatInputModule } from "@angular/material/input"

import { CMatModel, CMatModelErrors, CMatModelLabel } from "@lib/forms"
import { LoginFormModel } from "./loginForm"

@Component({
    selector: "app-login-form-credential",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule, FormsModule,
        MatFormFieldModule, MatInputModule, MatIconModule,
        CMatModelLabel, CMatModel, CMatModelErrors
    ],
    styles: [`
        :host { display: contents; }
    `],
    template: `
@if (form(); as form) {
    <mat-form-field>
        <mat-label [cmatModel]="form.username" />
        <input matInput [cmatModel]="form.username" cdkFocusInitial [ngModel]="form.username.value()"
            autocomplete="off" spellcheck="false" />
        <mat-error [cmatModel]="form.username" />
    </mat-form-field>

    <mat-form-field>
        <mat-label [cmatModel]="form.password" />
        <input matInput [cmatModel]="form.password" [ngModel]="form.password.value()" autocomplete="off"
            spellcheck="false" [type]="form.isPasswordHidden() ? 'password' : 'text'" />
        <mat-icon matSuffix (click)="form.togglePasswordVisibility()">
            {{form.isPasswordHidden() ? 'visibility_off' : 'visibility'}}
        </mat-icon>
        <mat-error [cmatModel]="form.password" />
    </mat-form-field>
}
`
})
export class LoginFormCredentialComponent {
    readonly form = input.required<LoginFormModel>()
}
