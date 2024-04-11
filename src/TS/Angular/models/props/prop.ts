import { signal, computed, WritableSignal, Signal } from "@angular/core"

import { SignalHelper } from "@lib/utils"

import {
    _IInternalFormItem, IFormPropCore as IFormPropCore, IFormItem,
    _IInternalFormGroupValidationManager, _getHasInternalValidation, _getInternalValidation
} from "./core"
import { FormPropRule, RuleResult, ValidationContext } from "./propRule"
import { FormPropRulesBuilder } from "./propRuleBuilder"
import { _InternalPropGroupValidationManager } from "./propValidation"

export class FormItem implements IFormItem, _IInternalFormItem {
    /**
     * For internal use only.
     */
    get _validation(): _IInternalFormGroupValidationManager {
        return this.#validation ??= new _InternalPropGroupValidationManager()
    }
    #validation?: _IInternalFormGroupValidationManager

    /**
     * For internal use only.
     */
    get _hasValidation(): boolean {
        return this.#validation !== undefined
    }

    #propGroupParent?: IFormItem

    protected _setPropGroupParent(propGroupParent: IFormItem): void {
        this.#propGroupParent = propGroupParent
        if (propGroupParent) {
            propGroupParent.addPropGroupChild(this)
        }
    }

    #propGroupChildren?: IFormItem[]

    get propGroupChildren(): IFormItem[] {
        return this.#propGroupChildren ??= []
    }

    constructor(propGroupParent?: IFormItem) {
        if (propGroupParent) {
            this._setPropGroupParent(propGroupParent)
        }
    }

    addPropGroupChild(child: IFormItem): void {
        this.propGroupChildren.push(child)
    }

    onPropValueChanged(_prop: IFormPropCore): void {
        // TODO: Do we need this?
    }

    // TODO: Can we make this a signal?
    //   Could be problematic.
    //   E.g. if we have a list with items (a list is also intended to be a prop group in the future):
    //   1) item A is modified -> list is modified
    //   2) item B is modified -> list was modified
    //   3) if item A is removed from the list -> we would need to recompute
    //      the list's modified state by checking all items again.

    isModified(): boolean {
        for (const prop in this) {
            if (this[prop] instanceof FormProp) {
                if ((this[prop] as FormProp).isModified()) {
                    return true
                }
            }
        }

        return false
    }

    async validate(): Promise<boolean> {
        let result = true

        const props: FormProp[] = []

        // Preprocess props
        await this.#visitPropsDeep(this, async prop => {
            props.push(prop)

            // TODO: Auto-trimming
            // if (prop instanceof StringProp) {
            //     prop.applyTrimming()
            // }
        })

        for (const prop of props) {
            if (! await prop.validate()) {
                result = false
            }
        }

        return result
    }

    async #visitPropsDeep(group: any, visitor: (prop: FormProp) => Promise<void>): Promise<void> {
        for (const memberName in group) {
            if (group[memberName] instanceof FormProp) {
                await visitor(group[memberName] as FormProp)
            } else if (group[memberName] instanceof FormItem) {
                await this.#visitPropsDeep(group[memberName], visitor)
            }
        }
    }
}

export class PropGroup extends FormItem {
}

type OnValueChangedFn<T> = ((value: T | null) => void) | null

export interface IFormPropControlAdaper {
    setErrorState(errorState: boolean): void
    focus(): void
    /** Hopefully we won't need this anymore when Angular Material moves to Signals.
     * But my hopes are not hight that it ever gets to that point :-(
     */
    detectChanges(): void
    setValue(value: any): void
}

export abstract class FormProp<TData = any> extends FormItem implements IFormPropCore<TData> {
    readonly #isDebugEnabled = false
    readonly id = crypto.randomUUID()
    readonly parent: IFormItem
    readonly #initialValue: WritableSignal<TData>
    readonly initialValue: Signal<TData>
    readonly _value: WritableSignal<TData>
    readonly value: Signal<TData>
    readonly #focusValue = signal<TData | undefined>(undefined)
    label?: string | null
    readonly #hasFocus = signal(false)
    readonly hasFocus = this.#hasFocus.asReadonly()
    readonly #isReadOnly = signal(false)
    readonly isReadOnly = this.#isReadOnly.asReadonly()

    override readonly isModified = computed(() => this._value() !== this.#initialValue())

    constructor(parent: IFormItem, initialValue: TData) {
        super(parent)

        this.parent = parent
        this.#initialValue = signal<TData>(initialValue)
        this.initialValue = this.#initialValue.asReadonly()
        this._value = signal<TData>(initialValue)
        this.value = this._value.asReadonly()
    }

    protected _controlAdapter?: IFormPropControlAdaper
    #isFocusPending?: boolean

    _setControlAdapter(controlAdapter: IFormPropControlAdaper | undefined): void {
        this._controlAdapter = controlAdapter
        if (this._controlAdapter && this.#isFocusPending) {
            this.#isFocusPending = undefined
            this._controlAdapter.focus()
        }
    }

    setLabel(label: string): this {
        this.label = label

        return this
    }

    setInitialValue(initialValue: TData): this {
        this.#initialValue.set(initialValue)
        this._value.set(initialValue)

        return this
    }

    setIsReadOnly(isReadOnly: boolean): this {
        this.#isReadOnly.set(isReadOnly)

        return this
    }

    setRequired(errorMessage?: string): this {
        this.#createDefaultRulesBuilder()
            .required(errorMessage)

        return this
    }

    setNotRequired(): this {
        this.#createDefaultRulesBuilder()
            .notRequired()

        return this
    }

    #createDefaultRulesBuilder() {
        return new FormPropRulesBuilder(this.parent, this)
    }

