/* eslint-disable @angular-eslint/directive-class-suffix */
/* eslint-disable @angular-eslint/directive-selector */
import { Directive, Input } from "@angular/core"

import { CdkCellDef } from "@angular/cdk/table"
import { MatTable, MatRowDef, MatCellDef } from "@angular/material/table"

// Source: https://nartc.me/blog/typed-mat-cell-def
@Directive({
    selector: "[matCellDef]",
    providers: [{ provide: CdkCellDef, useExisting: TypeSafeMatCellDef }],
    standalone: true,
})
export class TypeSafeMatCellDef<T> extends MatCellDef {

    // leveraging syntactic-sugar syntax when we use *matCellDef
    @Input() matCellDefTable?: MatTable<T>

    // ngTemplateContextGuard flag to help with the Language Service
    static ngTemplateContextGuard<T>(
        dir: TypeSafeMatCellDef<T>,
        ctx: unknown,
    ): ctx is { $implicit: T; index: number } {
        return true
    }
}

// Source: https://nartc.me/blog/typed-mat-cell-def
@Directive({
    selector: "[matRowDef]",
    providers: [{ provide: CdkCellDef, useExisting: TypeSafeMatCellDef }],
    standalone: true,
})
export class TypeSafeMatRowDef<T> extends MatRowDef<T> {

    // leveraging syntactic-sugar syntax when we use *matCellDef
    @Input() matRowDefTable?: MatTable<T>

    // ngTemplateContextGuard flag to help with the Language Service
    static ngTemplateContextGuard<T>(
        dir: TypeSafeMatRowDef<T>,
        ctx: unknown,
    ): ctx is { $implicit: T; index: number } {
        return true
    }
}
