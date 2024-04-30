import { Directive } from "@angular/core"
import { MatDialogRef } from "@angular/material/dialog"

@Directive()
export abstract class AbstractDialogComponent<T = unknown> {
    protected hasTitleCloseButton = false

    constructor(protected readonly dialogRef: MatDialogRef<T>) { }

    protected close(dialogResult?: any): void {
        this.dialogRef.close(dialogResult)
    }
}