    focus() {
        if (this._controlAdapter) {
            this._controlAdapter.focus()
        }
        else {
            this.#isFocusPending = true
        }
    }

    // TODO: Dunno if all this async validation is really feasible because
    //   now we have to await the value setter as well :-(
    //   How did Angular's reactive forms implement this?

    /**
    * Intended to be called by the DOM input element directive.
    */
    _onDomInputFocusIn(_ev: FocusEvent): void {
        this.#hasFocus.set(true)

        this.#focusValue.set(this.value())
    }

    /**
    * Intended to be called by the DOM input element directive.
    * If the input has focus then an intermediate validation is triggered.
    */
    async _onDomInput(ev: InputEvent): Promise<void> {
        // We can't use the InputEvent directly in the function signature
        // because Angular provides an Event and not the InputEvent.
        const inputEvent = ev as InputEvent

        const value = this.convertFromDomInputValueToData((inputEvent.currentTarget as any)?.value ?? null)
        if (value === this._value()) return

        this._setValueCore(value)
        this.onValueChanged()

        if (this.hasFocus()) {
            await this.#validateIntermediate()
        }
    }

    /** Called when the users inputs data. */
    protected abstract convertFromDomInputValueToData(inputValue: any): any

    setOnKeyDown(onKeyDownFn: (ev: KeyboardEvent) => void): this {
        this.#onKeyDownFn = onKeyDownFn

        return this
    }

    #onKeyDownFn?: ((ev: KeyboardEvent) => void)

    _onDomInputKeyDown(ev: KeyboardEvent): any {
        this.#onKeyDownFn?.(ev)
        // NOOP
        // Example: Prevent the user from entering the character "ö".
        // if (ev.key === "ö") {
        //     ev.preventDefault()
        // }
    }

    setOnKeyUp(onKeyUpFn: (ev: KeyboardEvent) => void): this {
        this.#onKeyUpFn = onKeyUpFn

        return this
    }

    #onKeyUpFn?: ((ev: KeyboardEvent) => void)

    _onDomInputKeyUp(ev: KeyboardEvent): any {
        this.#onKeyUpFn?.(ev)
    }

    setOnFocusOut(onFocusOutFn: (ev: FocusEvent) => void): this {
        this.#onFocusOutFn = onFocusOutFn

        return this
    }

    #onFocusOutFn?: ((ev: FocusEvent) => void)

    /**
    * Intended to be called by the DOM input element directive.
    * When the user leaves the input, a full validation is performed.
    */
    async _onDomInputFocusOut(ev: FocusEvent): Promise<void> {
        this.#hasFocus.set(false)

        await this._validateFull()

        this.#onFocusOutFn?.(ev)

        if (this.#onFullValueChangedFn && this.#focusValue() !== this.value()) {
            this.#onFullValueChangedFn(this.value())
        }

        this.#focusValue.set(undefined)
    }

    /**
    * Intended to be called by consumer code.
    * Triggers a full validation.
    */
    setValue(value: TData, validate: boolean = true): boolean {
        if (value === this._value()) return false

        if (this.#isDebugEnabled) console.debug(`## prop: setValue (normalizing: ${this.#isNormalizing})}`)

        this._setValueCore(value)
        this.onValueChanged()

        if (validate && !this.#isNormalizing) {
            // TODO: Note that this does not await the validation
            //   because we don't want to make the value setter async.
            //   Dunno if this will work.
            //   How did Angular's reactive forms implement async validation?
            this._validateFull()
        }

        return true
    }

    protected _setValueCore(value: TData) {
        this._value.set(value)
    }

    #onValueChangedFn: OnValueChangedFn<TData> = null

    setOnValueChanged(valueChangedFn: OnValueChangedFn<TData>): this {
        this.#onValueChangedFn = valueChangedFn

        return this
    }

    #onFullValueChangedFn: OnValueChangedFn<TData> = null

    setOnFullValueChanged(valueChangedFn: OnValueChangedFn<TData>): this {
        this.#onFullValueChangedFn = valueChangedFn

        return this
    }

    onValueChanged(): void {
        this.parent.onPropValueChanged(this)

        this.#onValueChangedFn?.(this._value())

        if (this.#onFullValueChangedFn && !this.hasFocus()) {
            this.#onFullValueChangedFn(this.value())
        }
    }

    /**
     *
     * Performs an intermediate validation when user is editing a value.
     * If unsatisfied rules are existing then only these rules are re-evaluated.
     * I.e. we want to inform the user of a successfull fix of an error in this case - nothing more.
     * This also avoids irritating movement of the UI (e.g. in a scroll view)
     * since we are not removing/adding any validation error messages of other props.
     */
    async #validateIntermediate(): Promise<void> {
        const rules = this.#getUnsatisfieldRules()
        if (rules === null) return

        await this.evaluateRules(rules)
    }

