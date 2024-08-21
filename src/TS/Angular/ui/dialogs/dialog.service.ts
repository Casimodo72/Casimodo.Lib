import { inject, Injectable } from "@angular/core"
import { firstValueFrom } from "rxjs"

import { ComponentType } from "@angular/cdk/portal"
import { MatDialog, MatDialogConfig, MatDialogRef } from "@angular/material/dialog"

import { base64toBlob } from "@lib/utils"
import { NotificationService } from "@lib/services/notification.service"

import type { IDialogFormArgs, IFormResult } from "@lib/ui/forms"
import { DialogFormDialog } from "@lib/ui/forms/dialog-form-dialog.component"

import { DialogConfig } from "./dialogConfig"
import { ConfirmationDialog, ConfirmationDialogConfig } from "./confirmation-dialog.component"
import { _InternalPdfDialogConfig, PdfDialogConfig, PdfViewerDialog } from "./pdf-viewer-dialog.component"
import { InfoDialog, InfoDialogConfig } from "./info-dialog.component"

@Injectable({
    providedIn: "root"
})
export class DialogService {
    readonly #notifier = inject(NotificationService)
    readonly _matDialogService = inject(MatDialog)

    // TODO: REMOVE?
    // openForm<TData = any>(formArgs: IDialogFormArgs<TData>, matDialogConfig?: MatDialogConfig<IDialogFormArgs<TData>>): Promise<IFormResult<TData>> {
    //     return this.openInputOutputForm<TData, TData>(formArgs, matDialogConfig)
    // }

    async openInputOutputForm<TInput, TOutput>(formArgs: IDialogFormArgs<TInput>, matDialogConfig?: MatDialogConfig<IDialogFormArgs<TInput>>): Promise<IFormResult<TOutput>> {
        matDialogConfig ??= new MatDialogConfig<IDialogFormArgs<TInput>>()
        matDialogConfig.disableClose = true
        matDialogConfig.data = formArgs

        let dialogRef: MatDialogRef<DialogFormDialog, IFormResult<TOutput>> | undefined = undefined
        try {
            dialogRef = this._matDialogService.open<DialogFormDialog, IDialogFormArgs<TInput>, IFormResult<TOutput>>(DialogFormDialog, matDialogConfig)
            dialogRef.componentInstance.dialogService = this
        }
        catch (error) {
            this.#notifier.showError(error)

            return {
                hasSucceeded: false,
                status: "cancelled"
            } satisfies IFormResult<TInput>
        }

        let output = await firstValueFrom(dialogRef.afterClosed())
        if (output === undefined) {
            // Result will be undefined if the close the form e.g. via click on the backdrop.
            output = {
                hasSucceeded: false,
                status: "cancelled"
            } satisfies IFormResult<TInput>
        }

        return output as IFormResult<TOutput>
    }

    async open<T, TInput = any, TOutput = any>(component: ComponentType<T>, matDialogConfig?: MatDialogConfig<TInput>): Promise<TOutput> {
        matDialogConfig ??= {}
        matDialogConfig.disableClose = true
        const dialogRef = this._matDialogService.open(component, matDialogConfig)
        const output = await firstValueFrom(dialogRef.afterClosed())

        return output
    }

    async confirm(config: ConfirmationDialogConfig): Promise<boolean> {
        const dialogRef = this._matDialogService.open(ConfirmationDialog, this.#buildMatDialogConfig(config))
        const output = await firstValueFrom(dialogRef.afterClosed())

        return !!output
    }

    showInfo(config: InfoDialogConfig) {
        this._matDialogService.open(InfoDialog, this.#buildMatDialogConfig(config))
    }

    async showPdf(config: PdfDialogConfig) {
        const blob = base64toBlob(config.base64Data, "application/pdf")
        const blobDataUri = URL.createObjectURL(blob)

        const effectiveConfig: _InternalPdfDialogConfig = {
            ...config,
            blobDataUri: blobDataUri
        }

        this._matDialogService.open(PdfViewerDialog, this.#buildMatDialogConfig(effectiveConfig))
    }

    #buildMatDialogConfig(config: DialogConfig): MatDialogConfig {
        config.closeStrategy ??= "backdrop"

        return {
            hasBackdrop: true,
            disableClose: config.closeStrategy !== "backdrop",
            data: config
        }
    }
}
