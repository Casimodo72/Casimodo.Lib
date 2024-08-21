import { Directive } from "@angular/core"

import type { _IInternalFormPartValidationManager, _IInternalFormPart } from "@lib/models/core"

import { FormMode, IInputOutputFormModel, InputOutputFormModel } from "./formModels"
import { FormPartComponent } from "./formPartComponent"

@Directive()
export class InputOutputForm<TInput = any, TOutput = any>
    extends FormPartComponent
    implements IInputOutputFormModel<TInput, TOutput> {

    protected override readonly _formPartModel = new InputOutputFormModel<TInput, TOutput>()
    readonly _formModel = this._formPartModel

    readonly inputData = this._formModel.inputData
    readonly outputData = this._formModel.outputData
    readonly settings = this._formModel.settings

    readonly canDelete = this._formModel.canDelete
    readonly canSubmit = this._formModel.canSubmit

    get mode() {
        return this._formModel.mode
    }
    setMode(mode: FormMode): void {
        this._formModel.setMode(mode)
    }

    setInputData(inputData: TInput): void {
        this._formModel.setInputData(inputData)
    }

    setOutputData(outputData: TOutput): void {
        this._formModel.setOutputData(outputData)
    }

    initializeForm(): Promise<void> {
        return this._formModel.initializeForm()
    }

    submit(): Promise<void> {
        return this._formModel.submit()
    }

    delete(): Promise<void> {
        return this._formModel.delete()
    }
}
