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
const audioKeys = new WeakMap();
export function connectAudioKeys(root, reference) {
    if (!root || audioKeys.has(root)) return;
    const keydown = event => {
        if (event.code !== 'Space' && event.key !== ' ') return;
        if (event.defaultPrevented || event.ctrlKey || event.altKey || event.metaKey || event.shiftKey || event.isComposing) return;
        const sphere = root.closest('.dynamic-sphere');
        if (!root.isConnected || !root.getClientRects().length || !sphere?.classList.contains('is-open') ||
            root.dataset.audioCanToggle !== 'true' || root.closest('[hidden],[inert],[data-hit-resource-sleep="true"]') ||
            sphere.querySelector('dialog[open]') || document.querySelector('[data-hit-select-open="true"]')) return;
        if (event.target instanceof Element && event.target.closest('input,textarea,select,button,a[href],[contenteditable]:not([contenteditable="false"]),[role="textbox"],[role="combobox"],[role="slider"],[role="radio"],[role="button"],[role="listbox"]')) return;
        event.preventDefault();
        if (!event.repeat) reference.invokeMethodAsync('TogglePlaybackFromKeyboard').catch(error => console.error('HIT playback shortcut failed.', error));
    };
    document.addEventListener('keydown', keydown, true);
    audioKeys.set(root, keydown);
}
export function disconnectAudioKeys(root) {
    const keydown = audioKeys.get(root);
    if (keydown) document.removeEventListener('keydown', keydown, true);
    audioKeys.delete(root);
}
export function enableLocalScroll(element) {
    if (!element || scrolling.has(element)) return;
    // Native scrolling stays inside the bounded view, outside Blazor's menu wheel handler.
    element.addEventListener("wheel", event => { if(consumesWheel(element,event))event.stopPropagation(); }, {passive:true});
    scrolling.add(element);
}

const lyricViews = new WeakMap();
export function followLyrics(element, index, instant = false, playback = null) {
    if (!element?.isConnected) return;
    let view = lyricViews.get(element);
    if (!view) {
        view = {index, lastManual:0, timer:0, observer:null, animations:[], position:0, playing:false, sampledAt:performance.now()};
        view.wheel = event => {
            if (consumesWheel(element,event)) event.stopPropagation();
            if (element.dataset.lyricsLayout === 'left' || element.dataset.lyricsLayout === 'right') {
                event.stopPropagation();
                event.preventDefault();
                element.scrollLeft += event.deltaX || event.deltaY;
            }
            view.lastManual = performance.now();
            clearTimeout(view.timer);
            view.timer = setTimeout(() => center(false), 1800);
        };
        const center = (force) => {
            const size = element.clientHeight + 'px';
            if (element.style.getPropertyValue('--lyrics-viewport-size') !== size) element.style.setProperty('--lyrics-viewport-size',size);
            const width = element.clientWidth + 'px';
            if (element.style.getPropertyValue('--lyrics-viewport-width') !== width) element.style.setProperty('--lyrics-viewport-width',width);
            if (!element.isConnected || (!force && performance.now() - view.lastManual < 1600)) return;
            const target = element.querySelector('[data-line-index="' + Math.max(0, view.index) + '"]');
            if (!target || !target.offsetHeight) return;
            const scaleY = element.getBoundingClientRect().height / element.offsetHeight || 1;
            const top = (target.getBoundingClientRect().top - element.getBoundingClientRect().top) / scaleY +
                element.scrollTop - element.clientHeight * 0.42 + target.offsetHeight / 2;
            const reduced = document.documentElement.dataset.hitMotion === "off" || matchMedia("(prefers-reduced-motion: reduce)").matches;
            const horizontal = element.dataset.lyricsLayout === 'left' || element.dataset.lyricsLayout === 'right';
            const scaleX = element.getBoundingClientRect().width / element.offsetWidth || 1;
            const left = (target.getBoundingClientRect().left - element.getBoundingClientRect().left) / scaleX +
                element.scrollLeft - element.clientWidth / 2 + target.offsetWidth / 2;
            element.scrollTo({top:horizontal ? 0 : Math.max(0,top), left:horizontal ? Math.max(0,left) : 0, behavior:force || reduced ? "auto" : "smooth"});
        };
        view.center = center;
        view.observer = new ResizeObserver(() => {
            cancelAnimationFrame(view.resizeFrame);
            view.resizeFrame = requestAnimationFrame(() => center(true));
        });
        view.observer.observe(element);
        element.addEventListener("wheel", view.wheel, {passive:false});
        view.motion = matchMedia('(prefers-reduced-motion: reduce)');
        view.environment = () => {
            const position = view.position + (view.playing ? (performance.now()-view.sampledAt)/1000 : 0);
            syncLyricsPlayback(element, position, view.playing);
            if (document.documentElement.dataset.hitMotion === 'off' || view.motion.matches) view.center(true);
        };
        view.motion.addEventListener('change', view.environment);
        document.addEventListener('visibilitychange', view.environment);
        view.environmentObserver = new MutationObserver(view.environment);
        view.environmentObserver.observe(document.documentElement, {attributes:true, attributeFilter:['data-hit-motion']});
        const area = element.closest('.sphere-area');
        if (area) view.environmentObserver.observe(area, {attributes:true, attributeFilter:['data-hit-resource-sleep']});
        lyricViews.set(element, view);
    }
    view.index = index;
    if (instant) {
        view.animations.forEach(animation => animation.cancel());
        view.animations = [];
        view.wordLine = null;
    }
    view.center(instant);
    if (playback) syncLyricsPlayback(element, playback.position, playback.playing);
}

