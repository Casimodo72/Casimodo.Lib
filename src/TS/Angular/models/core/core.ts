import { Injector, Signal, WritableSignal, computed, signal } from "@angular/core"
// import {
//     _IInternalFormPart, IFormInputModelCore, IFormPart,
//     _IInternalFormPartValidationManager, _getHasInternalValidation, _getInternalValidation
// } from "./base"

import { PropPath, PropPathSelection } from "@lib/data/utils"
import { SignalHelper } from "@lib/utils"
import { Subject, Observable } from "rxjs"

import { IconType } from "@lib/ui/components/icons/iconType"
import { DateTime } from "luxon"

//#region Base

export interface IFormPart {
    addFormPartChild(child: IFormPart): void
    formPartChildren: IFormPart[]
    isModified(): boolean
    validate(): Promise<boolean>
    onFormInputValueChanged(inputModel: IFormInputModelCore): void
}

/**
 * For internal use only.
 */
export interface _IInternalFormPart {
    /**
    * For internal use only.
    */
    get _validation(): _IInternalFormPartValidationManager
    /**
    * For internal use only.
    */
    get _hasValidation(): boolean
}

export interface IFormInputModelCore<T = any> {
    value: Signal<T>
    label?: string | null
    addRuleError(rule: FormRule, message: string): ValidationError | null
    removeRuleError(rule: FormRule): ValidationError | null
    _setIsRequiredCore(isRequired: boolean): void
}

/**
 * For internal use only.
 */
export interface _IInternalFormPartValidationManager {
    get instanceRules(): FormRule[]

    get hasInstanceRules(): boolean

    addInstanceRule(rule: FormRule): void

    removeInstanceRuleById(ruleId: string): void

    get hasRules(): boolean
    get hasRequiredRule(): boolean

    getRulesBySource(source: IFormInputModelCore): FormRule[] | null

    getRulesByTarget(target: IFormInputModelCore): FormRule[] | null

    readonly _hasErrors: Signal<boolean>

    increaseErrorCounter(): void

    decreaseErrorCounter(): void
}

export function _getInternalValidation(part: IFormPart): _IInternalFormPartValidationManager {
    return (part as unknown as _IInternalFormPart)._validation
}

export function _getHasInternalValidation(part: IFormPart): boolean {
    return (part as unknown as _IInternalFormPart)._hasValidation
}

//#endregion Base

//#region FormPart

export async function visitInputModelsAsync(formPart: any, deep: boolean, visit: (inputModel: FormInputModel) => Promise<boolean>): Promise<boolean> {
    for (const key in formPart) {
        const prop = formPart[key]
        if (prop instanceof FormInputModel) {
            if (await visit(prop as FormInputModel)) {
                return true
            }
        } else if (deep && prop instanceof FormPartModel) {
            if (await visitInputModelsAsync(prop, true, visit)) {
                return true
            }
        }
    }

    return false
}

export function visitInputModels(formPart: any, deep: boolean, visit: (inputModel: FormInputModel) => boolean): boolean {
    for (const key in formPart) {
        const prop = formPart[key]
        if (prop instanceof FormInputModel) {
            if (visit(prop)) {
                return true
            }
        } else if (deep && prop instanceof FormPartModel) {
            if (visitInputModels(formPart[key], true, visit)) {
                return true
            }
        }
    }

    return false
}

export class FormPartModel implements IFormPart, _IInternalFormPart {
    /**
     * For internal use only.
     */
    get _validation(): _IInternalFormPartValidationManager {
        return this.#validation ??= new _InternalFromPartValidationManager()
    }
    #validation?: _IInternalFormPartValidationManager

    /**
     * For internal use only.
     */
    get _hasValidation(): boolean {
        return this.#validation !== undefined
    }

    #formPartParent?: IFormPart

    #setFormPartParent(parentFormPart: IFormPart): void {
        this.#formPartParent = parentFormPart
        if (parentFormPart) {
            parentFormPart.addFormPartChild(this)
        }
    }

    #formPartChildren?: IFormPart[]

    get formPartChildren(): IFormPart[] {
        return this.#formPartChildren ??= []
    }

    constructor(parent?: IFormPart) {
        if (parent) {
            this.#setFormPartParent(parent)
        }
    }

    addFormPartChild(child: IFormPart): void {
        this.formPartChildren.push(child)
    }

    onFormInputValueChanged(_inputModel: IFormInputModelCore): void {
        // TODO: Do we need this?
    }

    // TODO: Can we make this a signal?
    //   Could be problematic.
    //   E.g. if we have a list with items (a list is also intended to be a form part in the future):
    //   1) item A is modified -> list is modified
    //   2) item B is modified -> list was modified
    //   3) if item A is removed from the list -> we would need to recompute
    //      the list's modified state by checking all items again.

    isModified(): boolean {
        return this._isPartModified(this)
    }

    _isPartModified(formPart: IFormPart): boolean {
        return visitInputModels(formPart, true, input => input.isModified())
    }

    validate(): Promise<boolean> {
        return this._validatePart(this)
    }

    async _validatePart(formPart: IFormPart): Promise<boolean> {
        let result = true

        await visitInputModelsAsync(formPart, true, async input => {
            // TODO: Auto-trimming
            // if (input instanceof StringInputModel) {
            //     input.applyTrimming()
            // }

            if (!input._getIsValidationDisabled() && ! await input.validate()) {
                result = false
            }

            return false
        })

        return result
    }
}

//#endregion FormPart

//#region Items

