const regions = new WeakMap();
export function enableLocalScroll(element) {
    if (!element || regions.has(element)) return;
    const stop = event => event.stopPropagation();
    element.addEventListener("wheel", stop, {passive:true});
    regions.set(element, stop);
}
export function disconnectLocalScroll(element) {
    const stop = regions.get(element);
    if (stop) element.removeEventListener("wheel", stop);
    regions.delete(element);
}
