import { Directive } from "@angular/core"

import { IFormItem, FormItem, IFormPropCore } from "@lib/models"

import { FormAction, FormConfig, IFormComponent, IFormComponentModel, IInputAndOutputFormModel, InputAndOutputFormModel } from "./formModels"
import { InputModalityDetector } from "@angular/cdk/a11y"

@Directive()
export abstract class FormItemComponent implements IFormItem {
    // TODO: IMPL _IInternalFormItem
    readonly #formItem = new FormItem()

    addPropGroupChild(child: IFormItem): void {
        this.#formItem.addPropGroupChild(child)
    }

    isModified(): boolean {
        return this.#formItem.isModified()
    }

    validate(): Promise<boolean> {
        return this.#formItem.validate()
    }

    onPropValueChanged(prop: IFormPropCore<any>): void {
        this.#formItem.onPropValueChanged(prop)
    }
}

/*
export class InputAndOutputFormComponent<TInput = any, TOutput = any>
    implements IFormComponent, IInputAndOutputFormModel<TInput, TOutput> {

    readonly model = new InputAndOutputFormModel<TInput, TOutput>()

    readonly inputData = this.model.inputData
    readonly resultData = this.model.resultData
    readonly title = this.model.title
    readonly dataDisplayName = this.model.dataDisplayName
    readonly dialogActionsPosition = this.model.dialogActionsPosition
    readonly canDelete = this.model.canDelete
    readonly canSubmit = this.model.canSubmit
    confirmButtonText = this.model.confirmButtonText
    validationErrorMessage = this.model.validationErrorMessage

    setInputData(inputData: TInput): void {
        this.model.setInputData(inputData)
    }

    setResultData(outputData: TOutput): void {
        this.model.setResultData(outputData)
    }

    initialize(config: FormConfig<TInput>): Promise<void> {
        return this.model.initialize(config)
    }

    submit(action: FormAction): Promise<void> {
        return this.model.submit(action)
    }

    getModel(): IFormComponentModel {
        return this.model
    }
}
*/
