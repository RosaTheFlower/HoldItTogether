import {configureEffects, renderAudioEffects, disposeEffects} from './audio-effects.js';
import {renderSpectrum,refreshSpectrum,disconnectSpectrum} from './audio-spectrum.js';
import {consumesWheel} from './hit-wheel-routing.js';
export {renderSpectrum,disconnectSpectrum};
export {configureSpectrum} from './audio-spectrum.js';
const spectra=new WeakMap();
export function syncSpectrum(root){
    const current=root?.querySelector('.audio-spectrum'),previous=spectra.get(root);
    if(previous&&previous!==current)disconnectSpectrum(previous);
    if(current)spectra.set(root,current);else if(root)spectra.delete(root);
}
// Data comes from the native output-device FFT, never from random/decorative animation.
export function renderAudioFrame(root, element, values) {
    if(root&&element?.isConnected)spectra.set(root,element);
    renderSpectrum(element, values);
    renderAudioEffects(root?.closest('.dynamic-sphere'), values);
}
const scrolling = new WeakSet();
export function enableLocalScroll(element) {
    if (!element || scrolling.has(element)) return;
    // Native scrolling stays inside the bounded view, outside Blazor's menu wheel handler.
    element.addEventListener("wheel", event => { if(consumesWheel(element,event))event.stopPropagation(); }, {passive:true});
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
            const size = element.clientHeight + 'px';
            if (element.style.getPropertyValue('--lyrics-viewport-size') !== size) element.style.setProperty('--lyrics-viewport-size',size);
            if (!element.isConnected || (!force && performance.now() - view.lastManual < 1600)) return;
            const target = element.querySelector('[data-line-index="' + Math.max(0, view.index) + '"]');
            if (!target) return;
            const scaleY = element.getBoundingClientRect().height / element.offsetHeight || 1;
            const top = (target.getBoundingClientRect().top - element.getBoundingClientRect().top) / scaleY +
                element.scrollTop - element.clientHeight * 0.42 + target.offsetHeight / 2;
            const reduced = document.documentElement.dataset.hitMotion === "off" || matchMedia("(prefers-reduced-motion: reduce)").matches;
            element.scrollTo({top:Math.max(0,top), behavior:force || reduced ? "auto" : "smooth"});
        };
        view.center = center;
        view.observer = new ResizeObserver(() => {
            cancelAnimationFrame(view.resizeFrame);
            view.resizeFrame = requestAnimationFrame(() => center(true));
        });
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
    cancelAnimationFrame(view.resizeFrame);
    view.observer.disconnect();
    element.removeEventListener("wheel",view.wheel);
    lyricViews.delete(element);
}

