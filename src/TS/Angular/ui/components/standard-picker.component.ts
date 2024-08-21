import { CommonModule } from "@angular/common"
import {
    AfterViewInit, ChangeDetectionStrategy, Component, DestroyRef, Type,
    computed, inject, input, output
} from "@angular/core"
import { FormsModule } from "@angular/forms"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"

import { MatButton } from "@angular/material/button"
import { MatFormFieldModule } from "@angular/material/form-field"
import { MatSelectModule } from "@angular/material/select"
import { MatDivider } from "@angular/material/divider"

import { CMatModel, CMatModelErrors } from "@lib/ui/forms"
import { PickerModel, UICommandEvent } from "@lib/models"

import { StandardInputComponent } from "./standardInputComponent"
import { PickItemComponent } from "./pickItemComponent"
import { TileComponent } from "./tile.component"

// TODO: Support on-demand loading of pick-items (e.g. for table column filters).
@Component({
    selector: "app-standard-picker",
    standalone: true,
    imports: [
        CommonModule,
        FormsModule, MatFormFieldModule, MatSelectModule, MatButton, MatDivider,
        CMatModel, CMatModelErrors, TileComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
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
        <!-- TODO: How to make mat-select read-only? There is no [readonly]. -->
        <mat-select [cmatModel]="model"
            ngModel
            [required]="effectiveIsRequired()"
            [disabled]="effectiveIsDisabled()"
            [ariaReadOnly]="model.isReadOnly()"
            (openedChange)="handleOpenedChanged($event)">

            <mat-select-trigger>
                @if (model.selectedItem(); as selectedItem) {
                    @if (effectiveSelectedComponent(); as selectedComponent) {
                        <ng-template *ngComponentOutlet="selectedComponent; inputs: { model: selectedItem }" />
                    }
                    @else {
                        {{selectedItem.toDisplayText()}}
                    }
                }
            </mat-select-trigger>

            @if (isPickValuesLoadingPending) {
                <mat-option class="app-command-select-option" (click)="$event.preventDefault(); $event.stopPropagation();">
                    Lade...
                </mat-option>
            }

            @if (model.hasNullValue() && model.value()) {
                <mat-option class="app-command-select-option" [value]="null">
                    <app-tile class="w-full mat-small h-12">
                        (Auswahl entfernen)
                    </app-tile>
                </mat-option>
            }

            @if (model.hasCommands) {
                @for (command of model.commands(); track command.id) {
                    <mat-option class="app-command-select-option" (click)="$event.preventDefault(); $event.stopPropagation();">
                        <app-tile class="w-full mat-small h-12" (click)="command.trigger(); $event.preventDefault(); $event.stopPropagation();">
                            {{command.text}}
                        </app-tile>
                    </mat-option>
                }
            }

            @for (pickItem of model.pickableItems(); track pickItem.id; let isLast = $last) {
                <mat-option class="w-full" [value]="pickItem">
                    @if (itemComponent(); as itemComponent) {
                        <ng-template *ngComponentOutlet="itemComponent; inputs: { model: pickItem }"></ng-template>
                    }
                    @else {
                        {{pickItem.toDisplayText()}}
                    }
                </mat-option>
                @if (!isLast) {
                    <mat-divider />
                }
            }
        </mat-select>
        <mat-error [cmatModel]="model" />
    </mat-form-field>
}
`
})
export class StandardPickerComponent
    extends StandardInputComponent<PickerModel>
    implements AfterViewInit {
    readonly #destroyRef = inject(DestroyRef)

    readonly itemComponent = input<Type<PickItemComponent>>()
    readonly selectedComponent = input<Type<PickItemComponent>>()

    readonly commandTriggered = output<UICommandEvent>()

    readonly effectiveSelectedComponent = computed(() => this.selectedComponent() ?? this.itemComponent())

    get isPickValuesLoadingPending() {
        return this.model()._getIsPickValuesLoadingPending()
    }

    ngAfterViewInit(): void {
        // TODO: How only do emit stuff *only* if the consumer actually subscribed to the event?
        if (this.model().hasCommands) {
            this.model().commandTriggered
                .pipe(takeUntilDestroyed(this.#destroyRef))
                .subscribe(commandEvent => this.commandTriggered.emit(commandEvent))
        }
    }

    async handleOpenedChanged(isOpen: boolean) {
        if (isOpen && this.isPickValuesLoadingPending) {
            await this.model()._loadPickValues()
        }
    }
}
