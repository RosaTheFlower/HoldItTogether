import { connectDragging } from "./sphere-interactions.js";
import { connectMenuChrome } from "./hit-menu-chrome.js";
import { connectModifiers } from "./hit-modifiers.js";
import { connectReadingBackground } from "./hit-reading-background.js";
import { connectCompactMotion } from "./hit-compact-motion.js";
import { connectNavigation } from "./hit-navigation.js";
import { connectIdleOpacity } from "./hit-idle-opacity.js";

const observers = new WeakMap();

export async function observe(element, dotnet) {
    disconnect(element);

    const sphere = element.querySelector(".dynamic-sphere");
    const menuMeasure = sphere.querySelector(".hit-menu-measure");
    const disconnectMenuChrome = connectMenuChrome(sphere, dotnet);
    const disconnectModifiers = connectModifiers(sphere);
    const disconnectReadingBackground = connectReadingBackground(sphere);
    const compactMotion=connectCompactMotion(sphere);
    const disconnectNavigation=connectNavigation(sphere,dotnet);
    const disconnectIdleOpacity=connectIdleOpacity(element,sphere);
    let hostSignature='';
    function publishHost(){const open=sphere.classList.contains('is-open'),compact=sphere.classList.contains('is-menu-compact')||sphere.dataset.hitControlsHidden==='true',pinned=sphere.classList.contains('is-pinned');const next=[open,compact,pinned,document.hidden].join(':');if(next!==hostSignature){hostSignature=next;dotnet.invokeMethodAsync('PublishHostState',open,compact,pinned).catch(()=>{});document.dispatchEvent(new CustomEvent('hit-appearance-changed'));}}
    const hostObserver=new MutationObserver(publishHost);hostObserver.observe(sphere,{attributes:true,attributeFilter:['class','data-hit-controls-hidden']});document.addEventListener('visibilitychange',publishHost);publishHost();
    let disposed = false;
    let frame = 0;
    let measureFrame = 0;
    let sending = false;
    let latest = null;
    let lastSent = "";
    let pointerFrame = 0;
    let pointerEvent = null;
    let trackThemePointer = false;
    let canvasWidth = 0, canvasHeight = 0;
    let wasOpen = false, openingFromClosed = false;
    let prepared=false;
    let placementEpoch=0,geometryPaused=false,flight=Promise.resolve();
    let nativeWidth=0,nativeHeight=0;
    element.dataset.hitGeometryPending='true';
    function align(origin) {
        if(!Number.isFinite(origin?.x)||!Number.isFinite(origin?.y))return;
        const previous=element.style.getPropertyValue('--sphere-align-x')+element.style.getPropertyValue('--sphere-align-y');
        setStyle(sphere,'--sphere-origin-x',(origin.x*100)+'%');setStyle(sphere,'--sphere-origin-y',(origin.y*100)+'%');
        setStyle(element,'--sphere-align-x',origin.x<.25?'start':origin.x>.75?'end':'center');
        setStyle(element,'--sphere-align-y',origin.y<.25?'start':origin.y>.75?'end':'center');
        return previous!==element.style.getPropertyValue('--sphere-align-x')+element.style.getPropertyValue('--sphere-align-y');
    }

    function setStyle(node, property, value) {
        if (node.style.getPropertyValue(property) !== value) node.style.setProperty(property, value);
    }

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
        if (element.dataset.hitResourceSleep === 'true') return;
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
            frame = requestAnimationFrame(()=>{flight=flush();});
        }
    }

    async function flush() {
        frame = 0;
        if (disposed || sending || !latest) return;

        const size = latest;
        const epoch=placementEpoch;
        latest = null;
        const signature = [
            Math.ceil(size.width * size.scale), Math.ceil(size.height * size.scale),
            Math.floor(size.contentLeft*size.scale), Math.floor(size.contentTop*size.scale),
            Math.ceil((size.contentLeft+size.contentWidth)*size.scale),Math.ceil((size.contentTop+size.contentHeight)*size.scale),
            Math.round(size.borderRadius*size.scale)
        ].join(":");
        if (signature === lastSent) return;

        sending = true;
        try {
            const origin = await dotnet.invokeMethodAsync(
                "ResizeWindow", size.width, size.height, size.scale,
                size.contentLeft, size.contentTop,
                size.contentWidth, size.contentHeight, size.borderRadius);
            nativeWidth=size.width;nativeHeight=size.height;
            if (!disposed && epoch===placementEpoch && Number.isFinite(origin?.x) && Number.isFinite(origin?.y)) {
                if(align(origin))scheduleMeasure();
            }
            if(!disposed)delete element.dataset.hitGeometryPending;
            if(epoch===placementEpoch)lastSent = signature;
        } catch (error) {
            // Closing the native window can disconnect a pending invocation.
            if (!disposed) console.error("DynamicSphere window resize failed.", error);
        } finally {
            if(!disposed)delete element.dataset.hitGeometryPending;
            sending = false;
            if (latest) schedule();
        }
    }
    function flushNow(){
        cancelAnimationFrame(frame);frame=0;
        if(sending)return flight;
        flight=flush();return flight;
    }

    function measure(force=false) {
        if(!prepared||disposed||(geometryPaused&&!force))return;
        const open = sphere.classList.contains("is-open");
        if(open&&!wasOpen)dotnet.invokeMethodAsync('PrepareSurface').catch(error=>{if(!disposed)console.error('HIT zoom reset failed.',error);});
        if (open && !wasOpen) openingFromClosed = true;
        if (!open) openingFromClosed = false;
        const panel = sphere.querySelector('.hit-menu-panel:not([hidden])');
        compactMotion.update(panel);
        const pending = !!panel?.querySelector('[data-hit-layout-pending="true"]');
        if (open && pending && openingFromClosed && !sphere.hasAttribute("data-hit-opening-pending")) {
            setStyle(sphere, "--sphere-pending-width", sphere.offsetWidth + "px");
            setStyle(sphere, "--sphere-pending-height", sphere.offsetHeight + "px");
            sphere.setAttribute("data-hit-opening-pending", "true");
        } else if (!open || !pending) sphere.removeAttribute("data-hit-opening-pending");
        if (open && pending) sphere.setAttribute("data-hit-menu-pending", "true");
        else sphere.removeAttribute("data-hit-menu-pending");
        let targetWidth = 0, targetHeight = 0;
        if (menuMeasure && !pending) {
            // Layout dimensions ignore the content reveal's scale/blur.
            // Intrinsic menu size does not depend on the animated window size.
            targetWidth = Math.ceil(menuMeasure.offsetWidth);
            targetHeight = Math.ceil(menuMeasure.offsetHeight);
            setStyle(sphere, "--sphere-open-width", targetWidth + "px");
            setStyle(sphere, "--sphere-open-height", targetHeight + "px");
            if (open) openingFromClosed = false;
        }
        wasOpen = open;
        const sizing = sphere.getAnimations().some(animation =>
            (animation.transitionProperty === "width" || animation.transitionProperty === "height") && animation.playState !== "finished");
        if ((open && !pending) || canvasWidth) {
            const width = sphere.offsetWidth, height = sphere.offsetHeight;
            if (open) {
                canvasWidth = Math.max(canvasWidth, targetWidth, width);
                canvasHeight = Math.max(canvasHeight, targetHeight, height);
                // Retain the largest canvas until closing. Shrinking the native WebView
                // between wheel selections can expose its transparent backing store.
            } else if (!sizing) { canvasWidth = 0; canvasHeight = 0; }
            if (canvasWidth) {
                // Reserve the canvas once. Only the sphere inside it animates, so native
                // resizing does not repeatedly move/re-layout the WebView during reveal.
                const padding = getComputedStyle(element);
                setStyle(element, "--sphere-canvas-width", (canvasWidth + parseFloat(padding.paddingLeft) + parseFloat(padding.paddingRight)) + "px");
                setStyle(element, "--sphere-canvas-height", (canvasHeight + parseFloat(padding.paddingTop) + parseFloat(padding.paddingBottom)) + "px");
                element.setAttribute("data-hit-canvas", "true");
            } else element.removeAttribute("data-hit-canvas");
        }
        const bounds = element.getBoundingClientRect();
        const content = sphere.getBoundingClientRect();
        setStyle(sphere,'--hit-dialog-left',(content.left+content.width/2)+'px');
        setStyle(sphere,'--hit-dialog-top',(content.top+content.height/2)+'px');
        setStyle(sphere,'--hit-dialog-max-width',Math.max(1,content.width-32)+'px');
        setStyle(sphere,'--hit-dialog-max-height',Math.max(1,content.height-32)+'px');
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
        if(bounds.width!==nativeWidth||bounds.height!==nativeHeight)void flushNow();
        else schedule();
    }

    const disconnectDragging = connectDragging(sphere,dotnet,async origin=>{
        // Receive the visible-surface pivot before publishing geometry for its new alignment.
        placementEpoch++;align(origin);lastSent='';geometryPaused=false;measure();await flushNow();
    },async()=>{
        // Old resize replies cannot re-align a surface that has already started moving.
        geometryPaused=true;placementEpoch++;latest=null;cancelAnimationFrame(frame);frame=0;
        await flight;
        measure(true);await flushNow();
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
    async function refreshPlacement(){
        geometryPaused=true;placementEpoch++;
        try {await flight;if(disposed)return;align(await dotnet.invokeMethodAsync('PrepareSurface'));lastSent='';}
        catch(error){if(!disposed)console.error('HIT placement update failed.',error);}
        finally {geometryPaused=false;if(!disposed){measure();await flushNow();}}
    }
    const observer = new ResizeObserver(scheduleMeasure);
    observer.observe(element);
    observer.observe(sphere);
    if (menuMeasure) observer.observe(menuMeasure);
    // Apply size/readiness changes in the mutation microtask before the next paint.
    const layoutObserver = new MutationObserver(records=>{
        if(records.some(record=>record.target===sphere&&record.attributeName==='data-hit-placement'))void refreshPlacement();
        else measure();
    });
    layoutObserver.observe(sphere, { subtree: true, childList: true, attributes: true,
        attributeFilter: ["class", "hidden", "data-hit-layout-pending", "data-hit-placement"] });
    function sizingEnded(event) {
        if (event.target === sphere && (event.propertyName === 'width' || event.propertyName === 'height')) scheduleMeasure();
    }
    sphere.addEventListener('transitionend', sizingEnded);
    sphere.addEventListener('transitioncancel', sizingEnded);
    window.addEventListener("resize", scheduleMeasure);
    document.addEventListener("hit-theme-status", onThemeChange);
    window.addEventListener("pointermove", onPointerMove, { passive: true });

    observers.set(element, () => {
        disposed = true;
        disconnectDragging();
        disconnectNavigation();
        disconnectIdleOpacity();
        hostObserver.disconnect();document.removeEventListener('visibilitychange',publishHost);
        disconnectMenuChrome();
        disconnectModifiers();
        disconnectReadingBackground();
        compactMotion.disconnect();
        observer.disconnect();
        layoutObserver.disconnect();
        sphere.removeEventListener('transitionend', sizingEnded);
        sphere.removeEventListener('transitioncancel', sizingEnded);
        element.removeAttribute("data-hit-canvas");
        delete element.dataset.hitGeometryPending;
        sphere.removeAttribute("data-hit-opening-pending");
        sphere.removeAttribute("data-hit-menu-pending");
        window.removeEventListener("resize", scheduleMeasure);
        document.removeEventListener("hit-theme-status", onThemeChange);
        window.removeEventListener("pointermove", onPointerMove);
        cancelAnimationFrame(pointerFrame);
        cancelAnimationFrame(measureFrame);
        cancelAnimationFrame(frame);
        latest = null;
    });
    try {align(await dotnet.invokeMethodAsync('PrepareSurface'));}
    catch(error){if(!disposed)console.error('HIT surface initialization failed.',error);}
    if(disposed)return;
    prepared=true;updateThemePointerPreference();measure();
}

export function disconnect(element) {
    observers.get(element)?.();
    observers.delete(element);
}

// Public, optional theme inputs. No theme can execute its own JavaScript.
export function setPointer(element, x, y) {
    if (element.dataset.hitResourceSleep === 'true') return;
    const sphere = element.querySelector(".dynamic-sphere");
    if (!sphere || !Number.isFinite(x) || !Number.isFinite(y)) return;
    x = Math.round(Math.max(-1, Math.min(1, x)) * 100) / 100;
    y = Math.round(Math.max(-1, Math.min(1, y)) * 100) / 100;
    sphere.style.setProperty("--hit-pointer-x", String(x));
    sphere.style.setProperty("--hit-pointer-y", String(y));
    sphere.style.setProperty("--hit-pointer-abs-x", String(Math.abs(x)));
    sphere.style.setProperty("--hit-pointer-abs-y", String(Math.abs(y)));
}
