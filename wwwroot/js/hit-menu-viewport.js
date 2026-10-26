const views = new WeakMap();
export function observe(viewport) {
    disconnect(viewport);
    const surface = viewport?.querySelector(":scope > .hit-menu-surface");
    if (!surface) return;
    let frame = 0;
    function measure() {
        frame = 0;
        // Natural dimensions stay independent of both the scaled canvas and native window.
        if (!viewport.isConnected || surface.offsetWidth === 0 || surface.offsetHeight === 0) return;
        viewport.style.setProperty("--hit-menu-natural-width", surface.offsetWidth + "px");
        viewport.style.setProperty("--hit-menu-natural-height", surface.offsetHeight + "px");
    }
    const observer = new ResizeObserver(() => {
        if (!frame) frame = requestAnimationFrame(measure);
    });
    observer.observe(surface);
    measure();
    views.set(viewport, () => { observer.disconnect(); cancelAnimationFrame(frame); });
}
export function disconnect(viewport) { views.get(viewport)?.(); views.delete(viewport); }
