import { animate, query, stagger, style, transition, trigger } from "@angular/animations"

export const fadeAnimation =
    trigger("fade", [
        transition(":enter", [
            style({ opacity: 0 }),
            animate(300, style({ opacity: 1 }))
        ]),
        transition(":leave", [
            style({ opacity: 1 }),
            animate(100, style({ opacity: 0 }))
        ])
    ])

export const fadeHeightAnimation =
    trigger("fadeHeight", [
        transition(":enter", [
            style({ opacity: 0, height: 0 }),
            animate(300, style({ opacity: 1, height: "*" }))
        ]),
        transition(":leave", [
            style({ opacity: 1, height: "*" }),
            animate(100, style({ opacity: 0, height: 0 }))
        ])
    ])

export const fadeWidthAnimation =
    trigger("fadeWidth", [
        transition(":enter", [
            style({ opacity: 0, width: 0 }),
            animate(400, style({ opacity: 1, width: "*" }))
        ]),
        transition(":leave", [
            style({ opacity: 1, width: "*" }),
            animate(100, style({ opacity: 0, width: 0 }))
        ])
    ])

// List animation source: https://ultimatecourses.com/blog/angular-animations-how-to-animate-lists
export const listAnimation = trigger("listAnimation", [
    transition("* <=> *", [
        query(":enter",
            [style({ opacity: 0 }), stagger("60ms", animate("600ms ease-out", style({ opacity: 1 })))],
            { optional: true }
        ),
        query(":leave",
            animate("200ms", style({ opacity: 0 })),
            { optional: true }
        )
    ])
])
