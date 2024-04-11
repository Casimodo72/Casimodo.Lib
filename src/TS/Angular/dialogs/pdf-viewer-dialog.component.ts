import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, Inject, inject } from "@angular/core"
import { FormsModule } from "@angular/forms"
import { DomSanitizer, SafeResourceUrl } from "@angular/platform-browser"
import { MatButtonModule } from "@angular/material/button"
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatInputModule } from "@angular/material/input"
import { MatIconModule } from "@angular/material/icon"
import { DialogConfig } from "./dialog.service"
import { DialogComponentBase } from "./dialogComponentBase"

export interface InternalPdfDialogConfig extends DialogConfig {
    readonly fileName?: string | undefined
    readonly blobDataUri: string
}

@Component({
    selector: "app-pdf-viewer-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CommonModule, MatDialogModule, MatFormFieldModule, MatInputModule, FormsModule, MatButtonModule, MatIconModule],
    styleUrls: ["./dialog.scss"],
    styles: [`
        :host { height: 100% }
    `],
    template: `
        <div class="flex">
            <h1 mat-dialog-title *ngIf="data.title">{{data.title}}</h1>
            <button mat-button *ngIf="data.closeStrategy === 'title-button'" class="app-dialog-title-close-button"
                (click)="close(false)">
                <mat-icon>close</mat-icon>
            </button>
        </div>

        <object type="application/pdf" [data]="dataUri" title="PDF" height="900px" width="800px">
            <p>The PDF cannot be displayed.</p>
        </object>

        <div mat-dialog-actions align="end">
            <button mat-button [mat-dialog-close]="false" cdkFocusInitial>Schließen</button>
        </div>
    `
})
export class PdfViewerDialogComponent extends DialogComponentBase<PdfViewerDialogComponent>{
    readonly #sanitizer = inject(DomSanitizer)
    readonly dataUri: SafeResourceUrl

    constructor(
        dialogRef: MatDialogRef<PdfViewerDialogComponent>,
        @Inject(MAT_DIALOG_DATA)
        public readonly data: InternalPdfDialogConfig
    ) {
        super(dialogRef)

        this.dataUri = this.#sanitizer.bypassSecurityTrustResourceUrl(data.blobDataUri)
    }
}
