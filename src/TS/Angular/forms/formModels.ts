import { computed, Signal, signal, Type, WritableSignal } from "@angular/core"

import { IFormItem, FormItem } from "@lib/models"

export type FormMode = "add" | "modify"

export interface IFormDialogArgs<TData = any> {
    mode: FormMode
    component: Type<any>
    data?: TData,
    initialValue?: any
}

export interface FormConfig<TData = any> {
    mode: FormMode
    inputData: TData | null
    initialValue?: any
}

export type FormResultStatus = "added" | "modified" | "deleted" | "cancelled" | "failed"

export interface IFormResult<TData = any> {
    status: FormResultStatus
    hasSucceeded: boolean
    data?: TData
}

export type IFormComponentModel = IEntityFormModel

export interface IFormComponent {
    getModel(): IFormComponentModel
}

export interface IFormModel extends IFormItem {
}

export class FormModel extends FormItem implements IFormModel {
    readonly #busyStateCounter = signal(0)
    readonly isBusy = computed(() => this.#busyStateCounter() > 0)

    enterBusyState(): void {
        this.#busyStateCounter.update(x => x++)
    }

    leaveBusyState(): void {
        this.#busyStateCounter.update(x => x--)
    }
}

export type FormAction = "add" | "modify" | "delete"

export type DialogActionsPosition = "default" | "title"

export interface IInputAndOutputFormModel<TInput = any, TResult = any> extends IFormModel {
    readonly inputData: Signal<TInput | null>
    readonly resultData: Signal<TResult | null>
    readonly title: Signal<string>
    readonly dataDisplayName: Signal<string>
    readonly dialogActionsPosition: Signal<DialogActionsPosition>
    readonly canDelete: Signal<boolean>
    readonly canSubmit: Signal<boolean>
    confirmButtonText?: WritableSignal<string>
    validationErrorMessage?: string
    setInputData(inputData: TInput): void
    setResultData(resultData: TResult): void
    initialize(config: FormConfig<TInput>): Promise<void>
    submit(action: FormAction): Promise<void>
}

export class InputAndOutputFormModel<TInput = any, TResult = any> extends FormModel implements IInputAndOutputFormModel<TInput, TResult> {
    protected readonly _mode = signal<FormMode>("modify")
    readonly mode = this._mode.asReadonly()
    readonly #inputData = signal<TInput | null>(null)
    readonly inputData = this.#inputData.asReadonly()
    readonly resultData = signal<TResult | null>(null)
    readonly title = signal("")
    readonly dataDisplayName = signal("")
    readonly dialogActionsPosition = signal<DialogActionsPosition>("default")
    /** If no confirm-button text is provided then a default text will be used. */
    confirmButtonText?: WritableSignal<string>
    /** If no validation error message is provided then a default message will be used. */
    validationErrorMessage?: string
    readonly canDelete = signal(false)
    canSubmit = signal(true)

    async initialize(config: FormConfig<TInput>): Promise<void> {
        this._mode.set(config.mode)

        if (config.mode !== "modify") {
            this.canDelete.set(false)
        }

        if (config.inputData !== null) {
            this.setInputData(config.inputData)
        }

        return Promise.resolve()
    }

    setInputData(inputData: TInput): void {
        this.#inputData.set(inputData)
    }

    setResultData(resultData: TResult): void {
        this.resultData.set(resultData)
    }

    setCanSubmitSignal(canSubmit: WritableSignal<boolean>) {
        this.canSubmit = canSubmit
    }

    async submit(_action: FormAction): Promise<void> {
        return Promise.resolve()
    }
}

export interface IEntityFormModel<TData = any> extends IInputAndOutputFormModel<TData, TData> {
}

export class EntityFormModel<TData = any> extends InputAndOutputFormModel<TData, TData>
    implements IEntityFormModel<TData> {
}
