import { Type } from "@angular/core"
import { FormMode, IFormResult } from "@lib/ui/forms"

import type { DialogService } from "./dialog.service"
import type { InputOutputFormDialog } from "./inputOutputFormDialog"
import { configureBlackBackgroundDialog } from "./matDialogUtils"

export async function openInputOutputFormDialog<
    TInput,
    TOutput = any,
    TDialog extends InputOutputFormDialog<TInput, TOutput> = InputOutputFormDialog<TInput, TOutput>>
    (
        dialogService: DialogService,
        component: Type<TDialog>,
        data: TInput,
        mode?: FormMode
    ): Promise<IFormResult<TOutput>> {
    return await dialogService.openInputOutputForm<TInput, TOutput>(
        {
            component: component,
            data: data,
            mode: mode,
        },
        configureBlackBackgroundDialog()
    )
}
