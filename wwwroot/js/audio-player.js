// Data comes from the native output-device FFT, never from random/decorative animation.
const bands = new WeakMap();
export function renderSpectrum(element, values) {
    if (!element?.isConnected || !Array.isArray(values)) return;
    let nodes = bands.get(element);
    if (!nodes) { nodes = [...element.querySelectorAll(".spectrum-bar")]; bands.set(element, nodes); }
    for (let index = 0; index < nodes.length; index++) {
        const level = Number.isFinite(values[index]) ? Math.max(0, Math.min(1, values[index])) : 0;
        nodes[index].style.setProperty("--level", level.toFixed(3));
    }
}
const scrolling = new WeakSet();
export function enableLocalScroll(element) {
    if (!element || scrolling.has(element)) return;
    // Native scrolling stays inside the bounded view, outside Blazor's menu wheel handler.
    element.addEventListener("wheel", event => event.stopPropagation(), {passive:true});
    scrolling.add(element);
}

const lyricViews = new WeakMap();
export function followLyrics(element, index, instant = false) {
    if (!element?.isConnected) return;
    let view = lyricViews.get(element);
    if (!view) {
        view = {index, lastManual:0, timer:0, observer:null};
        view.wheel = () => {
            view.lastManual = performance.now();
            clearTimeout(view.timer);
            view.timer = setTimeout(() => center(false), 1800);
        };
        const center = (force) => {
            element.style.setProperty("--lyrics-viewport-size",element.clientHeight+"px");
            if (!element.isConnected || (!force && performance.now() - view.lastManual < 1600)) return;
            const target = element.querySelector('[data-line-index="' + Math.max(0, view.index) + '"]');
            if (!target) return;
            const top = target.getBoundingClientRect().top - element.getBoundingClientRect().top +
                element.scrollTop - element.clientHeight * 0.42 + target.offsetHeight / 2;
            const reduced = document.documentElement.dataset.hitMotion === "off" || matchMedia("(prefers-reduced-motion: reduce)").matches;
            element.scrollTo({top:Math.max(0,top), behavior:force || reduced ? "auto" : "smooth"});
        };
        view.center = center;
        view.observer = new ResizeObserver(() => center(true));
        view.observer.observe(element);
        element.addEventListener("wheel", view.wheel, {passive:true});
        enableLocalScroll(element);
        lyricViews.set(element, view);
    }
    view.index = index;
    view.center(instant);
}
export function disconnectLyrics(element) {
    const view = lyricViews.get(element);
    if (!view) return;
    clearTimeout(view.timer);
    view.observer.disconnect();
    element.removeEventListener("wheel",view.wheel);
    lyricViews.delete(element);
}

