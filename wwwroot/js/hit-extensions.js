import { compileExtension } from './hit-themes.js';

const sheets = new Map();
export function styles(id, files) {
    const css = files.map(file => compileExtension(file.css, id, file.url)).join('\n');
    let style = sheets.get(id);
    if (!style) {
        style = document.createElement('style');
        style.dataset.hitExtension = id;
        document.head.insertBefore(style, document.getElementById('hit-base-theme'));
        sheets.set(id, style);
    }
    if (style.textContent !== css) style.textContent = css;
}
export function removeStyles(id) { sheets.get(id)?.remove(); sheets.delete(id); }

let dropController;
export function connectDrop(reference) {
    disconnectDrop();
    const controller = dropController = new AbortController();
    const options = { signal: controller.signal, capture: true };
    let overlay, depth = 0, busy = false;
    const clear = () => { overlay?.remove(); overlay = null; depth = 0; };
    controller.signal.addEventListener('abort', clear);
    const files = event => [...(event.dataTransfer?.types ?? [])].includes('Files');
    document.addEventListener('dragenter', event => {
        if (!files(event)) return;
        event.preventDefault(); depth++;
        if (!overlay) {
            overlay = document.createElement('div'); overlay.className = 'hit-drop-overlay';
            overlay.textContent = 'Import .css or .hitmenu';
            document.querySelector('.sphere-area')?.append(overlay);
        }
    }, options);
    document.addEventListener('dragover', event => {
        if (files(event)) { event.preventDefault(); event.dataTransfer.dropEffect = busy ? 'none' : 'copy'; }
    }, options);
    document.addEventListener('dragleave', event => { if (files(event) && --depth <= 0) clear(); }, options);
    document.addEventListener('drop', async event => {
        if (!files(event)) return;
        event.preventDefault(); event.stopPropagation(); clear();
        if (busy) return;
        const packages = [...event.dataTransfer.files];
        if (packages.length !== 1 || !/\.(hitmenu|css)$/i.test(packages[0].name)) {
            await reference.invokeMethodAsync('DropError', 'Drop one .css theme or .hitmenu package at a time.'); return;
        }
        const css = /\.css$/i.test(packages[0].name), limit = (css ? 1 : 64) * 1024 * 1024;
        if (packages[0].size > limit) {
            await reference.invokeMethodAsync('DropError', css ? 'Themes must not exceed 1 MB.' : 'Packages must not exceed 64 MB.'); return;
        }
        busy = true;
        try { await reference.invokeMethodAsync('ImportDropped', DotNet.createJSStreamReference(packages[0]), packages[0].name); }
        catch (error) { console.error('HIT extension import failed', error); }
        finally { busy = false; }
    }, options);
    window.addEventListener('blur', clear, { signal: controller.signal });
}
export function disconnectDrop() { dropController?.abort(); dropController = null; }

const ordering = new WeakMap();
export function connectOrder(root, reference) {
    disconnectOrder(root);
    if (!root) return;
    const controller = new AbortController(), options = {signal: controller.signal};
    let drag;
    const clear = () => {
        root.querySelectorAll('.is-dragging,.is-drop-before,.is-drop-after').forEach(node => node.classList.remove('is-dragging','is-drop-before','is-drop-after'));
        if (drag?.grip.hasPointerCapture(drag.pointer)) drag.grip.releasePointerCapture(drag.pointer);
        drag = null;
    };
    root.addEventListener('pointerdown', event => {
        const grip = event.target.closest('.extension-grip');
        if (event.button !== 0 || !grip || grip.disabled) return;
        event.preventDefault(); event.stopPropagation();
        clear();
        drag = {grip, pointer:event.pointerId, x:event.clientX,y:event.clientY,row:grip.closest('[data-extension-id]'),active:false};
        grip.setPointerCapture(event.pointerId);
    }, options);
    root.addEventListener('pointermove', event => {
        if (!drag || drag.pointer !== event.pointerId) return;
        if (!drag.active && Math.hypot(event.clientX-drag.x,event.clientY-drag.y)<6) return;
        drag.active = true; drag.row.classList.add('is-dragging');
        root.querySelectorAll('.is-drop-before,.is-drop-after').forEach(node=>node.classList.remove('is-drop-before','is-drop-after'));
        const list = root.querySelector('.extensions-list'), rect = list.getBoundingClientRect();
        if (event.clientY < rect.top+30) list.scrollTop -= 12;
        if (event.clientY > rect.bottom-30) list.scrollTop += 12;
        const target = document.elementFromPoint(event.clientX,event.clientY)?.closest('[data-extension-id]');
        drag.target = null;
        if (!target || !root.contains(target) || target===drag.row || target.dataset.orderLocked==='true') return;
        drag.target = target;
        const bounds = target.getBoundingClientRect();
        drag.after = event.clientY > bounds.top+bounds.height/2;
        target.classList.add(drag.after?'is-drop-after':'is-drop-before');
    }, options);
    root.addEventListener('pointerup', event => {
        if (!drag || drag.pointer !== event.pointerId) return;
        const completed=drag; clear();
        if(completed.active && completed.target) reference.invokeMethodAsync('Reorder',completed.row.dataset.extensionId,completed.target.dataset.extensionId,completed.after).catch(error=>console.error('HIT reorder failed',error));
    }, options);
    root.addEventListener('pointercancel',clear,options);
    window.addEventListener('blur',clear,options);
    ordering.set(root,()=>{clear();controller.abort()});
}
export function disconnectOrder(root) { ordering.get(root)?.(); ordering.delete(root); }
