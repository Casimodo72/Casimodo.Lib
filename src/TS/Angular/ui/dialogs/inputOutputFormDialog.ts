import { Directive } from "@angular/core"

import { InputOutputForm } from "@lib/ui/forms"

/** Base class for input-output forms that will be hosted by the DialogFormDialog. */
@Directive()
export abstract class InputOutputFormDialog<TInput, TOutput>
    extends InputOutputForm<TInput, TOutput> {
}
