import { IFormItem, FormItem, IFormPropCore } from "@lib/models"

export class FormComponent implements IFormItem {
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
