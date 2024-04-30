import { DestroyRef, Directive, EventEmitter, HostListener, OnInit, Output, inject, input } from "@angular/core"
import { takeUntilDestroyed } from "@angular/core/rxjs-interop"
import { Subject } from "rxjs"

// Source: https://javascript.plainenglish.io/stop-the-horrible-clash-between-single-and-double-clicks-in-angular-5798ce90fd1a
@Directive({
    // eslint-disable-next-line @angular-eslint/directive-selector
    selector: "[click.single],[click.double]",
    standalone: true
})
export class ClickHandlerDirective implements OnInit {
    readonly #destroyRef = inject(DestroyRef)
    readonly #clickEventSubject = new Subject<MouseEvent>()

    constructor() { }

    readonly debounceTime = input(300)
    @Output("click.double") doubleClick = new EventEmitter()
    @Output("click.single") singleClick = new EventEmitter()

    ngOnInit() {
        this.#clickEventSubject
            .pipe(takeUntilDestroyed(this.#destroyRef))
            .subscribe(event => {
                if (event.type === "click") {
                    this.singleClick.emit(event)
                } else {
                    this.doubleClick.emit(event)
                }
            })
    }

    @HostListener("click", ["$event"])
    clickEvent(event: MouseEvent) {
        event.preventDefault()
        event.stopPropagation()
        this.#clickEventSubject.next(event)
    }

    @HostListener("dblclick", ["$event"])
    doubleClickEvent(event: MouseEvent) {
        event.preventDefault()
        event.stopPropagation()
        this.#clickEventSubject.next(event)
    }
}
