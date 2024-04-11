import { Injectable, signal } from "@angular/core"

export interface NavNode {
    routeId?: string
    title?: string
    icon?: string
    iconOnly?: boolean
    isExpanded?: boolean
    depth?: number
    children?: NavNode[]
    path?: string
    params?: any
    clicked?: () => void
}

@Injectable({
    providedIn: "root"
})
export class SidenavService {
    readonly isOpen = signal(false)
    readonly isTextVisible = signal(false)

    toggleOpen() {
        this.setOpen(!this.isOpen())
    }

    setOpen(open: boolean) {
        this.isOpen.set(open)
    }

    toggleIsTextVisible() {
        this.setTextVisible(!this.isTextVisible())
    }

    setTextVisible(isTextVisible: boolean) {
        this.isTextVisible.set(isTextVisible)
    }

    hideText() {
        this.setTextVisible(false)
    }

    showText() {
        this.setTextVisible(true)
    }
}