export interface IItemModel {
    readonly id: string
    readonly isSelected: Signal<boolean>
    readonly canChangeSelection: Signal<boolean>
    /**
     * Tries to set the selection state.
     * @returns whether the selection state changed.
     */
    setIsSelected(isSelected: boolean): boolean
    readonly isCurrent: WritableSignal<boolean>
}

export abstract class ItemModel extends FormPartModel implements IItemModel {
    readonly id: string
    protected readonly _isSelected = signal(false)
    readonly isSelected = this._isSelected.asReadonly()
    readonly canChangeSelection = signal(true)
    readonly isCurrent = signal(false)

    constructor(id?: string) {
        super()

        this.id = id ?? `item${crypto.randomUUID()}`
    }

    /**
     * @inheritdoc
     */
    setIsSelected(isSelected: boolean): boolean {
        if (!this.canChangeSelection() || this._isSelected() === isSelected) {
            return false
        }

        this._isSelected.set(isSelected)

        return true
    }
}

export interface IDataItemModel<T = any> extends IItemModel {
    readonly data: T
}

export abstract class DataItemModel<T = any> extends ItemModel
    implements IDataItemModel<T> {
    readonly data: T

    constructor(data: T) {
        super((data as any).Id ?? (data as any).id)

        this.data = data
    }
}

export interface IMutableDataItemModel<T = any> extends IItemModel {
    readonly data: Signal<T>
    mutateData(delta: Partial<T>): void
}

