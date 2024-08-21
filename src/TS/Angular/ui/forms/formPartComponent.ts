import { Directive } from "@angular/core"

import {
    IFormPart, FormPartModel, IFormInputModelCore, _IInternalFormPartValidationManager, _IInternalFormPart
} from "@lib/models/core"

@Directive()
export abstract class FormPartComponent implements IFormPart, _IInternalFormPart {
    protected readonly _formPartModel = new FormPartModel()

    /**
     * For internal use only.
     */
    get _validation(): _IInternalFormPartValidationManager {
        return this._formPartModel._validation
    }

    /**
     * For internal use only.
     */
    get _hasValidation(): boolean {
        return this._formPartModel._hasValidation
    }

    addFormPartChild(child: IFormPart): void {
        this._formPartModel.addFormPartChild(child)
    }

    get formPartChildren(): IFormPart[] {
        return this._formPartModel.formPartChildren
    }

    isModified(): boolean {
        return this._formPartModel._isPartModified(this)
    }

    validate(): Promise<boolean> {
        return this._formPartModel._validatePart(this)
    }

    onFormInputValueChanged(prop: IFormInputModelCore<any>): void {
        this._formPartModel.onFormInputValueChanged(prop)
    }
}
