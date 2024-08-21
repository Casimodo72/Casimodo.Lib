import { ChangeDetectionStrategy, Component, Inject } from "@angular/core"

import { MatButtonModule } from "@angular/material/button"
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"

import { DialogComponent } from "./dialogComponent"
import { DialogTitleComponent } from "./dialog-title.component"
import { DialogConfig } from "./dialogConfig"

export interface InfoDialogConfig extends DialogConfig {
    readonly message: string
}

@Component({
    selector: "app-info-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatDialogModule, MatButtonModule, DialogTitleComponent],
    styleUrls: ["./dialog.scss"],
    template: `
<app-dialog-title
    [title]="data.title"
    [isCancelable]="data.closeStrategy === 'title-button'"
    (cancelled)="close(false)"/>

<div mat-dialog-content>
    <!-- TODO: The dialog-content adds more huge margins :-/
        Together with he title margin issue -> not acceptable.
    -->
    {{data.message}}
</div>

<div mat-dialog-actions align="end">
    <button mat-raised-button color="primary" [mat-dialog-close]="false" cdkFocusInitial>Schließen</button>
</div>
`
})
export class InfoDialog extends DialogComponent<InfoDialog> {
    constructor(
        dialogRef: MatDialogRef<InfoDialog>,
        @Inject(MAT_DIALOG_DATA)
        public readonly data: InfoDialogConfig
    ) {
        super(dialogRef)
    }
}
