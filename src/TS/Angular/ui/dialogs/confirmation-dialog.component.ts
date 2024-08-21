import { ChangeDetectionStrategy, Component, Inject } from "@angular/core"

import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"

import { DialogComponent } from "./dialogComponent"
import { DialogTitleComponent } from "./dialog-title.component"
import { DialogConfig } from "./dialogConfig"

export interface ConfirmationDialogConfig extends DialogConfig {
    readonly message: string
    readonly warning?: boolean
}

@Component({
    selector: "app-confirmation-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        MatDialogModule, MatButtonModule, MatIconModule,
        DialogTitleComponent],
    styleUrls: ["./dialog.scss"],
    template: `
<app-dialog-title
    [title]="data.title"
    [isCancelable]="data.closeStrategy === 'title-button'"
    (cancelled)="close(false)"/>

<div mat-dialog-content>
    <div [class]="{ 'app-warning': data.warning }">
        @for (line of messageLines; track line) {
            <div>{{line}}</div>
        }
    </div>
</div>

<div mat-dialog-actions align="end">
    <button mat-raised-button [mat-dialog-close]="false">Abbrechen</button>
    <button mat-raised-button color="primary" [mat-dialog-close]="true" cdkFocusInitial>Ok</button>
</div>
`
})
export class ConfirmationDialog extends DialogComponent<ConfirmationDialog> {
    readonly messageLines: string[] = []

    constructor(
        dialogRef: MatDialogRef<ConfirmationDialog>,
        @Inject(MAT_DIALOG_DATA)
        public readonly data: ConfirmationDialogConfig
    ) {
        super(dialogRef)

        this.messageLines = (data.message ?? "").split("\n")
    }
}
