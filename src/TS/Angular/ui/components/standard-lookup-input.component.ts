import { CommonModule } from "@angular/common"
import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, HostBinding, HostListener, Input, OnDestroy, OnInit, inject, input, viewChild } from "@angular/core"
import { AbstractControlDirective, FormsModule, NgControl } from "@angular/forms"
import { Subject } from "rxjs"

import { BooleanInput, coerceBooleanProperty } from "@angular/cdk/coercion"
import { MatError, MatFormField, MatFormFieldControl, MatLabel, MatSuffix } from "@angular/material/form-field"

import { CMatModelErrors } from "@lib/ui/forms"
import { EntityLookupModel } from "@lib/models"
import { ButtonComponent } from "./button.component"

import { StandardInputComponent } from "./standardInputComponent"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { fadeHeightAnimation } from "./animations"

// For docs on creation new form components see:
//   https://material.angular.io/guide/creating-a-custom-form-field-control
// Some other stuff:
// https://stackoverflow.com/questions/52977851/angular-material-custom-matformfieldcontrol-how-to-manage-error-state
@Component({
    selector: "app-lookup-button-form-field-control",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [{ provide: MatFormFieldControl, useExisting: LookupButtonFormFieldControlComponent }],
    imports: [ButtonComponent],
    styles: [`
    :host {
        display: block;
        width: var(--mdc-icon-button-state-layer-size);
        height: var(--mdc-icon-button-state-layer-size);
    }
`],
    template: `
@if (!readonly()) {
<app-button type="edit" />
}
`
})
export class LookupButtonFormFieldControlComponent
    implements MatFormFieldControl<any>, OnDestroy {

    static _nextId = 0
    @HostBinding() id = `example-tel-input-${LookupButtonFormFieldControlComponent._nextId++}`

    readonly model = input.required<EntityLookupModel>()

    readonly readonly = input<boolean>(false)

    readonly stateChanges = new Subject<void>()

    constructor(private _elementRef: ElementRef) {
    }

    get ngControl(): NgControl | AbstractControlDirective | null {
        return null
    }

    get empty() {
        return !this.value
    }

    get shouldLabelFloat() {
        return !!this.#focused || !!this.value
    }

    get focused() {
        return !!this.#focused
    }
    #focused?: boolean

    @HostListener("focusin", ["$event"])
    _onFocusIn(_event: FocusEvent) {
        if (!this.focused) {
            this.#focused = true
            this.stateChanges.next()
        }
    }

    @HostListener("focusout", ["$event"])
    _onFocusOut(event: FocusEvent) {
        if (!this._elementRef.nativeElement.contains(event.relatedTarget as Element)) {
            this.#focused = false
            //this.touched = true;
            //this.onTouched();
            this.stateChanges.next()
        }
    }

    @Input()
    get value(): any | null | undefined {
        return this.model().value()
    }
    set value(value: any | null | undefined) {
        if (this.model().setValue(value)) {
            this.stateChanges.next()
        }
    }

    @Input()
    get placeholder() {
        return this.#placeholder ?? ""
    }
    set placeholder(plh) {
        this.#placeholder = plh
        this.stateChanges.next()
    }
    #placeholder?: string

    // setShouldLabelFloat(shouldLabelFloat: boolean) {
    //     this.#shouldLabelFloat = shouldLabelFloat
    //     this.stateChanges.next()
    // }
    // #shouldLabelFloat?: boolean

    @Input()
    get required(): boolean {
        return this.model().isRequired()
    }
    set required(_req: BooleanInput) {
        // TODO: Do we want to support setting this rule via the component?
        // const isRequired = coerceBooleanProperty(req)
        // this.stateChanges.next()
    }

    @Input()
    get disabled(): boolean {
        return this.#isDisabled || this.model().isDisabled()
    }
    set disabled(value: BooleanInput) {
        this.#isDisabled = coerceBooleanProperty(value)
        //this.#isDisabled ? this.parts.disable() : this.parts.enable()
        this.stateChanges.next()
    }
    #isDisabled = false

    get errorState(): boolean {
        return false
        // TODO: this.parts.invalid && this.touched;
    }

    controlType?: string | undefined

    autofilled?: boolean | undefined

    userAriaDescribedBy?: string | undefined

    setDescribedByIds(_ids: string[]): void {
        // TODO: NOOP?
    }

    onContainerClick(_event: MouseEvent): void {
        // TODO: NOOP?
    }

    ngOnDestroy() {
        this.stateChanges.complete()
    }
}

@Component({
    selector: "app-standard-lookup-input",
    standalone: true,
    changeDetection: ChangeDetectionStrategy.OnPush,
    animations: [fadeHeightAnimation],
    imports: [
        CommonModule,
        FormsModule, MatLabel, MatFormField, MatSuffix, MatError,
        LookupButtonFormFieldControlComponent,
        CMatModelErrors
    ],
    styles: [":host { display: block; }"],
    template: `
@if (model(); as model) {
    <mat-form-field
        [ngClass]="effectiveGroupClass()"
        [ngStyle]="effectiveGroupStyle()"
        [appearance]="effectiveAppearance()"
        [floatLabel]="effectiveLabelMode()">
        @if (effectiveLabel()) {
            <mat-label>{{effectiveLabel()}}</mat-label>
        }
        <app-lookup-button-form-field-control matSuffix #lookupButton
            class="ms-auto"
            [required]="effectiveIsRequired()"
            [readonly]="effectiveIsReadOnly()"
            [ariaReadOnly]="effectiveIsReadOnly()"
            [disabled]="effectiveIsDisabled()"
            [model]="model"
            (click)="lookup()" />
        <mat-error [cmatModel]="model" />
        @if (model.details()) {
            <div @fadeHeight>
                <ng-content />
            </div>
        }
    </mat-form-field>
}
`
})
export class StandardLookupInputComponent
    extends StandardInputComponent<EntityLookupModel>
    implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly lookupButton = viewChild<LookupButtonFormFieldControlComponent>("lookupButton")

    lookup() {

        this.model().lookup()
    }

    ngOnInit(): void {
        this.model().valueChanged
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(value => {
                if (value) {
                    this.lookupButton()?.stateChanges.next()
                }
            })
    }
}
