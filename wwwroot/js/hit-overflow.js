import { consumesWheel } from './hit-wheel-routing.js';
let mutation, resize, frame = 0;
const regions = new Map();
const selector = '[data-hit-scroll],.extensions-list,.clock-world-list,.hit-scroll-region,.settings-sidebar,.chess-settings';
function update(element) {
    if (!element.isConnected) return;
    const top = element.scrollTop > 1 ? '18px' : '0px';
    const bottom = element.scrollHeight - element.clientHeight - element.scrollTop > 1 ? '18px' : '0px';
    if (element.style.getPropertyValue('--hit-overflow-top') !== top) element.style.setProperty('--hit-overflow-top', top);
    if (element.style.getPropertyValue('--hit-overflow-bottom') !== bottom) element.style.setProperty('--hit-overflow-bottom', bottom);
}
function scan() {
    frame = 0;
    for (const [element, stop] of regions) if (!element.isConnected) { stop(); regions.delete(element); }
    for (const element of document.querySelectorAll(selector)) {
        if (!regions.has(element)) {
            element.classList.add('hit-scroll-hint');
            const onScroll = () => update(element), onWheel = event => { if (consumesWheel(element,event)) event.stopPropagation(); };
            element.addEventListener('scroll', onScroll, { passive: true });
            element.addEventListener('wheel', onWheel, { passive: true });
            resize.observe(element);
            regions.set(element, () => { element.removeEventListener('scroll', onScroll); element.removeEventListener('wheel', onWheel); resize.unobserve(element); });
        }
        update(element);
    }
}
function schedule() { if (!frame) frame = requestAnimationFrame(scan); }
export function connect() {
    disconnect();
    resize = new ResizeObserver(schedule);
    mutation = new MutationObserver(schedule);
    mutation.observe(document.querySelector('.sphere-area') || document.body, { subtree: true, childList: true, characterData: true, attributes: true, attributeFilter: ['hidden'] });
    scan();
}
export function disconnect() {
    mutation?.disconnect();
    for (const stop of regions.values()) stop();
    regions.clear(); resize?.disconnect(); cancelAnimationFrame(frame); frame = 0;
}