const scenes = new WeakMap();
const owners = new WeakMap();
const artVariables = ["--audio-art-background","--audio-art-surface","--audio-art-accent","--audio-art-foreground","--audio-art-muted","--audio-artwork"];
function releaseShell(root, scene) {
    const sphere = scene.sphere;
    if (sphere && owners.get(sphere) === root) {
        delete sphere.dataset.audioTheme;
        delete sphere.dataset.audioCover;
        for (const [name,previous] of scene.previous || []) {
            if (previous.value) sphere.style.setProperty(name,previous.value,previous.priority);
            else sphere.style.removeProperty(name);
        }
        owners.delete(sphere);
    }
    scene.sphere = null;
    scene.previous = null;
}
function rgb(values) { return "rgb(" + values.map(value=>Math.round(Math.max(0,Math.min(255,value)))).join(",") + ")"; }
function palette(image) {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 36;
    const context = canvas.getContext("2d",{willReadFrequently:true});
    if (!context) return null;
    context.drawImage(image,0,0,36,36);
    const data = context.getImageData(0,0,36,36).data;
    const buckets = new Map();
    for (let i=0;i<data.length;i+=4) {
        if (data[i+3] < 180) continue;
        const color = [data[i],data[i+1],data[i+2]];
        const key = color.map(channel=>Math.floor(channel/24)).join(":");
        let bucket = buckets.get(key);
        if (!bucket) { bucket={sum:[0,0,0],count:0}; buckets.set(key,bucket); }
        color.forEach((channel,index)=>bucket.sum[index]+=channel);
        bucket.count++;
    }
    const candidates = [...buckets.values()].map(bucket=>({color:bucket.sum.map(value=>value/bucket.count),count:bucket.count}));
    if (!candidates.length) return null;
    candidates.sort((left,right)=>right.count-left.count);
    const primary = candidates[0].color;
    const accentColor = [...candidates].sort((left,right)=>{
        const score = item => item.count * (0.2 + (Math.max(...item.color)-Math.min(...item.color))/255);
        return score(right)-score(left);
    })[0].color;
    const background = primary.map(channel=>channel*0.23+7);
    const accent = accentColor.map(channel=>channel*0.52+118);
    return {
        "--audio-art-background":rgb(background),
        "--audio-art-surface":rgb(primary.map(channel=>channel*0.18+15)),
        "--audio-art-accent":rgb(accent),
        "--audio-art-foreground":"rgb(247,249,252)",
        "--audio-art-muted":"rgb(197,205,216)"
    };
}
function loadCover(source) {
    return new Promise(resolve=>{
        const image = new Image();
        const timer = setTimeout(()=>{image.onload=image.onerror=null;resolve(null)},2000);
        image.onload=()=>{clearTimeout(timer);resolve(image)};
        image.onerror=()=>{clearTimeout(timer);resolve(null)};
        image.src=source;
    });
}
export async function syncScene(root, artwork, enabled, visible, lyrics, options = {}) {
    if (!root) return;
    let scene = scenes.get(root);
    if (!scene) {scene={revision:0,source:null,colors:null,sphere:null};scenes.set(root,scene);}
    if (options.reuseArtwork === true) artwork = scene.source;
    const revision=++scene.revision;
    const globalAccent=options.global === true;
    const blur=Number.isFinite(options.blur) ? Math.max(0,Math.min(40,options.blur)) : 8;
    root.style.setProperty("--audio-cover-blur",blur+"px");
    if(options.customizing && !scene.customizing) scene.customizingAt=performance.now();
    scene.customizing=options.customizing === true;
    const navigationSphere=root.closest(".dynamic-sphere");
    if(scene.backSphere) {
        scene.backSphere.removeEventListener("pointerup",scene.backHandler,true);
        scene.backSphere=null;
    }
    if(visible && (lyrics || options.customizing) && navigationSphere) {
        scene.backHandler=event=>{
            if(event.button!==2)return;
            // The shared title reserves M2 for the current menu's own settings.
            if(event.target.closest?.(".sphere-title, [data-hit-menu-trigger]"))return;
            event.preventDefault();
            event.stopPropagation();
            if(scene.customizing && performance.now()-scene.customizingAt<220)return;
            root.querySelector(".audio-view-back")?.click();
        };
        scene.backSphere=navigationSphere;
        navigationSphere.addEventListener("pointerup",scene.backHandler,true);
    }
    releaseShell(root,scene);
    const supported=typeof artwork==="string" && artwork.length<1500000 && /^data:image\/(?:png|jpe?g|webp|gif);base64,/i.test(artwork);
    if (!enabled || (!visible && !globalAccent) || !supported || !root.isConnected) {
        delete root.dataset.audioPalette;
        for(const name of artVariables)root.style.removeProperty(name);
        scene.source = scene.colors = null;
        return;
    }
    if(scene.source!==artwork || !scene.colors) {
        const image=await loadCover(artwork);
        if(revision!==scene.revision || !root.isConnected) { image?.removeAttribute("src"); return; }
        scene.source=artwork;
        try {scene.colors=image?palette(image):null}catch{scene.colors=null;}
        image?.removeAttribute("src");
    }
    if(revision!==scene.revision || !root.isConnected)return;
    if(!scene.colors) {
        delete root.dataset.audioPalette;
        for(const name of artVariables)root.style.removeProperty(name);
        return;
    }
    const sphere=root.closest(".dynamic-sphere");
    for(const [name,value] of Object.entries(scene.colors))root.style.setProperty(name,value);
    root.style.setProperty("--audio-artwork","url("+JSON.stringify(artwork)+")");
    root.dataset.audioPalette = "true";
    if(sphere) {
        scene.sphere=sphere;
        const shellValues={...scene.colors,"--audio-artwork":"url("+JSON.stringify(artwork)+")","--audio-cover-blur":blur+"px"};
        scene.previous=Object.keys(shellValues).map(name=>[name,{value:sphere.style.getPropertyValue(name),priority:sphere.style.getPropertyPriority(name)}]);
        for(const [name,value]of Object.entries(shellValues))sphere.style.setProperty(name,value);
        sphere.dataset.audioTheme = globalAccent ? "global" : "local";
        if(globalAccent && options.cover !== false && (!visible || !lyrics)) sphere.dataset.audioCover="true";
        owners.set(sphere,root);
    }
}
export function clearScene(root) {
    const scene=scenes.get(root);
    if(!scene)return;
    scene.revision++;
    scene.backSphere?.removeEventListener("pointerup",scene.backHandler,true);
    releaseShell(root,scene);
    delete root.dataset.audioPalette;
    for(const name of artVariables)root.style.removeProperty(name);
    root.style.removeProperty("--audio-cover-blur");
    scenes.delete(root);
}