export function syncLyricsPlayback(element, position, playing) {
    const view = lyricViews.get(element);
    if (!view) return;
    view.position = Number.isFinite(position) ? position : 0;
    view.playing = !!playing;
    view.sampledAt = performance.now();
    const reduced = document.documentElement.dataset.hitMotion === 'off' || view.motion.matches;
    const line = element.dataset.lyricsLayout === 'wordrise' && !reduced ? element.querySelector('.lyric-line.is-current') : null;
    if (line !== view.wordLine) {
        view.animations.forEach(animation => animation.cancel());
        view.animations = [];
        view.wordLine = line;
        const budget = line ? (Number(line.dataset.lineEnd)-Number(line.dataset.lineStart))*1000*2/3 : 0;
        if (budget > 0 && Number.isFinite(budget)) {
            const words = [...line.querySelectorAll('.lyric-word')];
            // Normalize the latest ending word, including long words followed by shorter ones.
            let startWeight = 0, overlappingWeight = 0;
            for (const word of words) {
                overlappingWeight = Math.max(overlappingWeight, startWeight + word.textContent.length);
                startWeight += word.textContent.length / 2;
            }
            let delay = 0;
            view.animations = words.map(word => {
                const duration = budget * word.textContent.length / overlappingWeight;
                const animation = word.animate([{transform:'translateY(.4em)',opacity:.25},{transform:'translateY(0)',opacity:1}],
                    {duration,delay,fill:'both',easing:'cubic-bezier(.45,0,.55,1)'});
                animation.pause(); delay += duration / 2;
                return animation;
            });
        }
    }
    const elapsed = Math.max(0, (view.position - Number(line?.dataset.lineStart || 0))*1000);
    const active = playing && !document.hidden && !element.closest('[data-hit-resource-sleep="true"]') && element.isConnected;
    for (const animation of view.animations) {
        if (Math.abs((animation.currentTime ?? -1000)-elapsed)>100 || !active) animation.currentTime = elapsed;
        const timing = animation.effect.getTiming();
        if (active && elapsed < timing.delay + timing.duration) animation.play(); else animation.pause();
    }
}
export function disconnectLyrics(element) {
    const view = lyricViews.get(element);
    if (!view) return;
    clearTimeout(view.timer);
    cancelAnimationFrame(view.resizeFrame);
    view.observer.disconnect();
    view.animations.forEach(animation => animation.cancel());
    view.environmentObserver.disconnect();
    view.motion.removeEventListener('change', view.environment);
    document.removeEventListener('visibilitychange', view.environment);
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