    #getUnsatisfieldRules(): FormPropRule[] | null {
        const errors = this.#errors()
        if (!errors?.length) return null

        // Get unsatisfied rules.
        let rules: FormPropRule[] | null = null
        for (const error of errors) {
            if (!error.rule) continue

            if (rules === null) {
                rules = [error.rule]
            } else {
                rules.push(error.rule)
            }
        }

        return rules
    }

    /**
     * Full validation is performed when the DOM input looses focus.
     * Validates rules of this prop and any other rules having this prop as a source prop.
     */
    protected async _validateFull(): Promise<void> {
        if (!_getHasInternalValidation(this.parent) ||
            !_getInternalValidation(this.parent).hasRules
        ) {
            return
        }

        if (this.#isDebugEnabled) console.debug("## prop: validateFull")

        this.normalize()

        // TODO: If we want to support cross-form-group validation rules
        //   then we need to get the rules of the whole form (i.e not only of this form group).
        const rules = _getInternalValidation(this.parent).getRulesBySourceProp(this)
        if (rules === null) return

        await this.evaluateRules(rules)
    }

    #isNormalizing = false

    protected normalize(): void {
        if (this.#isDebugEnabled) console.debug(`## prop: normalize (isNormalizing: ${this.#isNormalizing})`)

        if (this.#isNormalizing) return

        this.#isNormalizing = true
        try {
            this.normalizeCore()
        }
        finally {
            this.#isNormalizing = false
        }
    }

    protected normalizeCore(): void {
        // NOOP
    }

    /**
     * Performs a validation of this prop only.
     * I.e. only the rules having this prop as target are evaluated.
     * @returns whether the prop is valid.
     */
    override async validate(): Promise<boolean> {
        if (!_getHasInternalValidation(this.parent) ||
            !_getInternalValidation(this.parent).hasRules
        ) {
            return true
        }

        this.normalize()

        const rules = _getInternalValidation(this.parent).getRulesByTargetProp(this)
        // TODO: Also take non-rule based errors into account.
        if (rules === null) return true

        return await this.evaluateRules(rules)
    }

    async evaluateRules(rules: FormPropRule[]): Promise<boolean> {
        if (!rules.length) return true

        if (this.#isDebugEnabled) console.debug("## prop: evaluateRules")

        let isValid = true
        const context = new ValidationContext()
        const validator = new Validator()
        for (const rule of rules) {
            context.rule = rule
            context.prop = this

            if (!await validator.validate(context)) {
                isValid = false
            }
        }

        return isValid
    }

    readonly #errors = signal<ValidationError[]>([])
    readonly errors = this.#errors.asReadonly()

    get hasErrors(): boolean {
        return !!this.#errors()?.length
    }

    addError(error: ValidationError): boolean {
        return this.#addErrorCore(error, true)
    }

    #addErrorCore(error: ValidationError, onlyIfNotExists: boolean): boolean {
        if (SignalHelper.push(this.#errors, error, onlyIfNotExists)) {
            this._controlAdapter?.setErrorState(true)
            _getInternalValidation(this.parent).increaseErrorCounter()

            return true
        }

        return false
    }

    removeError(error: ValidationError): boolean {
        if (SignalHelper.remove(this.#errors, error)) {
            if (!this.errors()?.length) {
                this._controlAdapter?.setErrorState(false)
            }

            _getInternalValidation(this.parent).decreaseErrorCounter()

            return true
        }

        return false
    }

    addRuleError(rule: FormPropRule, message: string): ValidationError | null {
        const errors = this.#errors()
        if (errors?.length &&
            errors.findIndex(x => x.rule === rule) >= 0
        ) {
            return null
        }

        const errorToAdd = new ValidationError(rule.id, message, rule)
        this.#addErrorCore(errorToAdd, false)

        return errorToAdd
    }

    removeRuleError(rule: FormPropRule): ValidationError | null {
        const errors = this.#errors()
        if (!errors?.length) return null

        const index = errors.findIndex(error => error.rule === rule)
        if (index === -1) return null

        const errorToRemove = errors[index]

        return this.removeError(errorToRemove)
            ? errorToRemove
            : null
    }
}

class Validator {
    async validate(context: ValidationContext): Promise<boolean> {
        let result: RuleResult = null

        if (context.rule.validate) {
            result = context.rule.validate(context)
        }

        if (result === null && context.rule.validateAsync) {
            result = await context.rule.validateAsync(context)
        }

        if (result !== null) {
            context.rule.prop.addRuleError(context.rule, result)
        }
        else {
            context.rule.prop.removeRuleError(context.rule)
        }

        return result === null
    }
}

export class ValidationError {
    readonly id: string
    readonly message: string
    readonly rule?: FormPropRule

    constructor(id: string, message: string, rule: FormPropRule | undefined) {
        this.id = id
        this.message = message
        this.rule = rule
    }
}
