import { computed, Signal, signal, Type, WritableSignal } from "@angular/core"

import { IFormPart, FormPartModel } from "@lib/models"

export type FormMode = "add" | "modify" | "select"

export interface IDialogFormArgs<TData = any> {
    component: Type<any>
    data?: TData,
    mode?: FormMode
}

export interface IFormConfig<TData = any> {
    inputData: TData | null,
    mode?: FormMode
}

export type FormResultStatus = "added" | "modified" | "deleted" | "selected" | "cancelled" | "failed"

export interface IFormResult<TData = any> {
    status: FormResultStatus
    hasSucceeded: boolean
    data?: TData
}

export interface IFormModel extends IFormPart {
}

export class FormModel extends FormPartModel implements IFormModel {
    readonly #busyStateCounter = signal(0)
    readonly isBusy = computed(() => this.#busyStateCounter() > 0)

    enterBusyState(): void {
        this.#busyStateCounter.update(x => x++)
    }

    leaveBusyState(): void {
        this.#busyStateCounter.update(x => x > 0 ? x - 1 : 0)
    }
}

export interface IInputOutputFormModel<TInput = any, TResult = any> extends IFormModel {
    readonly settings: FormSettings

    readonly inputData: Signal<TInput | null>
    readonly outputData: Signal<TResult | null>

    readonly canDelete: Signal<boolean>
    readonly canSubmit: Signal<boolean>

    mode: string
    setMode(mode: FormMode): void
    setInputData(inputData: TInput): void
    setOutputData(resultData: TResult): void
    initializeForm(config: IFormConfig<TInput>): Promise<void>
    submit(): Promise<void>
    delete(): Promise<void>
}

export class FormSettings {
    /** If no validation error message is provided then a default message will be used. */
    validationErrorMessage?: string
}

export class InputOutputFormModel<TInput = any, TResult = any>
    extends FormModel
    implements IInputOutputFormModel<TInput, TResult> {

    get mode() {
        return this._mode
    }
    setMode(mode: FormMode) {
        this._mode = mode
    }
    protected _mode: FormMode = "modify"

    readonly settings = new FormSettings()

    readonly #inputData = signal<TInput | null>(null)
    readonly inputData = this.#inputData.asReadonly()
    readonly outputData = signal<TResult | null>(null)

    readonly canDelete = signal(false)
    canSubmit = signal(true)

    async initializeForm(): Promise<void> {
        // NOOP
    }

    setInputData(inputData: TInput): void {
        this.#inputData.set(inputData)
    }

    setOutputData(outputData: TResult): void {
        this.outputData.set(outputData)
    }

    setCanSubmitSignal(canSubmit: WritableSignal<boolean>) {
        this.canSubmit = canSubmit
    }

    async submit() {
        // NOOP
    }

    async delete() {
        // NOOP
    }
}
