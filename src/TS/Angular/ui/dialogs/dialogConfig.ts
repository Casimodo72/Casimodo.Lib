export interface DialogConfig {
    readonly title?: string | undefined
    /**
     * Default: "backdrop"
     */
    closeStrategy?: "title-button" | "backdrop" | undefined
}