const scenes = new WeakMap();
const owners = new WeakMap();
const artVariables = ["--audio-art-background","--audio-art-surface","--audio-art-accent","--audio-art-foreground","--audio-art-muted","--audio-artwork"];
async function fadeCover(scene, reveal) {
    const backdrop=scene.sphere?.querySelector(':scope > .sphere-audio-backdrop');
    if(!backdrop)return;
    if(reveal)delete scene.sphere.dataset.audioCoverHiding;else scene.sphere.dataset.audioCoverHiding='true';
    const current=Number(getComputedStyle(backdrop).opacity);
    scene.coverAnimation?.cancel();scene.coverAnimation=null;
    if(matchMedia('(prefers-reduced-motion: reduce)').matches ||
        document.getElementById('hit-motion-preference')?.textContent.trim())return;
    if(getComputedStyle(backdrop).display==='none')return;
    const target=Number(getComputedStyle(backdrop).opacity);
    const animation=backdrop.animate([{opacity:reveal?0:current},{opacity:reveal?target:0}],
        {duration:650,easing:'ease-in-out'});
    scene.coverAnimation=animation;
    try{await animation.finished;}catch{} // Rapid reopen replaces the outgoing transition.
    if(scene.coverAnimation===animation)scene.coverAnimation=null;
}
function releaseShell(root, scene) {
    const sphere = scene.sphere;
    if (sphere && owners.get(sphere) === root) {
        delete sphere.dataset.audioTheme;
        delete sphere.dataset.audioCover;
        delete sphere.dataset.audioBackgroundBlend;
        delete sphere.dataset.audioCoverHiding;
        delete sphere.dataset.audioLyricsNeutral;
        delete sphere.dataset.audioDrift;
        for (const [name,previous] of scene.previous || []) {
            if (previous.value) sphere.style.setProperty(name,previous.value,previous.priority);
            else sphere.style.removeProperty(name);
        }
        owners.delete(sphere);
    }
    scene.sphere = null;
    scene.previous = null;
    document.dispatchEvent(new CustomEvent('hit-appearance-changed'));
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
    const revealCover=!scene.source || !!scene.clearing || !scene.sphere?.hasAttribute('data-audio-cover');
    scene.clearing=null;
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
    if(visible && lyrics && !options.customizing && navigationSphere) {
        scene.backHandler=event=>{
            if(event.button!==2)return;
            // The shared title reserves M2 for the current menu's own settings.
            if(event.target.closest?.(".sphere-title, [data-hit-menu-trigger]"))return;
            const back = navigationSphere.querySelector('.sphere-action-back') || root.querySelector('.audio-view-back');
            if (!back) return;
            event.preventDefault();
            event.stopPropagation();
            if(scene.customizing && performance.now()-scene.customizingAt<220)return;
            back.click();
        };
        scene.backSphere=navigationSphere;
        navigationSphere.addEventListener("pointerup",scene.backHandler,true);
    }
    const neutral = visible && lyrics && options.neutralLyrics === true;
    const retainNeutral = () => {
        if (!navigationSphere || !neutral) return;
        scene.sphere = navigationSphere;
        scene.previous = [];
        navigationSphere.dataset.audioLyricsNeutral = 'true';
        owners.set(navigationSphere, root);
    };
    const supported=typeof artwork==="string" && artwork.length<1500000 && /^data:image\/(?:png|jpe?g|webp|gif);base64,/i.test(artwork);
    if (!enabled || (!visible && !globalAccent) || !supported || !root.isConnected) {
        await fadeCover(scene,false);
        if(revision!==scene.revision)return;
        disposeEffects(scene.sphere);
        releaseShell(root,scene);
        retainNeutral();
        delete root.dataset.audioPalette;
        for(const name of artVariables)root.style.removeProperty(name);
        scene.source = scene.colors = null;
        refreshSpectrum(root.querySelector('.audio-spectrum'));
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
        disposeEffects(scene.sphere);
        releaseShell(root,scene);
        retainNeutral();
        delete root.dataset.audioPalette;
        for(const name of artVariables)root.style.removeProperty(name);
        return;
    }
    // Keep the previous opaque surface/cover while decoding the next image.
    // Commit its replacement synchronously, after all asynchronous work finishes.
    releaseShell(root,scene);
    const sphere=root.closest(".dynamic-sphere");
    for(const [name,value] of Object.entries(scene.colors))root.style.setProperty(name,value);
    root.style.setProperty("--audio-artwork","url("+JSON.stringify(artwork)+")");
    root.dataset.audioPalette = "true";
    if(sphere) {
        scene.sphere=sphere;
        const zoom = Math.max(100,Math.min(180,Number(options.zoom)||100))/100;
        const saturation = (Number.isFinite(options.saturation) ? Math.max(0,Math.min(200,options.saturation)) : 80)/100;
        const distortion = Math.max(0,Math.min(30,Number(options.distortion)||0));
        const shellValues={...scene.colors,"--audio-cover-opacity":String(Number.isFinite(options.backgroundOpacity)?Math.max(0,Math.min(100,options.backgroundOpacity))/100:.32),"--audio-artwork":"url("+JSON.stringify(artwork)+")","--audio-cover-blur":blur+"px",'--audio-cover-zoom':String(zoom),'--audio-cover-saturation':String(saturation)};
        scene.previous=Object.keys(shellValues).map(name=>[name,{value:sphere.style.getPropertyValue(name),priority:sphere.style.getPropertyPriority(name)}]);
        for(const [name,value]of Object.entries(shellValues))sphere.style.setProperty(name,value);
        sphere.dataset.audioTheme = globalAccent ? "global" : "local";
        sphere.dataset.audioBackgroundBlend=options.blendBackground===true?"true":"false";
        if (!neutral && (visible ? !lyrics || options.lyricsCover === true : globalAccent && options.cover !== false)) sphere.dataset.audioCover="true";
        if (neutral) sphere.dataset.audioLyricsNeutral='true';
        if (options.drift === true) sphere.dataset.audioDrift='true';
        owners.set(sphere,root);
        configureEffects(sphere,{blur,zoom:zoom*100,saturation:saturation*100,distortion,drift:options.drift === true,...options.effects});
        if(revealCover)void fadeCover(scene,true);
        refreshSpectrum(root.querySelector('.audio-spectrum'));
        document.dispatchEvent(new CustomEvent('hit-appearance-changed'));
    }
}
export function clearScene(root, immediate=false) {
    disconnectSpectrum(spectra.get(root));spectra.delete(root);
    disconnectSpectrum(root?.querySelector('.audio-spectrum'));
    const scene=scenes.get(root);
    if(!scene)return Promise.resolve();
    if(scene.clearing&&!immediate)return scene.clearing;
    const revision=++scene.revision;
    scene.backSphere?.removeEventListener("pointerup",scene.backHandler,true);
    clearLyricsTimeline(root);
    if(immediate){scene.coverAnimation?.cancel();scene.coverAnimation=null;}
    scene.clearing=(immediate?Promise.resolve():fadeCover(scene,false)).then(()=>{
        if(scene.revision!==revision)return;
        disposeEffects(scene.sphere);
        releaseShell(root,scene);
        clearLyricsTimeline(root);
        delete root.dataset.audioPalette;
        for(const name of artVariables)root.style.removeProperty(name);
        root.style.removeProperty("--audio-cover-blur");
        scenes.delete(root);
    });
    return scene.clearing;
}

