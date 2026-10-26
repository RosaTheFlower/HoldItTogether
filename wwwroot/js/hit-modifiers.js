// Discoverability belongs to the HIT shell, so every menu uses the same hints.
export function connectModifiers(sphere) {
    function flag(name, enabled) {
        if (enabled) { if (sphere.dataset[name] !== "true") sphere.dataset[name] = "true"; }
        else delete sphere.dataset[name];
    }
    function update(event) { flag("ctrlPressed", event.ctrlKey); flag("altPressed", event.altKey); }
    function clear() { flag("ctrlPressed", false); flag("altPressed", false); }
    function visibility() { if (document.hidden) clear(); }
    const events = ["keydown", "keyup", "pointermove", "pointerdown"];
    for (const name of events) window.addEventListener(name, update, true);
    window.addEventListener("blur", clear);
    document.addEventListener("visibilitychange", visibility);
    return () => {
        for (const name of events) window.removeEventListener(name, update, true);
        window.removeEventListener("blur", clear);
        document.removeEventListener("visibilitychange", visibility);
        clear();
    };
}
