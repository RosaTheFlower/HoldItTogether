const connections = new WeakMap();
export function connectIdle(element, dotnet) {
    disconnectIdle(element);
    if (!element?.isConnected) return;
    let pending = false, disposed = false;
    const idle = async () => {
        if (pending || disposed || !element.isConnected) return;
        pending = true;
        try { await dotnet.invokeMethodAsync("CompactAfterInactivity"); }
        catch (error) { if (!disposed) console.error("HIT clock compact transition failed.", error); }
        finally { pending = false; }
    };
    element.addEventListener("hit-menu-idle", idle);
    connections.set(element, () => { disposed = true; element.removeEventListener("hit-menu-idle", idle); });
}
export function disconnectIdle(element) { connections.get(element)?.(); connections.delete(element); }
