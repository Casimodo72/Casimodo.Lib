import { ChangeDetectorRef, DestroyRef, Directive, ElementRef, Input, OnDestroy, OnInit, Optional, inject } from "@angular/core"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { AbstractControl, NgModel } from "@angular/forms"
import { MatAutocompleteTrigger } from "@angular/material/autocomplete"
import { MatDatepickerInput } from "@angular/material/datepicker"
import { MatFormFieldControl } from "@angular/material/form-field"
import { MatSelect, MatSelectChange } from "@angular/material/select"
import { DomEventManager } from "@lib/dom"
import { IFormPropControlAdaper, FormProp, PickerFormProp, AnyDateTimeFormProp, PickerItemModel, SearchableStringFormProp } from "@lib/models/props"

@Directive({
    // eslint-disable-next-line @angular-eslint/directive-selector
    selector: "input[ccProp], textarea[ccProp], mat-select[ccProp]",
    standalone: true
})
/**
 * Acts as the glue between a Prop, the Angular forms stuff and the DOM.
 */
export class CCPropDirective implements OnInit, OnDestroy, IFormPropControlAdaper {
    readonly #destroyRef = inject(DestroyRef)
    readonly #elementRef: ElementRef
    readonly #changeDetectorRef: ChangeDetectorRef
    readonly _ngModel: NgModel
    readonly #formFieldControl: MatFormFieldControl<any> | null
    // TODO: Dunno yet if we should use HostListener (or something else) instead for listening to DOM events.
    readonly domEvents: DomEventManager

    @Input({ required: true }) ccProp!: FormProp

    // TODO: How do dynamically apply a read-only state?
    //   [disabled]="prop.isReadOnly()"
    //   [ariaReadOnly]="prop.isReadOnly()"

