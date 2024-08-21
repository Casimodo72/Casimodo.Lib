import { ChangeDetectionStrategy, Component, Inject, inject } from "@angular/core"
import { DomSanitizer, SafeResourceUrl } from "@angular/platform-browser"

import { MatButtonModule } from "@angular/material/button"
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog"

import { DialogComponent } from "./dialogComponent"
import { DialogTitleComponent } from "./dialog-title.component"
import { DialogConfig } from "./dialogConfig"

export interface PdfDialogConfig extends DialogConfig {
    readonly fileName?: string | undefined
    readonly base64Data: string
}

export interface _InternalPdfDialogConfig extends DialogConfig {
    readonly fileName?: string | undefined
    readonly blobDataUri: string
}

@Component({
    selector: "app-pdf-viewer-dialog",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatDialogModule, MatButtonModule, DialogTitleComponent],
    styleUrls: ["./dialog.scss"],
    styles: [`
        :host { height: 100% }
    `],
    template: `
<app-dialog-title
    [title]="data.title"
    [isCancelable]="data.closeStrategy === 'title-button'"
    (cancelled)="close(false)"/>

<object type="application/pdf" [data]="dataUri" title="PDF" height="900px" width="800px">
    <p>The PDF cannot be displayed.</p>
</object>

<div mat-dialog-actions align="end">
    <button mat-button [mat-dialog-close]="false" cdkFocusInitial>Schließen</button>
</div>
    `
})
export class PdfViewerDialog extends DialogComponent<PdfViewerDialog> {
    readonly #sanitizer = inject(DomSanitizer)
    readonly dataUri: SafeResourceUrl

    constructor(
        dialogRef: MatDialogRef<PdfViewerDialog>,
        @Inject(MAT_DIALOG_DATA)
        public readonly data: _InternalPdfDialogConfig
    ) {
        super(dialogRef)

        this.dataUri = this.#sanitizer.bypassSecurityTrustResourceUrl(data.blobDataUri)
    }
}
