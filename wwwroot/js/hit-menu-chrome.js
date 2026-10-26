// Optional menu chrome policy. Media updates never count as user activity.
export function connectMenuChrome(sphere) {
    let timer = 0, disposed = false, root = null, view = null, eligible = false, hidden = false;
    const targets = new Map();
    let pointer = null;
    let layoutTimer = 0;
    function endLayout() {
        clearTimeout(layoutTimer);
        delete sphere.dataset.hitLayoutAnimating;
    }
    function beginLayout() {
        clearTimeout(layoutTimer);
        if (document.documentElement.dataset.hitMotion === "off" || matchMedia("(prefers-reduced-motion: reduce)").matches) { endLayout(); return; }
        const value = getComputedStyle(sphere).getPropertyValue("--hit-focus-duration").trim();
        const parsed = parseFloat(value) * (value.endsWith("ms") ? 1 : 1000);
        const duration = Number.isFinite(parsed) ? Math.max(0, Math.min(10000, parsed)) : 1200;
        if (!duration) { endLayout(); return; }
        sphere.dataset.hitLayoutAnimating = "true";
        layoutTimer = setTimeout(endLayout, duration + 100);
    }
    function restore() {
        for (const [node, previous] of targets) {
            node.inert = previous.inert;
            if (previous.aria === null) node.removeAttribute("aria-hidden");
            else node.setAttribute("aria-hidden", previous.aria);
        }
        targets.clear();
    }
    function applyTargets() {
        const nodes = [sphere.querySelector(".sphere-header"), ...root.querySelectorAll("[data-hit-idle-controls]")];
        for (const node of nodes) {
            if (!node || targets.has(node)) continue;
            targets.set(node, {inert:node.inert, aria:node.getAttribute("aria-hidden")});
            node.inert = true;
            node.setAttribute("aria-hidden", "true");
        }
    }
    function show(animate = false) {
        if (hidden && animate) beginLayout();
        else if (!animate) endLayout();
        hidden = false;
        delete sphere.dataset.hitControlsHidden;
        delete sphere.dataset.hitIdleView;
        restore();
    }
    function arm() {
        clearTimeout(timer);
        if (!eligible || disposed) return;
        const requestedDelay = Number(root?.dataset.hitIdleDelay);
        const delay = Number.isFinite(requestedDelay) && requestedDelay >= 500 && requestedDelay <= 60000 ? requestedDelay : 5000;
        timer = setTimeout(() => {
            if (!eligible || disposed) return;
            if (root.dataset.hitIdleAction === "compact") {
                root.dispatchEvent(new CustomEvent("hit-menu-idle"));
                return;
            }
            hidden = true;
            beginLayout();
            sphere.dataset.hitControlsHidden = "true";
            sphere.dataset.hitIdleView = view;
            applyTargets();
        }, delay);
    }
    function activity(event) {
        if (event?.type === "pointermove" || event?.type === "pointerenter") {
            const next = {x:event.screenX, y:event.screenY};
            const unchanged = pointer && pointer.x === next.x && pointer.y === next.y;
            pointer = next;
            // Resizing under a stationary mouse can synthesize hover events.
            // Only actual pointer movement reveals/expands the menu again.
            if (unchanged) return;
        }
        if (!eligible || disposed) return;
        if (hidden) show(true);
        arm();
    }
    function refresh() {
        const nextRoot = sphere.querySelector('.hit-menu-panel:not([hidden]) [data-hit-hide-controls="true"], .hit-menu-panel:not([hidden]) [data-hit-idle-action="compact"]');
        const nextView = nextRoot?.dataset.hitView;
        const nextEligible = !!nextRoot && sphere.classList.contains("is-open") &&
            sphere.classList.contains("is-pinned") && !sphere.classList.contains("is-dragging");
        const changed = nextRoot !== root || nextView !== view || nextEligible !== eligible;
        root = nextRoot; view = nextView; eligible = nextEligible;
        if (changed) { show(); arm(); }
        else if (hidden && root) applyTargets();
    }
    const events = ["pointerenter", "pointermove", "pointerdown", "pointerup", "wheel", "focusin", "input"];
    for (const name of events) sphere.addEventListener(name, activity, {capture:true, passive:true});
    window.addEventListener("keydown", activity, true);
    const observer = new MutationObserver(refresh);
    observer.observe(sphere, {subtree:true, childList:true, attributes:true,
        attributeFilter:["class", "hidden", "data-hit-hide-controls", "data-hit-view", "data-hit-idle-action", "data-hit-idle-delay"]});
    refresh();
    return () => {
        disposed = true;
        clearTimeout(timer);
        observer.disconnect();
        for (const name of events) sphere.removeEventListener(name, activity, true);
        window.removeEventListener("keydown", activity, true);
        show();
    };
}