export class MutableDataItemModel<T = any> extends ItemModel
    implements IMutableDataItemModel<T> {
    readonly #data: WritableSignal<T>
    readonly data: Signal<T>

    constructor(data: T) {
        super((data as any).Id ?? (data as any).id)

        this.#data = signal(data)
        this.data = this.#data.asReadonly()
    }

    mutateData(delta: Partial<T>): void {
        const data = Object.assign({}, this.#data(), delta)
        this.#data.set(data)
    }
}

//#endregion Items

//#region FormInputModel

export interface IFormInputControlAdaper {
    setErrorState(errorState: boolean): void
    focus(): void
    /** Hopefully we won't need this anymore when Angular Material moves to Signals.
     * But my hopes are not hight that it ever gets to that point :-(
     */
    detectChanges(): void
    setValue(value: any): void
}

type OnValueChangedFn<T> = ((value: T | null) => void) | null

export abstract class FormInputModel<TData = any>
    extends FormPartModel implements IFormInputModelCore<TData> {

    readonly #isDebugEnabled = false
    readonly id = crypto.randomUUID()
    readonly parent: IFormPart
    readonly #initialValue: WritableSignal<TData>
    readonly initialValue: Signal<TData>
    readonly _value: WritableSignal<TData>
    readonly value: Signal<TData>
    #valueChanged?: Subject<TData>
    #valueChanged$?: Observable<TData>
    get valueChanged(): Observable<TData> {
        if (!this.#valueChanged$) {
            this.#valueChanged = new Subject<TData>()
            this.#valueChanged$ = this.#valueChanged.asObservable()
        }

        return this.#valueChanged$
    }
    readonly #focusValue = signal<TData | undefined>(undefined)
    label?: string | null
    readonly #hasFocus = signal(false)
    readonly hasFocus = this.#hasFocus.asReadonly()
    readonly #isReadOnly = signal(false)
    readonly isReadOnly = this.#isReadOnly.asReadonly()
    readonly #isDisabled = signal(false)
    readonly isDisabled = this.#isDisabled.asReadonly()

    override readonly isModified = computed(() => !this._isValueEqual(this.value(), this.#initialValue()))

    constructor(parent: IFormPart, initialValue: TData) {
        super(parent)

        this.parent = parent
        this.#initialValue = signal<TData>(initialValue)
        this.initialValue = this.#initialValue.asReadonly()
        this._value = signal<TData>(initialValue)
        this.value = this._value.asReadonly()
    }

    abstract setRules(rulesBuildFn: (rulesBuilder: FormRulesBuilder<TData>) => void): this

    protected _controlAdapter?: IFormInputControlAdaper
    #isFocusPending?: boolean

    _setControlAdapter(controlAdapter: IFormInputControlAdaper | undefined): void {
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

    setHint(hint: string): this {
        this.hint.set(hint)

        return this
    }
    readonly hint = signal<string | undefined>(undefined)

    get isInactiveOnModify() {
        return !!this.#isInactiveOnModify
    }

    setIsInactiveOnModify(isInactiveOnModify?: boolean): this {
        this.#isInactiveOnModify = isInactiveOnModify === undefined || isInactiveOnModify === true

        return this
    }

    #isInactiveOnModify?: boolean

    get targetProp(): PropPath | undefined {
        return this.#targetProp
    }
    #targetProp?: PropPath

    setTarget<TTarget = any>(targetProp: PropPathSelection<TTarget>): this {
        this.#targetProp = PropPath.fromSelection(targetProp)

        return this
    }

    setInitialValue(initialValue: TData): this {
        this.#initialValue.set(initialValue)
        this._value.set(initialValue)

        return this
    }

    setIsReadOnly(isReadOnly?: boolean): this {
        this.#isReadOnly.set(isReadOnly === undefined || isReadOnly === true)

        return this
    }

    setIsDisabled(isDisabled: boolean): this {
        this.#isDisabled.set(isDisabled)

        return this
    }

    get hasCommands() {
        return !!this.#commandList?.items()?.length
    }
    readonly commands = computed(() => this.#ensureCommandList().items())
    #ensureCommandList() {
        return this.#commandList ??= new ListModel<UICommand>()
    }
    #commandList?: ListModel<UICommand>

    // TODO: Introduce a generalized "events" Observable (like they did in Angular 18's form control)
    // instead of having the overhead of dedicated observables.
    get commandTriggered(): Observable<UICommandEvent> {
        return (this.#commandTriggeredSubject ??= new Subject()) as Observable<UICommandEvent>
    }
    #commandTriggeredSubject?: Subject<UICommandEvent>

    #commandHandlerFn?: UICommandHandlerFn

    setCommandHandler(commandHandlerFn: UICommandHandlerFn) {
        this.#commandHandlerFn = commandHandlerFn
    }

    addCommand(commandConfig: IUICommandConfig): this {
        const consumerHandlerFn = commandConfig.onTriggered

        commandConfig.onTriggered = (event) => {
            consumerHandlerFn?.(event)
            this.#commandHandlerFn?.(event)
            this.#commandTriggeredSubject?.next(event)
        }
        this.#ensureCommandList().add(new UICommand(commandConfig))

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

    _setIsRequiredCore(isRequired: boolean) {
        this.#isRequired.set(isRequired)
    }
    readonly #isRequired = signal(false)
    readonly isRequired = this.#isRequired.asReadonly()

    _setInjector(injector: Injector) {
        this._injector = injector
    }
    protected _injector?: Injector

    #createDefaultRulesBuilder() {
        return new FormRulesBuilder(this.parent, this)
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

        const value = this._convertFromDomInputValueToData((inputEvent.currentTarget as any)?.value ?? null)
        if (this._isValueEqual(value, this.value())) {
            return
        }

        this._setValueCore(value)
        this._onValueChanged()

        if (this.hasFocus()) {
            await this.#validateIntermediate()
        }
    }

    /** Called when the users inputs data. */
    protected abstract _convertFromDomInputValueToData(inputValue: any): any

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

        if (this.#onFullValueChangedFn && !this._isValueEqual(this.#focusValue(), this.value())) {
            this.#onFullValueChangedFn(this.value())
        }

        this.#focusValue.set(undefined)
    }

    /**
    * Intended to be called by consumer code.
    * Triggers a full validation.
    */
    setValue(value: TData, validate: boolean = true): boolean {
        if (this._isValueEqual(value, this.value())) return false

        if (this.#isDebugEnabled) console.debug(`## input: setValue (normalizing: ${this.#isNormalizing})}`)

        this._setValueCore(value)
        this._onValueChanged()

        if (validate && !this.#isValidationDisabled && !this.#isNormalizing) {
            // TODO: Note that this does not await the validation
            //   because we don't want to make the value setter async.
            //   Dunno if this will work.
            //   How did Angular's reactive forms implement async validation?
            this._validateFull()
        }

        return true
    }

    protected _isValueEqual(value: TData | null | undefined, value2: TData | null | undefined): boolean {
        if (value == null && value2 == null) return true
        if (value == null || value2 == null) return false

        return value === value2
    }

    protected _setValueCore(value: TData) {
        this._value.set(value)
    }

    /** Sets a value-changed callback singleton function. */
    setOnValueChanged(valueChangedFn: OnValueChangedFn<TData>): this {
        this.#onValueChangedFn = valueChangedFn

        return this
    }
    #onValueChangedFn: OnValueChangedFn<TData> = null

    /** Sets a full-value-changed callback singleton function. */
    setOnFullValueChanged(valueChangedFn: OnValueChangedFn<TData>): this {
        this.#onFullValueChangedFn = valueChangedFn

        return this
    }
    #onFullValueChangedFn: OnValueChangedFn<TData> = null

    protected _onValueChanged(): void {
        this.parent.onFormInputValueChanged(this)

        if (this.#valueChanged) {
            this.#valueChanged.next(this.value())
        }
        this.#onValueChangedFn?.(this.value())

        if (this.#onFullValueChangedFn && !this.hasFocus()) {
            this.#onFullValueChangedFn(this.value())
        }
    }

    _setIsValidationDisabled(isValidationDisabled?: boolean) {
        this.#isValidationDisabled = isValidationDisabled === undefined || isValidationDisabled === true
    }
    #isValidationDisabled?: boolean

    _getIsValidationDisabled() {
        return !!this.#isValidationDisabled
    }

    /**
     *
     * Performs an intermediate validation when user is editing a value.
     * If unsatisfied rules are existing then only these rules are re-evaluated.
     * I.e. we want to inform the user of a successfull fix of an error in this case - nothing more.
     * This also avoids irritating movement of the UI (e.g. in a scroll view)
     * since we are not removing/adding any validation error messages of other dependant (validation-wise) input models.
     */
    async #validateIntermediate(): Promise<void> {
        const rules = this.#getUnsatisfieldRules()
        if (rules === null) return

        await this.#evaluateRules(rules)
    }

    #getUnsatisfieldRules(): FormRule[] | null {
        const errors = this.#errors()
        if (!errors?.length) return null

        let rules: FormRule[] | null = null
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
     * Validates rules of this input model and any other rules having this input model as a source.
     */
    protected async _validateFull(): Promise<void> {
        if (this.#isValidationDisabled) return

        if (!_getHasInternalValidation(this.parent) ||
            !_getInternalValidation(this.parent).hasRules
        ) {
            return
        }

        if (this.#isDebugEnabled) console.debug("## input: validateFull")

        this._normalize()

        // TODO: If we want to support cross-form-group validation rules
        //   then we need to get the rules of the whole form (i.e not only of this form part).
        const rules = _getInternalValidation(this.parent).getRulesBySource(this)
        if (rules === null) return

        await this.#evaluateRules(rules)
    }

    #isNormalizing = false

    protected _normalize(): void {
        if (this.#isDebugEnabled) console.debug(`## input: normalize (isNormalizing: ${this.#isNormalizing})`)

        if (this.#isNormalizing) return

        this.#isNormalizing = true
        try {
            this._normalizeCore()
        }
        finally {
            this.#isNormalizing = false
        }
    }

    protected _normalizeCore(): void {
        // NOOP
    }

    /**
     * Performs a validation of this input model only.
     * I.e. only the rules having this input model as target are evaluated.
     * @returns whether the input value is valid.
     */
    override async validate(): Promise<boolean> {
        if (this.#isValidationDisabled) return true

        if (!_getHasInternalValidation(this.parent) ||
            !_getInternalValidation(this.parent).hasRules
        ) {
            return true
        }

        this._normalize()

        const rules = _getInternalValidation(this.parent).getRulesByTarget(this)
        // TODO: ? Maybe also take non-rule based errors into account.
        if (rules === null) return true

        return await this.#evaluateRules(rules)
    }

    async #evaluateRules(rules: FormRule[]): Promise<boolean> {
        if (!rules.length) return true

        if (this.#isDebugEnabled) console.debug("## input: evaluateRules")

        let isValid = true
        const context = new FormRuleValidationContext()
        const validator = new Validator()
        for (const rule of rules) {
            context.rule = rule
            context.target = this

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

    hasErrorById(errorId: string): boolean {
        if (!this.hasErrors) return false

        return this.errors().find(error => error.id === errorId) !== undefined
    }

    addError(error: ValidationError): boolean {
        return this.#addErrorCore(error, true)
    }

    removeError(error: ValidationError): boolean {
        const errors = this.#errors()
        if (!errors?.length) return false

        if (errors.find(error => error === error || error.id === error.id) === undefined) {
            return false
        }

        return this.#removeErrorCore(error)
    }

    #addErrorCore(error: ValidationError, onlyIfNotExists: boolean): boolean {
        if (SignalHelper.push(this.#errors, error, onlyIfNotExists)) {
            this._controlAdapter?.setErrorState(true)
            _getInternalValidation(this.parent).increaseErrorCounter()

            return true
        }

        return false
    }

    addRuleError(rule: FormRule, message: string): ValidationError | null {
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

    removeRuleError(rule: FormRule): ValidationError | null {
        const errors = this.#errors()
        if (!errors?.length) return null

        const index = errors.findIndex(error => error.rule === rule)
        if (index === -1) return null

        const errorToRemove = errors[index]

        return this.#removeErrorCore(errorToRemove)
            ? errorToRemove
            : null
    }

    #removeErrorCore(error: ValidationError): boolean {
        if (SignalHelper.remove(this.#errors, error)) {
            if (!this.errors()?.length) {
                this._controlAdapter?.setErrorState(false)
            }

            _getInternalValidation(this.parent).decreaseErrorCounter()

            return true
        }

        return false
    }

}

class Validator {
    async validate(context: IFormRuleValidationContext): Promise<boolean> {
        let result: FormRuleResult = null

        if (context.rule.validate) {
            result = context.rule.validate(context)
        }

        if (result === null && context.rule.validateAsync) {
            result = await context.rule.validateAsync(context)
        }

        if (result !== null) {
            context.rule.target.addRuleError(context.rule, result)
        }
        else {
            context.rule.target.removeRuleError(context.rule)
        }

        return result === null
    }
}

//#endregion FormInputModel

//#region ListModel

type SelectionMode = "single" | "multiple"
type SelectionState = "empty" | "some" | "all"

export interface IListSelectionModel<T extends IItemModel> {
    readonly mode: SelectionMode
    readonly items: Signal<T[]>
    readonly state: Signal<SelectionState>
    setItem(item: T, isSelected: boolean): boolean
    setAll(areSelected: boolean): boolean
    readonly isEmpty: Signal<boolean>
    readonly hasSome: Signal<boolean>
    readonly hasAll: Signal<boolean>
}

// TODO: This may not be scalable. I tried to mimic the Angular Material selection model a bit,
// but for huge amounts of selected items this may not be the best approach.
// Alternative: Don't keep a list of selected items, but just keep the selected state.
// TODO: Fix ListModel: Selection contains duplicate items.
class ListSelectionModel<T extends IItemModel> implements IListSelectionModel<T> {
    readonly #list: ListModel<T>
    readonly mode: SelectionMode = "multiple"
    readonly #items = signal<T[]>([])
    readonly items = this.#items.asReadonly()

    readonly isEmpty = computed<boolean>(
        () => this.items().length === 0)

    readonly hasSome = computed<boolean>(() => {
        const selectedCount = this.items().length
        const listCount = this.#list.items().length

        return selectedCount !== 0 && selectedCount !== listCount
    })

    readonly hasAll = computed<boolean>(
        () => this.items().length === this.#list.items().length)

    readonly state = computed<SelectionState>(() => {
        const selectedCount = this.items().length
        const listCount = this.#list.items().length

        return selectedCount === 0
            ? "empty"
            : selectedCount === listCount
                ? "all"
                : "some"
    })

    constructor(list: ListModel<T>, mode: SelectionMode) {
        this.#list = list
        this.mode = mode
    }

    /**
    * Tries to select the given items.
    * If the selection mode is "single" then all other items will be deselected.
    * @returns whether the selection was changed - either on the selected list of items of the items itself.
    */
    select(...items: T[]): boolean {
        if (!items?.length) return false

        items = items.filter(x => this.#list.items().includes(x))

        let changed = false

        if (this.mode === "single") {
            // Select the first item only.
            const item = items[0]

            if (item.canChangeSelection()) {
                if (item.setIsSelected(true)) {
                    changed = true
                }

                if (this.#items().length !== 1 || this.#items()[0] !== item) {
                    this.#items.set([item])
                    changed = true
                }
            }

            return changed
        }

        for (const item of items) {
            if (item.canChangeSelection()) {
                if (item.setIsSelected(true)) {
                    changed = true
                }

                if (SignalHelper.push(this.#items, item, true)) {
                    changed = true
                }
            }
        }

        return changed
    }

    selectAll(): boolean {
        return this.select(...this.#list.items())
    }

    /**
     * Tries to deleselect the given items.
     * @returns whether the selection was changed - either on the selected list of items of the items itself.
     */
    deselect(...items: T[]): boolean {
        if (!items?.length) return false

        items = items.filter(x => this.#list.items().includes(x))

        let changed = false

        for (const item of items) {
            if (item.canChangeSelection()) {
                if (item.setIsSelected(false)) {
                    changed = true
                }

                if (SignalHelper.remove(this.#items, item)) {
                    changed = true
                }
            }
        }

        return changed
    }

    deselectAll(): boolean {
        return this.deselect(...this.#list.items())
    }

    clear(): boolean {
        return this.deselectAll()
    }

    /**
     * Tries to set the selection state of the given item.
     * If the selection mode is "single" then all other items will be deselected.
     * @param item
     * @param isSelected
     * @returns whether the selection was changed on any items involved.
     */
    setItem(item: T, isSelected: boolean): boolean {
        return isSelected
            ? this.select(item)
            : this.deselect(item)
    }

    setAll(areSelected: boolean): boolean {
        return areSelected
            ? this.selectAll()
            : this.deselectAll()
    }
}

type ListModelOptions = {
    selectionMode?: SelectionMode
}

export class ListModel<T extends IItemModel> extends FormPartModel {
    protected readonly _items = signal<T[]>([])
    readonly items = this._items.asReadonly()
    readonly count = computed(() => this._items().length)
    protected readonly _current = signal<T | null>(null)
    readonly current = this._current.asReadonly()
    readonly #selection: ListSelectionModel<T>
    readonly isEmpty = computed(() => this._items().length === 0)

    constructor(options?: ListModelOptions) {
        super()

        // TODO: Do we want to allow for dynamically changing the selection mode?
        this.#selection = new ListSelectionModel<T>(this, options?.selectionMode ?? "single")
    }

    get selection(): IListSelectionModel<T> {
        return this.#selection
    }

    clear() {
        this._items.set([])
        this._current.set(null)
        this.#selection.clear()
    }

    findPrevious(item: T): T | undefined {
        const index = this._items().indexOf(item)
        if (index <= 0) return undefined

        return this._items()[index - 1]
    }

    findNext(item: T): T | undefined {
        const index = this._items().indexOf(item)
        if (index < 0) return undefined

        return this._items()[index + 1]
    }

    last(): T | undefined {
        const items = this._items()

        return items.length > 0
            ? items[items.length - 1]
            : undefined
    }

    includes(item: T) {
        return this.items().find(x => x === item)
    }

    findById(id: string): T | undefined {
        return this.items().find(x => x.id === id)
    }

    setCurrent(item: T | null | undefined) {
        item ??= null

        if (this._current() === item) return

        for (const item2 of this._items()) {
            item2.isCurrent.set(false)
        }

        if (item) {
            item.isCurrent.set(true)
        }

        this._current.set(item)
    }

    setCurrentByIndex(index: number) {
        const item = this._items()[index]
        if (!item) return

        this.setCurrent(item)
    }

    setCurrentById(id: string) {
        const item = this._items().find(x => x.id === id)

        this.setCurrent(item)
    }

    setItems(items: T[]) {
        this.clear()
        this._items.set([...items])
    }

    insertFirst(item: T) {
        SignalHelper.insertFirst(this._items, item)
    }

    add(item: T) {
        this.addCore(item)
    }

    addRange(items: T[]) {
        this.addRangeCore(items)
    }

    protected addCore(item: T) {
        SignalHelper.push(this._items, item)
    }

    insertAfter(contextItem: T, item: T): boolean {
        const items = this._items()
        const contextIndex = items.indexOf(contextItem)
        // TODO: Should we throw errors or just return false?
        if (contextIndex === -1) return false

        if (items.includes(item)) return false

        if (contextIndex === items.length - 1) {
            SignalHelper.push(this._items, item)
        }
        else {
            SignalHelper.splice(this._items, contextIndex + 1, 0, item)
        }

        return true
    }

    insertBefore(contextItem: T, item: T): boolean {
        const items = this._items()
        const contextIndex = items.indexOf(contextItem)
        // TODO: Should we throw errors or just return false?
        if (contextIndex === -1) return false

        if (items.includes(item)) return false

        SignalHelper.splice(this._items, contextIndex, 0, item)

        return true
    }

    protected addRangeCore(items: T[]) {
        if (!items?.length) return

        SignalHelper.pushRange(this._items, ...items)
    }

    remove(item: T): boolean {
        return this.removeCore(item)
    }

    removeAndMoveCurrent(item: T, direction: "next" | "previous" = "next"): boolean {
        const index = this._items().indexOf(item)
        if (index === -1) return false

        if (this._current() === item) {
            this.setCurrentToAdjacentItem(index, direction)
        }

        return this.removeCore(item)
    }

    isLast(item: T): boolean {
        const items = this.items()
        const index = items.indexOf(item)

        return index === items.length - 1
    }

    setCurrentToAdjacentItem(contextIndex: number, direction: "next" | "previous") {
        if (contextIndex === -1) return

        // Deselect if no adjacent item exists.
        if (this._items().length === 1) {
            this._current.set(null)

            return
        }

        if (direction === "next") {
            if (contextIndex < this._items().length - 1) {
                this.setCurrentByIndex(contextIndex + 1)
            } else {
                direction = "previous"
            }
        }

        if (direction === "previous") {
            if (contextIndex > 0) {
                this.setCurrentByIndex(contextIndex - 1)
            }
        }
    }

    protected removeCore(item: T): boolean {
        const wasRemoved = SignalHelper.remove(this._items, item)

        if (wasRemoved && this._current() === item) {
            this._current.set(null)
        }

        return wasRemoved
    }

    replace(oldItem: T, newItem: T): boolean {
        return this.replaceCore(oldItem, newItem)
    }

    protected replaceCore(oldItem: T, newItem: T): boolean {
        const wasReplaced = SignalHelper.replace(this._items, oldItem, newItem)

        if (wasReplaced && this._current() === oldItem) {
            this._current.set(newItem)
        }

        return wasReplaced
    }
}

//#endregion ListModel

//#region Commands

export interface UICommandEvent {
    readonly command: UICommand
}

export type UICommandHandlerFn = (event: UICommandEvent) => void

export interface IUICommandConfig {
    id?: string
    text?: string
    // TODO: Not good to have that dependency here.
    icon?: IconType
    onTriggered?: UICommandHandlerFn
}

export class UICommand extends ItemModel {
    readonly type?: string
    readonly text?: string
    readonly icon?: string
    readonly onTriggered?: UICommandHandlerFn

    constructor(config: IUICommandConfig) {
        super(config.id)

        this.text = config.text
        this.icon = config.icon
        this.onTriggered = config.onTriggered
    }

    trigger() {
        this.onTriggered?.({ command: this })
    }
}

//#endregion Commands

//#region Rule

export type FormRuleResult = string | null
export type FormRuleValidationFn = (context: IFormRuleValidationContext) => FormRuleResult
export type FormRuleAsyncValidationFn = (context: IFormRuleValidationContext) => Promise<FormRuleResult>
const noopValidationFn: FormRuleValidationFn = () => null
const noopAsyncValidationFn: FormRuleAsyncValidationFn = () => Promise.resolve(null)
const emptyFormInputModelArray: IFormInputModelCore[] = []

export class FormRuleValidationContext {
    target!: FormInputModel
    rule!: FormRule
}

export interface IFormRuleValidationContext {
    readonly target: FormInputModel
    readonly rule: FormRule
    // TODO: REMOVE: readonly errorMessage?: string
}

export interface IFormRuleConfig {
    id: string
    target: IFormInputModelCore
    sources?: IFormInputModelCore[]
    errorMessage?: string
    errorMessageFn?: FormRuleMessageFn
    validate?: FormRuleValidationFn | undefined
    validateAsync?: FormRuleAsyncValidationFn | undefined
}

type FormRuleMessageFn = (context: IFormRuleValidationContext, args?: any) => string | null | undefined

// TODO: We are relying on the group and input model instances which
//   will make implementation of static rules (per type) impossible :-(
//   Maybe static rules are actually not implementable in TS/JS.
// TODO: We will need an error message function (taking the value as an argument).
export class FormRule {
    readonly id: string
    readonly target: IFormInputModelCore
    readonly sources?: IFormInputModelCore[]
    readonly #errorMessage?: string
    readonly #errorMessageFn?: FormRuleMessageFn
    readonly validate?: FormRuleValidationFn | undefined
    readonly validateAsync?: FormRuleAsyncValidationFn | undefined

    constructor(config: IFormRuleConfig) {
        this.id = config.id
        this.target = config.target
        this.sources = config.sources ?? emptyFormInputModelArray
        if (config.errorMessage) {
            this.#errorMessage = config.errorMessage
        }
        if (config.errorMessageFn) {
            this.#errorMessageFn = config.errorMessageFn
        }
        this.validate = config.validate
        this.validateAsync = config.validateAsync
    }

    isSource(model: IFormInputModelCore): boolean {
        return this.target === model || !!this.sources?.includes(model)
    }

    isTarget(model: IFormInputModelCore): boolean {
        return this.target === model
    }

    buildErrorMessage(context: IFormRuleValidationContext, args?: object): string | undefined {
        if (this.#errorMessage != null) {
            return this.#errorMessage
        }

        if (this.#errorMessageFn) {
            const message = this.#errorMessageFn(context, args)
            if (message != null) {
                return message
            }
        }

        return undefined
    }
}

export class FormRuleDefinition {
    constructor(
        id: string,
        validateFn?: FormRuleValidationFn,
        validateAsync?: FormRuleAsyncValidationFn
    ) {
        this.id = id
        this.validate = validateFn ?? noopValidationFn
        this.validateAsync = validateAsync ?? noopAsyncValidationFn
    }

    readonly id: string
    readonly validate: FormRuleValidationFn
    readonly validateAsync: FormRuleAsyncValidationFn
}

// TODO: Just an experiment. Move all built-in rules to a dedicated place.
export function createIbanRule(opts?: { countryCode?: string }): FormRuleDefinition {
    return new FormRuleDefinition(
        "#iban#",
        (context: IFormRuleValidationContext): FormRuleResult => {
            const value = context.rule.target.value()

            if (!value || typeof value !== "string") {
                return null
            }

            // TODO: How to use i18n in validation functions?

            if (opts?.countryCode && !value.startsWith(opts.countryCode)) {
                return `Diese IBAN ist nicht gültig. Bitte geben Sie eine IBAN ein, welche mit ${opts.countryCode} beginnt.`
            }

            // TODO: Validation
            return "Diese IBAN ist nicht gültig. Bitte geben Sie eine gültige IBAN ein."
        })
}

//#endregion Rule

//#region RuleBuilder

export interface ICustomFormRule {
    id?: string,
    sources?: IFormInputModelCore[]
    validate: FormRuleValidationFn
}

function isValueEmpty(value: any): boolean {
    return value === null ||
        value === undefined ||
        (typeof value === "string" && !value) ||
        (Array.isArray(value) && !value.length)
}

const requiredRuleDef = new FormRuleDefinition(
    "#required#",
    (context: IFormRuleValidationContext): FormRuleResult => {
        const value = context.rule.target.value()

        if (isValueEmpty(value)) {
            return context.rule.buildErrorMessage(context) ??
                "Ein Wert wird benötigt."
        }

        return null
    })

function validateMinimum(context: IFormRuleValidationContext, dataKind: string | undefined, minimum: any): FormRuleResult {
    const value = context.rule.target.value() as string | number | Date | null | undefined

    if (isValueEmpty(value)) {
        // This validation needs an actual value.
        return null
    }

    if (typeof value === "string" && value.length < minimum) {
        return context.rule.buildErrorMessage(context, { minimum: minimum }) ?? `Der Text muss mindestens ${minimum} Zeichen lang sein.`
    }
    else if (typeof value === "number" && value < minimum) {
        return context.rule.buildErrorMessage(context, { minimum: minimum }) ?? `Die Zahl muss mindestens ${minimum} betragen.`
    }
    else if (DateTime.isDateTime(value)) {
        const dateTime = value as DateTime
        const minimumDateTime = minimum as DateTime
        if (dateTime < minimumDateTime) {
            const message = context.rule.buildErrorMessage(context, { dataKind: dataKind, minimum: minimum })
            if (message) {
                return message
            }

            if (dataKind === "date-only") {
                return "Das Datum darf nicht vor " +
                    `${minimumDateTime.toLocaleString(DateTime.DATE_SHORT)} liegen.`
            }
            else if (dataKind === "time-only") {
                return "Die Uhrzeit darf nicht vor " +
                    `${minimumDateTime.toLocaleString(DateTime.TIME_SIMPLE)} ` +
                    `(${minimumDateTime.toLocaleString(DateTime.DATE_SHORT)}) liegen.`
            }
            else if (dataKind === "date-time") {
                return "Das Datum und die Uhrzeit darf nicht vor " +
                    `${minimumDateTime.toLocaleString(DateTime.DATETIME_SHORT)} liegen.`
            }
        }
    }
    else if (value instanceof Date && (value as Date) < (minimum as Date)) {
        const message = context.rule.buildErrorMessage(context, { dataKind: dataKind, minimum: minimum })
        if (message) {
            return message
        }

        if (dataKind === "date-only") {
            return `Das Datum darf nicht vor ${minimum} liegen.`
        }
        else if (dataKind === "time-only") {
            return `Die Uhrzeit darf nicht vor ${minimum} liegen.`
        }
        else if (dataKind === "date-time") {
            return `Das Datum und die Uhrzeit darf nicht vor ${minimum} liegen.`
        }
    }

    return null
}

function validateMaximum(context: IFormRuleValidationContext, dataKind: string | undefined, maximum: any): FormRuleResult {
    const value = context.rule.target.value() as string | number | Date | null | undefined

    if (isValueEmpty(value)) {
        // This validation needs an actual value.
        return null
    }

    if (typeof value === "string" && value.length > maximum) {
        return context.rule.buildErrorMessage(context, { dataKind: dataKind, maximum: maximum }) ?? `Der Text darf nicht länger als ${maximum} Zeichen lang sein.`
    }
    else if (typeof value === "number" && value > maximum) {
        return context.rule.buildErrorMessage(context, { dataKind: dataKind, maximum: maximum }) ?? `Die Zahl darf nicht mehr als ${maximum} betragen.`
    }
    else if (DateTime.isDateTime(value)) {
        const dateTime = value as DateTime
        const maximumDateTime = maximum as DateTime
        if (dateTime > maximumDateTime) {
            const message = context.rule.buildErrorMessage(context, { dataKind: dataKind, maximum: maximum })
            if (message) {
                return message
            }

            if (dataKind === "date-only") {
                return "Das Datum darf nicht nach " +
                    `${maximumDateTime.toLocaleString(DateTime.DATE_SHORT)} liegen.`
            }
            else if (dataKind === "time-only") {
                return "Die Uhrzeit darf nicht nach " +
                    `${maximumDateTime.toLocaleString(DateTime.TIME_SIMPLE)} ` +
                    `(${maximumDateTime.toLocaleString(DateTime.DATE_SHORT)}) liegen.`
            }
            else if (dataKind === "date-time") {
                return "Das Datum und die Uhrzeit darf nicht nach " +
                    `${maximumDateTime.toLocaleString(DateTime.DATETIME_SHORT)} liegen.`
            }
        }
    }
    else if (value instanceof Date && (value as Date) > (maximum as Date)) {
        const message = context.rule.buildErrorMessage(context, { dataKind: dataKind, maximum: maximum })
        if (message) {
            return message
        }

        if (dataKind === "date-only") {
            return `Das Datum darf nicht nach ${maximum} liegen.`
        }
        else if (dataKind === "time-only") {
            return `Die Uhrzeit darf nicht nach ${maximum} liegen.`
        }
        else if (dataKind === "date-time") {
            return `Das Datum und die Uhrzeit darf nicht nach ${maximum} liegen.`
        }
    }

    return null
}

export class FormRulesBuilder<TData = any> {
    readonly parent: IFormPart
    readonly target: IFormInputModelCore<TData>
    /**
     * Used e.g. for differentiation of dates (date or time or date-time).
     */
    protected _dataKind?: string

    constructor(parent: IFormPart, target: IFormInputModelCore<TData>) {
        this.parent = parent
        this.target = target

        this.init()
    }

    protected init(): void {
        // NOOP
    }

    /**
     * Adds a custom rule.
     */
    custom(customRule: ICustomFormRule): this {
        const rule = new FormRule({
            id: customRule.id ?? crypto.randomUUID(),
            target: this.target,
            sources: customRule.sources ?? undefined,
            validate: customRule.validate
        })

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }

    notRequired(): this {
        this.target._setIsRequiredCore(false)
        _getInternalValidation(this.parent).removeInstanceRuleById("#required#")

        return this
    }

    required(errorMessage?: string | undefined): this {
        this.target._setIsRequiredCore(true)

        const requiredRule = new FormRule({
            id: requiredRuleDef.id,
            target: this.target,
            errorMessage: errorMessage,
            validate: requiredRuleDef.validate
        })
        _getInternalValidation(this.parent).addInstanceRule(requiredRule)

        return this
    }

    protected isValueEmpty(value: any): boolean {
        return isValueEmpty(value)
    }

    // TODO: Rule validation functions need to be dynamic in order to react on e.g. min/max changes.

    protected addMinimumRuleCore(minimum: any, errorMessage?: string): this {
        const rule = new FormRule({
            id: "#minimum#",
            target: this.target,
            errorMessage: errorMessage,
            validate: (context: IFormRuleValidationContext): FormRuleResult =>
                validateMinimum(context, this._dataKind, minimum)
        })

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }

    protected addMaximumRuleCore(maximum: any, errorMessage?: string): this {
        const rule = new FormRule({
            id: "#maximum#",
            target: this.target,
            errorMessage: errorMessage,
            validate: (context: IFormRuleValidationContext): FormRuleResult => validateMaximum(context, this._dataKind, maximum)
        })

        _getInternalValidation(this.parent).addInstanceRule(rule)

        return this
    }
}

//#endregion RuleBuilder

//#region Validation

export type ValidationType = "full" | "intermediate"

export class ValidationError {
    readonly id: string
    readonly message: string
    readonly rule?: FormRule

    constructor(id: string, message: string, rule?: FormRule) {
        this.id = id
        this.message = message
        this.rule = rule
    }
}

/** @inheritdoc */
export class _InternalFromPartValidationManager implements _IInternalFormPartValidationManager {
    get instanceRules(): FormRule[] {
        return this.#instanceRules ??= []
    }
    #instanceRules?: FormRule[]

    get hasInstanceRules(): boolean {
        return !!this.#instanceRules?.length
    }

    addInstanceRule(rule: FormRule): void {
        this.instanceRules.push(rule)
    }

    removeInstanceRuleById(ruleId: string): void {
        if (!this.#instanceRules?.length) return

        const index = this.#instanceRules.findIndex(x => x.id === ruleId)
        if (index !== -1) {
            this.#instanceRules.splice(index, 1)
        }
    }

    get hasRules(): boolean {
        return this.hasInstanceRules
    }

    get hasRequiredRule(): boolean {
        if (!this.#instanceRules?.length) return false

        return this.#instanceRules.find(x => x.id === "#required#") !== undefined
    }

    getRulesBySource(source: IFormInputModelCore): FormRule[] | null {
        if (!this.#instanceRules?.length) return null

        let rules: FormRule[] | null = null

        for (const rule of this.#instanceRules) {
            if (rule.isSource(source)) {
                if (rules === null) {
                    rules = [rule]
                } else {
                    rules.push(rule)
                }
            }
        }

        return rules
    }

    getRulesByTarget(target: IFormInputModelCore): FormRule[] | null {
        if (!this.#instanceRules?.length) return null

        let rules: FormRule[] | null = null

        for (const rule of this.#instanceRules) {
            if (rule.isTarget(target)) {
                if (rules === null) {
                    rules = [rule]
                } else {
                    rules.push(rule)
                }
            }
        }

        return rules
    }

    readonly #errorCounter = signal(0)
    readonly _hasErrors = computed(() => this.#errorCounter() > 0)

    increaseErrorCounter(): void {
        this.#errorCounter.update(x => x++)
    }

    decreaseErrorCounter(): void {
        this.#errorCounter.update(x => x--)
    }
}

//#endregion Validation
