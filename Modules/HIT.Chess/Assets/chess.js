// DOM mechanics only: annotations, local scrolling and lifecycle. Chess state stays in C#.
export function connect(root, reference) {
    let disposed = false, gesture = null, previousBoard = null, previousPosition = null, scrollFrame = 0;
    const arrows = new Map(), squares = new Set();
    const sphere = root.closest('.dynamic-sphere');
    const hoverTarget = sphere ?? root;
    let pointerInside = hoverTarget.matches(':hover');
    const isPointerOver = () => pointerInside || hoverTarget.matches(':hover');
    const isBoardVisible = () => root.dataset.hitView !== 'settings' && sphere?.classList.contains('is-open') && !root.closest('.hit-menu-panel')?.hidden;
    const eligible = () => !disposed && !gesture && !isPointerOver() && root.dataset.hitView === 'board' &&
        sphere?.classList.contains('is-pinned') && sphere.classList.contains('is-open') &&
        !root.closest('.hit-menu-panel')?.hidden;
    const compact = () => {
        if (eligible()) reference.invokeMethodAsync('CompactAsync').catch(() => {});
    };
    // The host owns the configurable departure timer; the module only checks eligibility.
    const entered = () => { pointerInside = true; };
    const left = () => { pointerInside = false; };
    const scrollRegions = new Map();
    const movingPieces = new Set();
    let pieceSnapshot = new Map(), previousPly = null, previousGame = null, selectedSquare = null, previousBoardSize = 0;
    const moveDuration = 250;
    let layoutReady = root.dataset.hitLayoutPending !== 'true';
    let audio = null, moveSound = null, captureSound = null;
    const playingSounds = new Set();
    const motionPreference = matchMedia('(prefers-reduced-motion: reduce)');
    const motionAllowed = () => document.documentElement.dataset.hitMotion !== 'off' && !motionPreference.matches;
    function stopSounds() {
        for (const source of playingSounds) { try { source.stop(); } catch (error) { if (!(error instanceof DOMException)) throw error; } source.disconnect(); }
        playingSounds.clear();
    }
    function soundBuffer(context, capture) {
        const duration = capture ? .24 : .1;
        const buffer = context.createBuffer(1, Math.ceil(context.sampleRate * duration), context.sampleRate);
        const samples = buffer.getChannelData(0);
        let low = 0;
        for (let i = 0; i < samples.length; i++) {
            const t = i / context.sampleRate;
            const noise = Math.random() * 2 - 1;
            low += .14 * (noise - low);
            if (capture) {
                // A low wooden contact followed by a brighter, separated clack.
                const contact = (low * .45 + Math.sin(2 * Math.PI * 130 * t) * .16) * Math.exp(-t * 70);
                const after = t - .075;
                const clack = after > 0 ? ((noise - low) * .23 + Math.sin(2 * Math.PI * 720 * after) * .1) * Math.exp(-after * 65) : 0;
                samples[i] = contact + clack;
            } else {
                samples[i] = (low * .55 + Math.sin(2 * Math.PI * 240 * t) * .14) * Math.exp(-t * 65);
            }
        }
        return buffer;
    }
    function unlockAudio() {
        if (disposed || root.dataset.soundEnabled !== 'true') return;
        const Audio = window.AudioContext ?? window.webkitAudioContext;
        if (!Audio) return;
        try {
            if (!audio) {
                audio = new Audio();
                moveSound = soundBuffer(audio, false); captureSound = soundBuffer(audio, true);
            }
            if (audio.state === 'suspended') audio.resume().catch(() => {});
        } catch (error) { if (!(error instanceof DOMException)) throw error; }
    }
    function playSound(capture) {
        if (!audio || audio.state !== 'running' || root.dataset.soundEnabled !== 'true' || disposed) return;
        const source = audio.createBufferSource(), gain = audio.createGain();
        source.buffer = capture ? captureSound : moveSound;
        gain.gain.value = .16;
        source.connect(gain); gain.connect(audio.destination);
        playingSounds.add(source);
        source.onended = () => { playingSounds.delete(source); source.disconnect(); gain.disconnect(); };
        source.start();
    }
    function cancelMoves() {
        for (const item of [...movingPieces]) { item.animation.cancel(); item.ghost.remove(); item.target.style.removeProperty('visibility'); delete item.target.dataset.chessMoving; }
        movingPieces.clear();
    }
    function motionChanged() { if (!motionAllowed()) cancelMoves(); }
    const motionObserver = new MutationObserver(motionChanged);
    motionObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-hit-motion'] });
    motionPreference.addEventListener('change', motionChanged);
    function animatePiece(from, to) {
        const previous = pieceSnapshot.get(from), destination = cell(to), frame = root.querySelector('.chess-board-frame');
        const target = destination?.querySelector('.chess-piece');
        if (!previous || !destination || !target || !frame) return;
        const ghost = document.createElement('div');
        ghost.className = 'chess-moving-piece';
        ghost.setAttribute('aria-hidden', 'true');
        ghost.style.left = Number(destination.dataset.col) * 12.5 + '%';
        ghost.style.top = Number(destination.dataset.row) * 12.5 + '%';
        ghost.innerHTML = previous.html;
        frame.append(ghost); target.style.visibility = 'hidden'; target.dataset.chessMoving = 'true';
        const size = board().getBoundingClientRect().width / 8;
        const dx = (previous.col - Number(destination.dataset.col)) * size;
        const dy = (previous.row - Number(destination.dataset.row)) * size;
        const lifted = from === selectedSquare;
        const animation = ghost.animate([
            { transform: 'translate3d(' + dx + 'px,' + (dy - (lifted ? 6 : 0)) + 'px,0) scale(' + (lifted ? 1.1 : 1) + ')' },
            { transform: 'translate3d(0,0,0) scale(1)' }
        ], { duration: moveDuration, easing: 'cubic-bezier(.42,0,.58,1)', fill: 'both' });
        const item = { animation, ghost, target };
        movingPieces.add(item);
        animation.finished.catch(() => {}).finally(() => { movingPieces.delete(item); ghost.remove(); target.style.removeProperty('visibility'); delete target.dataset.chessMoving; });
        return animation;
    }
    function syncMoves(currentBoard) {
        const ply = Number(root.dataset.ply), game = root.dataset.game;
        const size = currentBoard?.getBoundingClientRect().width ?? 0;
        if (!currentBoard || !isBoardVisible() || currentBoard !== previousBoard || game !== previousGame || size !== previousBoardSize || !motionAllowed()) cancelMoves();
        previousBoardSize = size;
        if (root.dataset.soundEnabled !== 'true') stopSounds();
        if (currentBoard === previousBoard && game === previousGame && ply === previousPly) return;
        if (currentBoard && isBoardVisible() && currentBoard === previousBoard && game === previousGame && previousPly + 1 === ply) {
            const from = Number(root.dataset.lastFrom), to = Number(root.dataset.lastTo);
            const animations = [];
            if (motionAllowed()) {
                animations.push(animatePiece(from, to));
                if (Math.abs(pieceSnapshot.get(from)?.piece) === 6 && Math.abs(to - from) === 2)
                    animations.push(animatePiece(to > from ? from + 3 : from - 4, to > from ? from + 1 : from - 1));
            }
            const capture = root.dataset.lastCapture === 'true';
            const active = animations.filter(Boolean);
            if (active.length) Promise.allSettled(active.map(animation => animation.finished)).then(() => {
                if (root.dataset.game === game && Number(root.dataset.ply) === ply && board() && isBoardVisible()) playSound(capture);
            });
            else playSound(capture);
        }
        pieceSnapshot = new Map();
        if (currentBoard) for (const element of currentBoard.querySelectorAll('[data-square]')) {
            const svg = element.querySelector('.chess-piece');
            if (!svg) continue;
            const clone = svg.cloneNode(true); clone.style.removeProperty('visibility');
            delete clone.dataset.chessMoving; delete clone.dataset.chessPending;
            pieceSnapshot.set(Number(element.dataset.square), { piece: Number(element.dataset.piece), row: Number(element.dataset.row), col: Number(element.dataset.col), html: clone.outerHTML });
        }
        previousPly = ply; previousGame = game;
    }
    function updateEdges(element) {
        const horizontal = element.dataset.chessScroll === 'x';
        const position = horizontal ? element.scrollLeft : element.scrollTop;
        const maximum = horizontal ? element.scrollWidth - element.clientWidth : element.scrollHeight - element.clientHeight;
        const frame = element.parentElement;
        const start = String(position > 1), end = String(maximum - position > 1);
        if (frame.dataset.canStart !== start) frame.dataset.canStart = start;
        if (frame.dataset.canEnd !== end) frame.dataset.canEnd = end;
    }
    function updateScrollHints() {
        for (const element of scrollRegions.keys()) updateEdges(element);
    }
    function scheduleScrollHints() {
        if (disposed || scrollFrame) return;
        scrollFrame = requestAnimationFrame(() => { scrollFrame = 0; if (!disposed) updateScrollHints(); });
    }
    const scrollObserver = new ResizeObserver(scheduleScrollHints);
    function syncScrollRegions() {
        for (const [element, content] of scrollRegions) {
            if (!root.contains(element)) {
                scrollObserver.unobserve(element);
                if (content) scrollObserver.unobserve(content);
                scrollRegions.delete(element);
            }
        }
        for (const element of root.querySelectorAll('[data-chess-scroll]')) {
            const content = element.firstElementChild;
            if (!scrollRegions.has(element)) scrollObserver.observe(element);
            const previousContent = scrollRegions.get(element);
            if (previousContent !== content) {
                if (previousContent) scrollObserver.unobserve(previousContent);
                if (content) scrollObserver.observe(content);
            }
            scrollRegions.set(element, content);
        }
        updateScrollHints();
    }
    function scrolled(event) {
        if (event.target instanceof Element && event.target.hasAttribute('data-chess-scroll')) updateEdges(event.target);
    }
    const board = () => root.querySelector('.chess-board');
    const cell = square => root.querySelector('.chess-square[data-square="' + square + '"]');
    function point(event) {
        const area = board()?.getBoundingClientRect();
        if (!area || event.clientX < area.left || event.clientX >= area.right || event.clientY < area.top || event.clientY >= area.bottom) return null;
        const col = Math.floor((event.clientX - area.left) / area.width * 8);
        const row = Math.floor((event.clientY - area.top) / area.height * 8);
        const element = root.querySelector('.chess-square[data-row="' + row + '"][data-col="' + col + '"]');
        return element ? Number(element.dataset.square) : null;
    }
    function center(square) {
        const element = cell(square);
        return element ? [Number(element.dataset.col) * 100 + 50, Number(element.dataset.row) * 100 + 50] : null;
    }
    function arrowNode(from, to, preview = false) {
        const start = center(from), end = center(to);
        if (!start || !end || from === to) return null;
        const ns = 'http://www.w3.org/2000/svg';
        const group = document.createElementNS(ns, 'g');
        group.setAttribute('class', 'chess-arrow' + (preview ? ' preview' : ''));
        group.dataset.from = from; group.dataset.to = to;
        let corner = start;
        const dx = end[0] - start[0], dy = end[1] - start[1];
        // Knight arrows use the familiar L shape; other arrows are straight.
        if (Math.abs(dx * dy) === 20000) corner = Math.abs(dy) > Math.abs(dx) ? [start[0], end[1]] : [end[0], start[1]];
        const length = Math.hypot(end[0] - corner[0], end[1] - corner[1]);
        const ux = (end[0] - corner[0]) / length, uy = (end[1] - corner[1]) / length;
        const bx = end[0] - ux * 28, by = end[1] - uy * 28;
        const path = document.createElementNS(ns, 'path');
        path.setAttribute('d', 'M' + start.join(' ') + (corner === start ? '' : ' L' + corner.join(' ')) + ' L' + bx + ' ' + by);
        path.setAttribute('fill', 'none'); path.setAttribute('stroke-width', '14'); path.setAttribute('stroke-linecap', 'round'); path.setAttribute('stroke-linejoin', 'round');
        const head = document.createElementNS(ns, 'path');
        head.setAttribute('d', 'M' + end.join(' ') + ' L' + (bx - uy * 21) + ' ' + (by + ux * 21) + ' L' + (bx + uy * 21) + ' ' + (by - ux * 21) + ' Z');
        head.setAttribute('stroke', 'none');
        group.append(path, head); return group;
    }
    function paint() {
        root.querySelectorAll('.chess-square').forEach(element => element.classList.toggle('marked-red', squares.has(Number(element.dataset.square))));
        const overlay = root.querySelector('.chess-annotations');
        if (!overlay) return;
        overlay.replaceChildren();
        for (const [from, to] of arrows.values()) { const node = arrowNode(from, to); if (node) overlay.append(node); }
        if (gesture?.to !== null && gesture?.to !== undefined) { const node = arrowNode(gesture.from, gesture.to, true); if (node) overlay.append(node); }
    }
    function clear() { squares.clear(); arrows.clear(); gesture = null; paint(); }
    function sync() {
        if (disposed) return;
        pointerInside = hoverTarget.matches(':hover');
        const ready = root.dataset.hitLayoutPending !== 'true';
        if (ready && !layoutReady) previousGame = null;
        layoutReady = ready;
        const currentBoard = board(), position = root.dataset.position;
        syncMoves(currentBoard);
        for (const svg of root.querySelectorAll('[data-chess-pending="true"]'))
            if (svg.dataset.chessMoving !== 'true') svg.style.removeProperty('visibility');
        const selected = currentBoard?.querySelector('.selected');
        selectedSquare = selected ? Number(selected.dataset.square) : null;
        if (currentBoard !== previousBoard || position !== previousPosition) {
            clear();
            const history = root.querySelector('.chess-history');
            if (history) history.scrollLeft = history.scrollWidth;
            previousBoard = currentBoard; previousPosition = position;
        } else paint();
        syncScrollRegions();
        return movingPieces.size ? moveDuration : 0;
    }
    function pointerDown(event) {
        unlockAudio();
        if (!event.target.closest('.chess-board-frame')) return;
        if (event.button === 0) { if (movingPieces.size) { event.preventDefault(); event.stopPropagation(); return; } clear(); return; }
        if (event.button !== 2) return;
        const from = point(event); if (from === null) return;
        event.preventDefault(); event.stopPropagation();
        gesture = { from, to: from, id: event.pointerId };
        root.setPointerCapture(event.pointerId);
    }
    function pointerMove(event) {
        if (!gesture || gesture.id !== event.pointerId) return;
        event.preventDefault(); event.stopPropagation();
        gesture.to = point(event); paint();
    }
    function pointerUp(event) {
        if (!gesture || gesture.id !== event.pointerId) return;
        event.preventDefault(); event.stopPropagation();
        const { from, id } = gesture, to = point(event);
        gesture = null;
        if (root.hasPointerCapture(id)) root.releasePointerCapture(id);
        if (to !== null) {
            if (to === from) squares.has(from) ? squares.delete(from) : squares.add(from);
            else { const key = from + ':' + to; arrows.has(key) ? arrows.delete(key) : arrows.set(key, [from, to]); }
        }
        paint();
    }
    function cancelGesture() {
        if (gesture && root.hasPointerCapture(gesture.id)) root.releasePointerCapture(gesture.id);
        gesture = null; paint();
    }
    function contextMenu(event) {
        if (!event.target.closest('.chess-board-frame')) return;
        event.preventDefault(); event.stopPropagation();
    }
    function click(event) {
        if (movingPieces.size && event.target.closest('.chess-board') && !event.target.closest('.chess-expand')) {
            event.preventDefault(); event.stopPropagation();
        }
    }
    function wheel(event) {
        const history = event.target.closest('.chess-history');
        if (history) { history.scrollLeft += event.deltaX || event.deltaY; updateEdges(history); event.preventDefault(); event.stopPropagation(); }
        else if (root.dataset.hitView === 'settings' || event.target.closest('[data-chess-scroll], dialog')) event.stopPropagation();
    }
    function changed(event) { if (event.target instanceof HTMLSelectElement) queueMicrotask(() => event.target.blur()); }
    function blurred() { cancelGesture(); }
    function key(event) { unlockAudio(); if (event.key === 'Escape' && root.dataset.hitView !== 'settings') clear(); }
    const idle = () => compact();
    root.addEventListener('hit-menu-idle', idle);
    hoverTarget.addEventListener('pointerleave', left);
    hoverTarget.addEventListener('pointerenter', entered);
    root.addEventListener('pointerdown', pointerDown, true);
    root.addEventListener('contextmenu', contextMenu, true);
    root.addEventListener('click', click, true);
    root.addEventListener('wheel', wheel, { passive: false });
    root.addEventListener('change', changed);
    root.addEventListener('scroll', scrolled, true);
    root.addEventListener('keydown', key);
    window.addEventListener('pointermove', pointerMove, { capture: true, passive: false });
    window.addEventListener('pointerup', pointerUp, true);
    window.addEventListener('pointercancel', cancelGesture, true);
    window.addEventListener('blur', blurred);
    sync();
    return { sync, isPointerOver, confirm() {
        const dialog = root.querySelector('dialog[data-chess-confirm]');
        if (dialog && !dialog.open) { dialog.showModal(); dialog.querySelector('button')?.focus(); }
    }, dispose() {
        disposed = true; cancelGesture();
        cancelMoves(); stopSounds();
        motionObserver.disconnect(); motionPreference.removeEventListener('change', motionChanged);
        if (audio && audio.state !== 'closed') audio.close().catch(() => {});
        cancelAnimationFrame(scrollFrame); scrollObserver.disconnect(); scrollRegions.clear();
        root.removeEventListener('hit-menu-idle', idle);
        hoverTarget.removeEventListener('pointerleave', left);
        hoverTarget.removeEventListener('pointerenter', entered);
        root.removeEventListener('pointerdown', pointerDown, true);
        root.removeEventListener('contextmenu', contextMenu, true);
        root.removeEventListener('click', click, true);
        root.removeEventListener('wheel', wheel);
        root.removeEventListener('change', changed);
        root.removeEventListener('scroll', scrolled, true);
        root.removeEventListener('keydown', key);
        window.removeEventListener('pointermove', pointerMove, true);
        window.removeEventListener('pointerup', pointerUp, true);
        window.removeEventListener('pointercancel', cancelGesture, true);
        window.removeEventListener('blur', blurred);
    }};
}