    constructor(
        changeDetectorRef: ChangeDetectorRef,
        elementRef: ElementRef,
        ngModel: NgModel,
        @Optional() formFieldControl: MatFormFieldControl<any> | null
    ) {
        this.#changeDetectorRef = changeDetectorRef
        this.#elementRef = elementRef
        this._ngModel = ngModel
        this.#formFieldControl = formFieldControl

        // The ndModel needs to be standalone; we will get errors otherwise, since
        // we don't use the rest of the angular forms stuff.
        this._ngModel.options = { standalone: true }
        this.domEvents = new DomEventManager(this, this.#elementRef)

        if (this.#formFieldControl) {
            const ngFormsAbstractControl: AbstractControl | null | undefined = this.#formFieldControl.ngControl?.control
            if (ngFormsAbstractControl) {
                ngFormsAbstractControl.setValidators((_control: AbstractControl) => {
                    if (this.ccProp.errors()?.length) {
                        return { dummy: true }
                    }

                    return null
                })
            }
        }
    }

    detectChanges() {
        if (this.#changeDetectorRef) {
            this.#changeDetectorRef.detectChanges()
        }
    }

    /** Couldn't make binding (ngModel="mySignal()") work with mat-select,
     * so we're sometimes setting the value programmatically on the control.
     */

    setValue(value: any): void {
        const control = this._ngModel.control
        if (control) {
            control.setValue(value)
        }
    }

    focus() {
        this.#elementRef.nativeElement?.focus()
    }

    setErrorState(errorState: boolean): void {
        if (!this.#formFieldControl) return

        const ngFormsAbstractControl: AbstractControl | null | undefined = this.#formFieldControl.ngControl?.control
        if (!ngFormsAbstractControl) return

        if (errorState) {
            // TODO: without detectChanges() Material's the error state somehow
            // does not update. I'm trying to use updateValueAndValidity()
            // and see if it also works.

            // "mat-error" will only be shown (correctly) by Angular material when the
            //  the control is in a touched and error state.
            //  This is a workaround to enforce the error state.
            ngFormsAbstractControl.markAsTouched()
            ngFormsAbstractControl.updateValueAndValidity()
            ngFormsAbstractControl.setErrors({ dummy: true })

            // TODO: When calling detectChanges() here, the date-picker
            // will reset to a value of null :-/ Dunno why, it's Material :-/
            // TODO: REMOVE? this.#changeDetectorRef.detectChanges()
        } else {
            ngFormsAbstractControl.setErrors(null)
        }
    }

    ngOnInit() {
        this.ccProp._setControlAdapter(this)

        const el = this.#elementRef.nativeElement as HTMLInputElement
        el.setAttribute("name", this.ccProp.id)
        this.domEvents.addInput(this.#onInput)
        this.domEvents.add("keydown", this.#onKeyDown)
        this.domEvents.add("keyup", this.#onKeyUp)
        this.domEvents.add("focusin", this.#onFocusIn)
        this.domEvents.add("focusout", this.#onFocusOut)

        if (this.ccProp.errors()?.length) {
            this.setErrorState(true)
        }

        // Date picker
        if (this.ccProp instanceof AnyDateTimeFormProp) {
            if (this._ngModel.valueAccessor instanceof MatDatepickerInput &&
                this._ngModel.valueChanges
            ) {
                this._ngModel.valueChanges
                    .pipe(takeUntilDestroyed(this.#destroyRef))
                    .subscribe(data => {
                        if (data && !(data as any).isLuxonDateTime) {
                            throw new Error("A Luxon date-time was expected.")
                        }

                        this.ccProp.setValue(data ?? null)
                        // console.log(data)
                    })
            }
        }

        if (this.ccProp instanceof SearchableStringFormProp) {
            const textModel = this.ccProp as SearchableStringFormProp
            if (this._ngModel.valueAccessor instanceof MatAutocompleteTrigger &&
                this._ngModel.valueChanges
            ) {
                this._ngModel.valueChanges
                    .pipe(takeUntilDestroyed(this.#destroyRef))
                    .subscribe(data => {
                        if (data == null || typeof data === "string") {
                            textModel.setValue(data ?? null)
                        }
                    })
            }
        }

        // Value picker
        if (this.ccProp instanceof PickerFormProp) {
            const pickerModel = this.ccProp as PickerFormProp

            if (this.#formFieldControl instanceof MatSelect) {
                this.#formFieldControl.compareWith = this.#compareValues
                this.#formFieldControl.selectionChange
                    .pipe(takeUntilDestroyed(this.#destroyRef))
                    .subscribe((change: MatSelectChange) => {
                        if (change.value instanceof PickerItemModel) {
                            pickerModel.selectItem(change.value)
                        }
                        else {
                            pickerModel.setValue(change.value)
                        }
                    })

                // const matSelect = this._ngModel.valueAccessor as MatSelect
                // const matSelectValue = matSelect.value
                // if ((matSelectValue === null || matSelectValue === undefined) &&
                //     this.ccProp.selectedItem() !== null
                // ) {
                //     matSelect.writeValue(this.ccProp.selectedItem())

                //     this.changeDetectorRef.detectChanges()
                // }
            }
            else if (this._ngModel.valueAccessor instanceof MatAutocompleteTrigger &&
                this._ngModel.valueChanges
            ) {
                this._ngModel.valueChanges
                    .pipe(takeUntilDestroyed(this.#destroyRef))
                    .subscribe(data => {
                        if (data == null || typeof data === "string") {
                            pickerModel.setFilterValue(data ?? null)
                            pickerModel.setValue(data ?? null)
                        }
                    })
            }
        }
    }

    /** User by MatSelect. */
    #compareValues(o1: any, o2: any): boolean {
        // NOTE: "==" is used here deliberately.

        // Case: both values are null or undefined.
        if (o1 == null && o2 == null) {
            return true
        }

        // Case: picker items are used; those have an ID.
        if (o1?.id != null && o1.id === o2?.id) {
            return true
        }

        const o1t = typeof o1

        if (o1t === "object") {
            // TODO: We currently don't support non generically comparable types
            // (e.g. Date, DateTime, Duration, any non-built in class, etc.)
            return false
        }

        if (o1t === typeof o2) {
            return o1 === o2
        }

        return false
    }

    #onFocusIn(ev: FocusEvent): any {
        this.ccProp._onDomInputFocusIn(ev)
    }

    #onKeyDown(ev: KeyboardEvent): any {
        this.ccProp._onDomInputKeyDown(ev)
    }

    #onKeyUp(ev: KeyboardEvent): any {
        this.ccProp._onDomInputKeyUp(ev)
    }

    #onInput(ev: InputEvent): any {
        this.ccProp._onDomInput(ev)
    }

    #onFocusOut(ev: FocusEvent): any {
        this.ccProp._onDomInputFocusOut(ev)
    }

    ngOnDestroy() {
        this.domEvents.removeAll()
        this.ccProp._setControlAdapter(undefined)
    }
}
