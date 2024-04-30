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
        return this.openFormWithInputAndOutput<TData, TData>(formArgs, matDialogConfig)
    }

    async openFormWithInputAndOutput<TInput, TOutput>(formArgs: IFormDialogArgs<TInput>, matDialogConfig?: MatDialogConfig<IFormDialogArgs<TInput>>): Promise<IFormResult<TOutput>> {
        matDialogConfig ??= new MatDialogConfig<IFormDialogArgs<TInput>>()
        matDialogConfig.disableClose = true
        matDialogConfig.data = formArgs

        const dialogRef = this.#matDialog.open<FormDialogComponent, IFormDialogArgs<TInput>, IFormResult<TOutput>>(FormDialogComponent, matDialogConfig)
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
        const dialogRef = this.#matDialog.open(component, matDialogConfig)
        const output = await firstValueFrom(dialogRef.afterClosed())

        return output
    }

    async confirm(config: ConfirmationDialogConfig): Promise<boolean> {
        const dialogRef = this.#matDialog.open(ConfirmationDialogComponent, this.#buildMatDialogConfig(config))
        const output = await firstValueFrom(dialogRef.afterClosed())

        return !!output
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
