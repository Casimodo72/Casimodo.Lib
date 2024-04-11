import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Input } from "@angular/core"
import { FormsModule } from "@angular/forms"

import { MatFormFieldModule } from "@angular/material/form-field"
import { MatIconModule } from "@angular/material/icon"
import { MatInputModule } from "@angular/material/input"

import { CCPropDirective, PropErrorsComponent, PropLabelComponent } from "@lib/forms"
import { LoginForm } from "./index"

@Component({
    selector: "app-login-form-credential",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule, FormsModule,
        MatFormFieldModule, MatInputModule, MatIconModule,
        PropLabelComponent, CCPropDirective, PropErrorsComponent
    ],
    styles: [`
        :host { display: contents; }
    `],
    template: `
        <mat-form-field>
            <mat-label [ccProp]="form.username" />
            <input matInput [ccProp]="form.username" cdkFocusInitial [ngModel]="form.username.value()"
                autocomplete="off" spellcheck="false" />
            <mat-error [ccProp]="form.username" />
        </mat-form-field>

        <mat-form-field>
            <mat-label [ccProp]="form.password" />
            <input matInput [ccProp]="form.password" [ngModel]="form.password.value()" autocomplete="off"
                spellcheck="false" [type]="form.isPasswordHidden() ? 'password' : 'text'" />
            <mat-icon matSuffix (click)="form.togglePasswordVisibility()">
                {{form.isPasswordHidden() ? 'visibility_off' : 'visibility'}}
            </mat-icon>
            <mat-error [ccProp]="form.password" />
        </mat-form-field>
    `
})
export class LoginFormCredentialComponent {
    @Input({ required: true }) form!: LoginForm
}
