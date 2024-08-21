
import { MatDialogConfig } from "@angular/material/dialog"

export function configureFullScreenDialog<TData = any>(config?: MatDialogConfig<TData>): MatDialogConfig<TData> {
    // TODO: Leave room (hard-coded height) for the app's title bar.
    config ??= new MatDialogConfig<TData>()
    config.height = "100%"
    config.maxHeight = "100%"
    config.width = "100%"
    config.maxWidth = "100%"

    return config
}

// TODO: Rename in order to also make sense in light mode.
export function configureBlackBackgroundDialog<TData = any>(config?: MatDialogConfig<TData>): MatDialogConfig<TData> {
    config ??= new MatDialogConfig<TData>()
    config.panelClass = "app-black-dialog-background"

    return config
}

export function configureNoAutofocusDialog<TData = any>(config?: MatDialogConfig<TData>): MatDialogConfig<TData> {
    config ??= new MatDialogConfig<TData>()
    config.autoFocus = "dialog"

    return config
}
