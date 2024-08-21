import { Directive, signal, WritableSignal } from "@angular/core"

import { InputOutputFormModel, IInputOutputFormModel } from "./formModels"
import { InputOutputForm } from "./inputOutputFormComponent"

export type DialogActionsPosition = "default" | "title"

export abstract class InputOutputDialogFormModel<TInput = any, TOutput = any>
    extends InputOutputFormModel<TInput, TOutput> {
}

interface IDialogFormSettingsConfig {
    title?: string
}

export class DialogFormSettings {
    readonly title = signal("")
    readonly dataDisplayName = signal("")
    readonly dialogActionsPosition = signal<DialogActionsPosition>("default")
    /** If no confirm-button text is provided then a default text will be used. */
    confirmButtonText?: WritableSignal<string>

    constructor(config?: IDialogFormSettingsConfig) {
        if (config?.title) {
            this.title.set(config.title)
        }
    }
}

export interface IDialogForm {
    dialogSettings: DialogFormSettings
    getModel(): IInputOutputFormModel
}

/** Base class for input-output forms that are the content of the DialogFormDialog. */
@Directive()
export abstract class InputOutputDialogForm<TInput = any, TOutput = any>
    extends InputOutputForm<TInput, TOutput>
    implements IDialogForm {
    readonly dialogSettings = new DialogFormSettings()
    getModel(): IInputOutputFormModel {
        return this
    }
}
