const dragThreshold = 6;
const interactiveSelector =
    'button, a, input, textarea, select, [role="button"], [contenteditable], [data-no-drag]';

export function connectDragging(sphere, dotnet, onSettled, onStarting) {
    let gesture = null;
    let ending = false;
    let disposed = false;
    let suppressClick = false;

    function preventNativeSelection(event) {
        // Browser selection and HTML drag previews are separate from window dragging.
        event.preventDefault();
    }

    function report(error) {
        if (!disposed) console.error("DynamicSphere drag failed.", error);
    }

    function releaseCapture(current) {
        if (sphere.hasPointerCapture(current.id)) {
            sphere.releasePointerCapture(current.id);
        }
    }

    function scheduleMove(current) {
        if (disposed || current.ended || current.frame || current.moving) return;

        current.frame = requestAnimationFrame(() => {
            current.frame = 0;
            current.dirty = false;
            current.moving = true;
            current.work = (async () => {
                if (await current.start && !disposed) {
                    await dotnet.invokeMethodAsync("DragWindow");
                }
            })().catch(report).finally(() => {
                current.moving = false;
                if (current.dirty) scheduleMove(current);
            });
        });
    }

    function pointerDown(event) {
        const handle = event.target.closest("[data-hit-drag-handle]");
        const control = event.target.closest(interactiveSelector);
        if (disposed || ending || gesture || event.button !== 0 || !event.isPrimary ||
            (event.ctrlKey && event.target.closest("[data-ctrl-link]")) ||
            (control && control !== handle)) {
            return;
        }

        suppressClick = false;
        const surface=sphere.getBoundingClientRect();
        gesture = {
            id: event.pointerId,
            x: event.screenX,
            y: event.screenY,
            offsetX: event.clientX - surface.left,
            offsetY: event.clientY - surface.top,
            scale: window.devicePixelRatio || 1,
            dragged: false,
            deferredCapture: !!handle,
            ended: false,
            dirty: false,
            moving: false,
            frame: 0,
            start: null,
            work: Promise.resolve()
        };

        // Clickable drag handles retain their native click target until an actual drag starts.
        if (!gesture.deferredCapture) {
            event.preventDefault();
            sphere.setPointerCapture(event.pointerId);
        }
    }

    function pointerMove(event) {
        const current = gesture;
        if (!current || current.id !== event.pointerId || disposed) return;

        if (!current.dragged) {
            if (Math.hypot(event.screenX - current.x, event.screenY - current.y) < dragThreshold) {
                return;
            }

            current.dragged = true;
            suppressClick = true;
            const surface=sphere.getBoundingClientRect();
            sphere.style.setProperty('--sphere-drag-width',surface.width+'px');
            sphere.style.setProperty('--sphere-drag-height',surface.height+'px');
            sphere.dataset.hitDragSizing='true';
            if (current.deferredCapture) sphere.setPointerCapture(current.id);
            sphere.classList.add("is-dragging");
            current.start = (async()=>{
                await onStarting?.();
                if(disposed)return false;
                return dotnet.invokeMethodAsync("BeginDrag",current.offsetX,current.offsetY,current.scale);
            })().catch(error => {
                report(error);
                return false;
            });
        }

        current.dirty = true;
        scheduleMove(current);
    }

    async function pointerEnd(event) {
        const current = gesture;
        if (!current || current.id !== event.pointerId) return;

        gesture = null;
        current.ended = true;
        cancelAnimationFrame(current.frame);
        releaseCapture(current);

        if (!current.dragged) return;

        ending = true;
        let started = false;
        let origin;
        try {
            await current.work;
            started = await current.start;
            if (started && !disposed) {
                await dotnet.invokeMethodAsync("DragWindow");
            }
        } catch (error) {
            report(error);
        } finally {
            try {
                if (started && !disposed) {
                    origin=await dotnet.invokeMethodAsync("EndDrag");
                }
            } catch (error) {
                report(error);
            } finally {
                try {if(!disposed)await onSettled(origin);}
                finally {
                    ending=false;
                    delete sphere.dataset.hitDragSizing;
                    sphere.classList.remove('is-dragging');
                }
            }
        }
    }

    function click(event) {
        if (suppressClick) {
            suppressClick = false;
            event.preventDefault();
            event.stopImmediatePropagation();
            return;
        }

        if (!event.target.closest(interactiveSelector)) {
            sphere.focus({ preventScroll: true });
        }
    }

    function cancelGesture() {
        if (gesture) void pointerEnd({pointerId: gesture.id});
    }

    sphere.addEventListener("selectstart", preventNativeSelection, true);
    sphere.addEventListener("dragstart", preventNativeSelection, true);
    sphere.addEventListener("pointerdown", pointerDown);
    window.addEventListener("pointermove", pointerMove);
    window.addEventListener("pointerup", pointerEnd);
    window.addEventListener("pointercancel", pointerEnd);
    window.addEventListener("blur", cancelGesture);
    sphere.addEventListener("lostpointercapture", pointerEnd);
    sphere.addEventListener("click", click, true);

    return () => {
        disposed = true;
        const current = gesture;
        gesture = null;
        if (current) {
            current.ended = true;
            cancelAnimationFrame(current.frame);
            releaseCapture(current);
        }

        sphere.removeEventListener("selectstart", preventNativeSelection, true);
        sphere.removeEventListener("dragstart", preventNativeSelection, true);
        sphere.removeEventListener("pointerdown", pointerDown);
        window.removeEventListener("pointermove", pointerMove);
        window.removeEventListener("pointerup", pointerEnd);
        window.removeEventListener("pointercancel", pointerEnd);
        window.removeEventListener("blur", cancelGesture);
        sphere.removeEventListener("lostpointercapture", pointerEnd);
        sphere.removeEventListener("click", click, true);
        sphere.classList.remove("is-dragging");
        delete sphere.dataset.hitDragSizing;
    };
}
