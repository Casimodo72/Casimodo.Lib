import { MatDialogRef } from "@angular/material/dialog"

export class DialogComponentBase<T = unknown> {
    protected hasTitleCloseButton = false

    constructor(protected readonly dialogRef: MatDialogRef<T>) { }

    protected close(dialogResult?: any): void {
        this.dialogRef.close(dialogResult)
    }
}
