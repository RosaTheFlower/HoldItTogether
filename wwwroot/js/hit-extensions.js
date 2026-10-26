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
            overlay.textContent = 'Install';
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
        if (packages.length !== 1 || !packages[0].name.toLowerCase().endsWith('.hitmenu')) {
            await reference.invokeMethodAsync('DropError', 'Drop one .hitmenu package at a time.'); return;
        }
        if (packages[0].size > 64 * 1024 * 1024) {
            await reference.invokeMethodAsync('DropError', 'Packages must not exceed 64 MB.'); return;
        }
        busy = true;
        try { await reference.invokeMethodAsync('ImportDropped', DotNet.createJSStreamReference(packages[0]), packages[0].name); }
        catch (error) { console.error('HIT extension import failed', error); }
        finally { busy = false; }
    }, options);
    window.addEventListener('blur', clear, { signal: controller.signal });
}
export function disconnectDrop() { dropController?.abort(); dropController = null; }
