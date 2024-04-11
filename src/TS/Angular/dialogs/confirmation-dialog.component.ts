import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Inject } from "@angular/core"
import { MatIconModule } from "@angular/material/icon"
import { MatButtonModule } from "@angular/material/button"
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog"
import { ConfirmationDialogConfig } from "./dialog.service"
import { DialogComponentBase } from "./dialogComponentBase"

@Component({
    selector: "app-confirmation-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatIconModule, MatDialogModule, MatButtonModule],
    styleUrls: ["./dialog.scss"],
    template: `
        <div class="flex">
            <h1 mat-dialog-title *ngIf="data.title">{{data.title}}</h1>
            <button mat-button *ngIf="data.closeStrategy === 'title-button'" class="app-dialog-title-close-button"
                (click)="close(false)"><mat-icon>close</mat-icon></button>
        </div>

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
export class ConfirmationDialogComponent extends DialogComponentBase<ConfirmationDialogComponent> {
    readonly messageLines: string[] = []

    constructor(
        dialogRef: MatDialogRef<ConfirmationDialogComponent>,
        @Inject(MAT_DIALOG_DATA)
        public readonly data: ConfirmationDialogConfig
    ) {
        super(dialogRef)

        this.messageLines = (data.message ?? "").split("\n")
    }
}
