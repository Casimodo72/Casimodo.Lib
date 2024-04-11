import { inject, Injectable } from "@angular/core"
import { MatDialog, MatDialogConfig } from "@angular/material/dialog"
import { ConfirmationDialogComponent } from "./confirmation-dialog.component"
import { firstValueFrom } from "rxjs"
import { InternalPdfDialogConfig, PdfViewerDialogComponent } from "./pdf-viewer-dialog.component"
import { base64toBlob } from "@lib/utils"
import { InfoDialogComponent } from "./info-dialog.component"
import { ComponentType } from "@angular/cdk/portal"
import { FormDialogComponent, IFormDialogArgs, IFormResult } from "@lib/forms"

export interface DialogConfig {
    readonly title?: string | undefined
    /**
     * Default: "backdrop"
     */
    closeStrategy?: "title-button" | "backdrop" | undefined
}

export interface ConfirmationDialogConfig extends DialogConfig {
    readonly message: string
    readonly warning?: boolean
}

export interface InfoDialogConfig extends DialogConfig {
    readonly message: string
}

export interface PdfDialogConfig extends DialogConfig {
    readonly fileName?: string | undefined
    readonly base64Data: string
}

export function configureFullScreenDialog<TData = any>(config?: MatDialogConfig<TData>): MatDialogConfig<TData> {
    config ??= new MatDialogConfig<TData>()
    config.height = "100%"
    config.maxHeight = "100%"
    config.width = "100%"
    config.maxWidth = "100%"

    return config
}

export function configureBlackBackgroundDialog<TData = any>(config?: MatDialogConfig<TData>): MatDialogConfig<TData> {
    config ??= new MatDialogConfig<TData>()
    config.panelClass = "app-black-dialog-background"

    return config
}

@Injectable({
    providedIn: "root"
})
export class DialogService {
    readonly #matDialog = inject(MatDialog)

    openForm<TData = any>(formArgs: IFormDialogArgs<TData>, matDialogConfig?: MatDialogConfig<IFormDialogArgs<TData>>): Promise<IFormResult<TData>> {
        return this.openFormWithInputAndResult<TData, TData>(formArgs, matDialogConfig)
    }

    async openFormWithInputAndResult<TInput, TResult>(formArgs: IFormDialogArgs<TInput>, matDialogConfig?: MatDialogConfig<IFormDialogArgs<TInput>>): Promise<IFormResult<TResult>> {
        matDialogConfig ??= new MatDialogConfig<IFormDialogArgs<TInput>>()
        matDialogConfig.disableClose = true
        matDialogConfig.data = formArgs

        const dialogRef = this.#matDialog.open<FormDialogComponent, IFormDialogArgs<TInput>, IFormResult<TResult>>(FormDialogComponent, matDialogConfig)
        let result = await firstValueFrom(dialogRef.afterClosed())

        if (result === undefined) {
            // Result will be undefined if the close the form e.g. via click on the backdrop.
            result = {
                hasSucceeded: false,
                status: "cancelled"
            } satisfies IFormResult<TInput>
        }

        return result as IFormResult<TResult>
    }

    async open<T, TInput = any, TResult = any>(component: ComponentType<T>, matDialogConfig?: MatDialogConfig<TInput>): Promise<TResult> {
        matDialogConfig ??= {}
        matDialogConfig.disableClose = true
        const dialogRef = this.#matDialog.open(component, matDialogConfig)
        const result = await firstValueFrom(dialogRef.afterClosed())

        return result
    }

    async confirm(config: ConfirmationDialogConfig): Promise<boolean> {
        const dialogRef = this.#matDialog.open(ConfirmationDialogComponent, this.#buildMatDialogConfig(config))
        const result = await firstValueFrom(dialogRef.afterClosed())

        return !!result
    }

    showInfo(config: InfoDialogConfig) {
        this.#matDialog.open(InfoDialogComponent, this.#buildMatDialogConfig(config))
    }

    async showPdf(config: PdfDialogConfig) {
        const blob = base64toBlob(config.base64Data, "application/pdf")
        const blobDataUri = URL.createObjectURL(blob)

        const effectiveConfig: InternalPdfDialogConfig = {
            ...config,
            blobDataUri: blobDataUri
        }

        this.#matDialog.open(PdfViewerDialogComponent, this.#buildMatDialogConfig(effectiveConfig))
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
