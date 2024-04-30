import { trigger, transition, style, query, animate } from "@angular/animations"

export const pageFadeTransitionAnimation =
    trigger("pageFadeTransition", [
        transition("* <=> *", [
            // Set a default  style for enter and leave
            query(":enter, :leave", [
                style({
                    position: "absolute",
                    left: 0,
                    width: "100%",
                    opacity: 0,
                    transform: "scale(0) translateY(100%)",
                }),
            ]),
            // Animate the new page in
            query(":enter", [
                animate("600ms ease", style({ opacity: 1, transform: "scale(1) translateY(0)" })),
            ])
        ]),
    ])

/*
export const defaultRouterAnimation =
    trigger("routeAnimations", [
        transition("HomePage <=> AboutPage", [
            style({ position: "relative" }),
            query(":enter, :leave", [
                style({
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%"
                })
            ]),
            query(":enter", [
                style({ left: "-100%" })
            ], { optional: true }),
            query(":leave", animateChild(), { optional: true }),
            group([
                query(":leave", [
                    animate("300ms ease-out", style({ left: "100%" }))
                ], { optional: true }),
                query(":enter", [
                    animate("300ms ease-out", style({ left: "0%" }))
                ], { optional: true }),
            ]),
        ]),
        transition("* <=> *", [
            style({ position: "relative" }),
            query(":enter, :leave", [
                style({
                    position: "absolute",
                    top: 0,
                    left: 0,
                    width: "100%"
                })
            ], { optional: true }),
            query(":enter", [
                style({ left: "-100%" })
            ], { optional: true }),
            query(":leave", animateChild(), { optional: true }),
            group([
                query(":leave", [
                    animate("200ms ease-out", style({ left: "100%", opacity: 0 }))
                ], { optional: true }),
                query(":enter", [
                    animate("300ms ease-out", style({ left: "0%" }))
                ], { optional: true }),
                query("@*", animateChild(), { optional: true })
            ]),
        ])
    ])
*/
