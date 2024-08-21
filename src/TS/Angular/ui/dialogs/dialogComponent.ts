import { Directive } from "@angular/core"

import { MatDialogRef } from "@angular/material/dialog"

@Directive()
/** Base class for dialogs. */
export abstract class DialogComponent<T = unknown> {
    protected hasTitleCloseButton = false

    constructor(protected readonly dialogRef: MatDialogRef<T>) { }

    protected close(dialogResult?: any): void {
        this.dialogRef.close(dialogResult)
    }
}