const timelines = new WeakMap();
export function syncLyricsTimeline(root, reference, options) {
    const sphere = root?.closest('.dynamic-sphere');
    if (!sphere || !options.enabled || !root.isConnected) { clearLyricsTimeline(root); return; }
    let timeline = timelines.get(root);
    if (!timeline) {
        const wrapper = document.createElement('div'); wrapper.className='sphere-lyrics-timeline'; wrapper.dataset.noDrag='';
        const input = document.createElement('input'); input.type='range'; input.min='0'; input.step='0.1'; input.setAttribute('aria-label','Lyrics playback position');
        wrapper.append(input); sphere.append(wrapper);
        const controller = new AbortController();
        timeline = {wrapper,input,controller,reference,options,dragging:false}; timelines.set(root,timeline);
        const begin = () => { if (!timeline.dragging) { timeline.dragging=true; timeline.generation=timeline.options.generation; } };
        input.addEventListener('pointerdown',begin,{signal:controller.signal});
        input.addEventListener('keydown',begin,{signal:controller.signal});
        input.addEventListener('input',()=> { begin(); drawTimeline(timeline,Number(input.value)); },{signal:controller.signal});
        input.addEventListener('change',async()=> {
            const generation=timeline.generation ?? timeline.options.generation, position=Number(input.value);
            try { await timeline.reference.invokeMethodAsync('SeekFromTimeline',generation,position); }
            catch(error) { console.error('HIT lyrics seek failed',error); }
            finally { timeline.dragging=false; }
        },{signal:controller.signal});
        input.addEventListener('pointercancel',()=>{timeline.dragging=false;},{signal:controller.signal});
        input.addEventListener('blur',()=>{timeline.dragging=false;},{signal:controller.signal});
        wrapper.addEventListener('wheel',event=>event.stopPropagation(),{passive:true,signal:controller.signal});
        wrapper.addEventListener('contextmenu',event=>{event.preventDefault();event.stopPropagation();},{signal:controller.signal});
    }
    timeline.reference=reference; timeline.options=options;
    timeline.wrapper.style.setProperty('--lyrics-timeline-thickness',Math.max(1,Math.min(6,options.thickness))+'px');
    timeline.input.max=String(Math.max(1,options.duration));
    timeline.input.disabled=!options.canSeek || options.busy || options.duration<=0;
    if (!timeline.dragging) { timeline.input.value=String(options.position); drawTimeline(timeline,options.position); }
}
function drawTimeline(timeline,position) {
    const progress=timeline.options.duration>0 ? Math.max(0,Math.min(100,position/timeline.options.duration*100)):0;
    timeline.wrapper.style.setProperty('--lyrics-timeline-progress',progress+'%');
    timeline.input.setAttribute('aria-valuetext',Math.floor(position)+' of '+Math.floor(timeline.options.duration)+' seconds');
}
export function clearLyricsTimeline(root) {
    const timeline=timelines.get(root); if(!timeline)return;
    timeline.controller.abort(); timeline.wrapper.remove(); timelines.delete(root);
}
