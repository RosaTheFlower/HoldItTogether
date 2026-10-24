import { connectDragging } from "./sphere-interactions.js";

const observers = new WeakMap();

export function observe(element, dotnet) {
    disconnect(element);

    const sphere = element.querySelector(".dynamic-sphere");
    const menuMeasure = sphere.querySelector(".hit-menu-measure");
    let disposed = false;
    let frame = 0;
    let measureFrame = 0;
    let sending = false;
    let latest = null;
    let lastSent = "";
    let pointerFrame = 0;
    let pointerEvent = null;
    let trackThemePointer = false;

    function updateThemePointerPreference() {
        const enabled = getComputedStyle(sphere).getPropertyValue("--hit-track-pointer").trim() === "1";
        if (enabled === trackThemePointer) return;
        trackThemePointer = enabled;
        if (!enabled) setPointer(element, 0, 0);
        dotnet.invokeMethodAsync("SetThemePointerTracking", enabled).catch(error => {
            if (!disposed) console.error("HIT theme pointer preference failed.", error);
        });
    }

    function onThemeChange() {
        updateThemePointerPreference();
        scheduleMeasure();
    }

    function onPointerMove(event) {
        if (!trackThemePointer || disposed) return;
        pointerEvent = { x: event.clientX, y: event.clientY };
        if (pointerFrame) return;
        pointerFrame = requestAnimationFrame(() => {
            pointerFrame = 0;
            if (!pointerEvent || disposed) return;
            const bounds = sphere.getBoundingClientRect();
            setPointer(element,
                (pointerEvent.x - bounds.left - bounds.width / 2) / Math.max(80, bounds.width / 2),
                (pointerEvent.y - bounds.top - bounds.height / 2) / Math.max(80, bounds.height / 2));
        });
    }

    function schedule() {
        if (!disposed && !sending && !frame) {
            frame = requestAnimationFrame(flush);
        }
    }

    async function flush() {
        frame = 0;
        if (disposed || sending || !latest) return;

        const size = latest;
        latest = null;
        const signature = [
            Math.ceil(size.width * size.scale), Math.ceil(size.height * size.scale),
            size.contentLeft, size.contentTop, size.contentWidth, size.contentHeight, size.borderRadius
        ].join(":");
        if (signature === lastSent) return;

        sending = true;
        try {
            const origin = await dotnet.invokeMethodAsync(
                "ResizeWindow", size.width, size.height, size.scale,
                size.contentLeft, size.contentTop,
                size.contentWidth, size.contentHeight, size.borderRadius);
            if (!disposed && Number.isFinite(origin?.x) && Number.isFinite(origin?.y)) {
                sphere.style.setProperty("--sphere-origin-x", (origin.x * 100) + "%");
                sphere.style.setProperty("--sphere-origin-y", (origin.y * 100) + "%");
                sphere.style.setProperty("--sphere-enter-x", ((origin.x - 0.5) * 16) + "px");
                sphere.style.setProperty("--sphere-enter-y", ((origin.y - 0.5) * 16) + "px");
            }
            lastSent = signature;
        } catch (error) {
            // Closing the native window can disconnect a pending invocation.
            if (!disposed) console.error("DynamicSphere window resize failed.", error);
        } finally {
            sending = false;
            if (latest) schedule();
        }
    }

    function measure() {
        if (menuMeasure) {
            // Layout dimensions ignore the content reveal's scale/blur.
            // Intrinsic menu size does not depend on the animated window size.
            sphere.style.setProperty("--sphere-open-width", Math.ceil(menuMeasure.offsetWidth) + "px");
            sphere.style.setProperty("--sphere-open-height", Math.ceil(menuMeasure.offsetHeight) + "px");
        }
        const bounds = element.getBoundingClientRect();
        const content = sphere.getBoundingClientRect();
        latest = {
            width: bounds.width,
            height: bounds.height,
            scale: window.devicePixelRatio || 1,
            contentLeft: content.left - bounds.left,
            contentTop: content.top - bounds.top,
            contentWidth: content.width,
            contentHeight: content.height,
            borderRadius: parseFloat(getComputedStyle(sphere).borderTopLeftRadius) || 0
        };
        schedule();
    }

    const disconnectDragging = connectDragging(sphere, dotnet, () => {
        // Dragging changes the desktop origin even when CSS dimensions do not.
        lastSent = "";
        measure();
    });

    // A theme or disabled transitions can change intrinsic size immediately.
    // Write geometry in the next frame rather than during ResizeObserver delivery.
    function scheduleMeasure() {
        if (!disposed && !measureFrame) {
            measureFrame = requestAnimationFrame(() => {
                measureFrame = 0;
                measure();
            });
        }
    }
    const observer = new ResizeObserver(scheduleMeasure);
    observer.observe(element);
    observer.observe(sphere);
    if (menuMeasure) observer.observe(menuMeasure);
    window.addEventListener("resize", scheduleMeasure);
    document.addEventListener("hit-theme-status", onThemeChange);
    window.addEventListener("pointermove", onPointerMove, { passive: true });
    updateThemePointerPreference();
    measure();

    observers.set(element, () => {
        disposed = true;
        disconnectDragging();
        observer.disconnect();
        window.removeEventListener("resize", scheduleMeasure);
        document.removeEventListener("hit-theme-status", onThemeChange);
        window.removeEventListener("pointermove", onPointerMove);
        cancelAnimationFrame(pointerFrame);
        cancelAnimationFrame(measureFrame);
        cancelAnimationFrame(frame);
        latest = null;
    });
}

export function disconnect(element) {
    observers.get(element)?.();
    observers.delete(element);
}

// Public, optional theme inputs. No theme can execute its own JavaScript.
export function setPointer(element, x, y) {
    const sphere = element.querySelector(".dynamic-sphere");
    if (!sphere || !Number.isFinite(x) || !Number.isFinite(y)) return;
    x = Math.round(Math.max(-1, Math.min(1, x)) * 100) / 100;
    y = Math.round(Math.max(-1, Math.min(1, y)) * 100) / 100;
    sphere.style.setProperty("--hit-pointer-x", String(x));
    sphere.style.setProperty("--hit-pointer-y", String(y));
    sphere.style.setProperty("--hit-pointer-abs-x", String(Math.abs(x)));
    sphere.style.setProperty("--hit-pointer-abs-y", String(Math.abs(y)));
}