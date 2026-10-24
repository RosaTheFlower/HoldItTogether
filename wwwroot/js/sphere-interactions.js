const dragThreshold = 6;
const interactiveSelector =
    "button, a, input, textarea, select, [contenteditable], [data-no-drag]";

export function connectDragging(sphere, dotnet, onSettled) {
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
        if (disposed || ending || gesture || event.button !== 0 || !event.isPrimary ||
            event.target.closest(interactiveSelector)) {
            return;
        }

        suppressClick = false;
        gesture = {
            id: event.pointerId,
            x: event.screenX,
            y: event.screenY,
            offsetX: event.clientX - window.innerWidth / 2,
            offsetFromBottom: window.innerHeight - event.clientY,
            scale: window.devicePixelRatio || 1,
            dragged: false,
            ended: false,
            dirty: false,
            moving: false,
            frame: 0,
            start: null,
            work: Promise.resolve()
        };

        // Delay focus until a genuine click. A drag must not open the menu.
        event.preventDefault();
        sphere.setPointerCapture(event.pointerId);
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
            sphere.classList.add("is-dragging");
            current.start = dotnet.invokeMethodAsync(
                "BeginDrag", current.offsetX, current.offsetFromBottom, current.scale
            ).catch(error => {
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
                    await dotnet.invokeMethodAsync("EndDrag");
                }
            } catch (error) {
                report(error);
            } finally {
                ending = false;
                sphere.classList.remove("is-dragging");
                if (!disposed) onSettled();
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

    sphere.addEventListener("selectstart", preventNativeSelection, true);
    sphere.addEventListener("dragstart", preventNativeSelection, true);
    sphere.addEventListener("pointerdown", pointerDown);
    sphere.addEventListener("pointermove", pointerMove);
    sphere.addEventListener("pointerup", pointerEnd);
    sphere.addEventListener("pointercancel", pointerEnd);
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
        sphere.removeEventListener("pointermove", pointerMove);
        sphere.removeEventListener("pointerup", pointerEnd);
        sphere.removeEventListener("pointercancel", pointerEnd);
        sphere.removeEventListener("lostpointercapture", pointerEnd);
        sphere.removeEventListener("click", click, true);
        sphere.classList.remove("is-dragging");
    };
}