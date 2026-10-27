// Real output-device FFT drives the envelope. A steady tone is not a beat.
export class BeatEnvelope {
    average=0; previous=0; lastBeat=-Infinity; initialized=false;
    feed(values,now) {
        let low=0;for(let index=0;index<8;index++)low+=Math.max(0,Math.min(1,Number(values[index])||0));low/=8;
        if(!this.initialized){this.average=low;this.previous=low;this.initialized=true;return;}
        if(low>.065&&low-this.previous>.035&&low>this.average*1.22&&now-this.lastBeat>190)this.lastBeat=now;
        this.average+=(low-this.average)*.1;this.previous=low;
    }
    value(now) {
        const age=now-this.lastBeat;
        if(age<0||age>760)return 0;
        return age<85?age/85:Math.exp(-(age-85)/180)*Math.max(0,(760-age)/675);
    }
}

const engines=new WeakMap();let sequence=0;
const clamp=(value,min,max,fallback=0)=>Number.isFinite(Number(value))?Math.max(min,Math.min(max,Number(value))):fallback;
function style(node,name,value){if(node&&node.style[name]!==value)node.style[name]=value;}
function attribute(node,name,value){if(node&&node.getAttribute(name)!==value)node.setAttribute(name,value);}
function layer(parent,className){const node=document.createElement('div');node.className=className;parent.append(node);return node;}
function optional(engine,key,className,parent,enabled){
    if(enabled){if(!engine[key])engine[key]=layer(parent,className);}
    else {engine[key]?.remove();engine[key]=null;}
}
function visible(engine){return engine.sphere.isConnected&&engine.sphere.classList.contains('is-open')&&
    engine.sphere.dataset.audioCover==='true'&&engine.sphere.dataset.audioReadingHidden!=='true'&&
    engine.sphere.dataset.audioLyricsNeutral!=='true'&&!document.hidden;}

function configureLayers(engine) {
    const {options:o,optics,backdrop}=engine;
    const count=o.radial&&o.blur>0?7:1;
    while(engine.copies.length>count)engine.copies.pop().remove();
    while(engine.copies.length<count){const copy=layer(optics,'audio-artwork-copy');copy.style.opacity=String(1/(engine.copies.length+1));engine.copies.push(copy);}
    engine.copies.forEach((node,index)=>style(node,'transform',`scale(${1+(o.radial?index*o.blur/1000:0)})`));
    optional(engine,'lens','audio-artwork-lens',optics,o.lens>0);
    optional(engine,'red','audio-artwork-chromatic red',optics,o.chromatic>0);
    optional(engine,'blue','audio-artwork-chromatic blue',optics,o.chromatic>0);
    // Radial copies are behind the optical overlays even when enabled later.
    for(const node of [engine.lens,engine.red,engine.blue])if(node)optics.append(node);
    optional(engine,'vignette','audio-artwork-vignette',backdrop,o.vignette>0);
    optional(engine,'grain','audio-artwork-grain',backdrop,o.grain>0);

    const distort=o.distortion>0,balance=o.colorEnabled&&(o.temperature!==0||o.tint!==0);
    const key=distort+':'+balance;
    if(engine.graph===key)return;
    engine.graph=key;engine.svg?.remove();engine.svg=engine.displacement=engine.color=null;
    if(!distort&&!balance)return;
    const svg=document.createElementNS('http://www.w3.org/2000/svg','svg');
    svg.setAttribute('width','0');svg.setAttribute('height','0');svg.setAttribute('aria-hidden','true');
    svg.style.cssText='position:absolute;pointer-events:none';
    // Color balance must never run turbulence/displacement when distortion is off.
    svg.innerHTML='<defs>'+(distort?`<filter id="${engine.id}-distort" x="-15%" y="-15%" width="130%" height="130%"><feTurbulence type="fractalNoise" baseFrequency="0.012" numOctaves="2" seed="7" result="noise"/><feDisplacementMap in="SourceGraphic" in2="noise" scale="0" xChannelSelector="R" yChannelSelector="G"/></filter>`:'')+
        (balance?`<filter id="${engine.id}-color" color-interpolation-filters="sRGB"><feColorMatrix type="matrix"/></filter>`:'')+'</defs>';
    document.body.append(svg);engine.svg=svg;engine.displacement=svg.querySelector('feDisplacementMap');engine.color=svg.querySelector('feColorMatrix');
}

export function configureEffects(sphere,options) {
    const backdrop=sphere?.querySelector(':scope > .sphere-audio-backdrop');if(!backdrop)return;
    let engine=engines.get(sphere);
    if(!engine){
        engine={sphere,backdrop,optics:layer(backdrop,'audio-artwork-optics'),copies:[],id:'hit-art-'+(++sequence),
            beat:new BeatEnvelope(),frame:0,driftAnimation:null,active:false};
        engine.motionQuery=matchMedia('(prefers-reduced-motion: reduce)');
        engine.changed=()=>draw(engine,performance.now());
        engine.observer=new MutationObserver(engine.changed);
        engine.observer.observe(sphere,{attributes:true,attributeFilter:['class','data-audio-cover','data-audio-reading-hidden','data-audio-lyrics-neutral']});
        engine.motionObserver=new MutationObserver(engine.changed);
        engine.motionObserver.observe(document.documentElement,{attributes:true,attributeFilter:['data-hit-motion']});
        engine.motionQuery.addEventListener('change',engine.changed);document.addEventListener('visibilitychange',engine.changed);
        engines.set(sphere,engine);
    }
    engine.driftAnimation?.cancel();engine.driftAnimation=null;
    engine.options={blur:clamp(options.blur,0,40,8),zoom:clamp(options.zoom,100,180,100),radial:options.radial===true,
        lens:clamp(options.lens,0,100),chromatic:clamp(options.chromatic,0,20),vignette:clamp(options.vignette,0,100),grain:clamp(options.grain,0,30),
        distortion:clamp(options.distortion,0,30),colorEnabled:options.colorEnabled===true,saturation:clamp(options.saturation,0,200,80),
        brightness:clamp(options.brightness,20,200,100),contrast:clamp(options.contrast,25,200,100),temperature:clamp(options.temperature,-100,100),
        tint:clamp(options.tint,-100,100),hue:clamp(options.hue,-180,180),sepia:clamp(options.sepia,0,100),grayscale:clamp(options.grayscale,0,100),
        onBeat:options.onBeat===true,strength:clamp(options.strength,0,200,100),drift:options.drift===true};
    configureLayers(engine);engine.factor=null;engine.filterAt=-Infinity;sphere.dataset.audioEffects='true';draw(engine,performance.now());
}

function paint(engine,factor) {
    const {options:o,optics}=engine,filters=[];
    const distortion=o.distortion*factor;attribute(engine.displacement,'scale',distortion.toFixed(2));
    if(distortion>0)filters.push(`url("#${engine.id}-distort")`);
    if(!o.radial&&o.blur>0)filters.push(`blur(${o.blur}px)`);
    if(o.colorEnabled){
        const temperature=o.temperature*factor/100,tint=o.tint*factor/100;
        if(engine.color)attribute(engine.color,'values',`${1+temperature*.22+tint*.08} 0 0 0 0 0 ${1-tint*.16} 0 0 0 0 0 ${1-temperature*.22+tint*.08} 0 0 0 0 0 1 0`);
        if(temperature||tint)filters.push(`url("#${engine.id}-color")`);
        if(o.saturation!==100&&factor)filters.push(`saturate(${1+(o.saturation/100-1)*factor})`);
        if(o.brightness!==100&&factor)filters.push(`brightness(${1+(o.brightness/100-1)*factor})`);
        if(o.contrast!==100&&factor)filters.push(`contrast(${1+(o.contrast/100-1)*factor})`);
        if(o.hue&&factor)filters.push(`hue-rotate(${o.hue*factor}deg)`);
        if(o.sepia&&factor)filters.push(`sepia(${o.sepia*factor/100})`);
        if(o.grayscale&&factor)filters.push(`grayscale(${o.grayscale*factor/100})`);
    }
    style(optics,'filter',filters.join(' ')||'none');
    if(engine.lens){const amount=o.lens*factor;style(engine.lens,'display',amount?'block':'none');style(engine.lens,'transform',`scale(${1+amount/200})`);}
    if(engine.red){const shift=o.chromatic*factor;for(const node of [engine.red,engine.blue]){style(node,'display',shift?'block':'none');style(node,'opacity','0.24');}
        style(engine.red,'transform',`translateX(${shift}px)`);style(engine.blue,'transform',`translateX(${-shift}px)`);}
    for(const [node,amount]of [[engine.vignette,o.vignette],[engine.grain,o.grain]])if(node){style(node,'display',factor?'block':'none');style(node,'opacity',String(amount*factor/100));}
}
function requestDraw(engine){if(!engine.frame)engine.frame=requestAnimationFrame(time=>{engine.frame=0;draw(engine,time);});}
function draw(engine,now) {
    const {options:o,sphere,backdrop}=engine;if(!o)return;
    engine.active=visible(engine);
    const reduced=document.documentElement.dataset.hitMotion==='off'||engine.motionQuery.matches;
    const envelope=engine.active&&!reduced?engine.beat.value(now):0;
    const factor=o.onBeat?Math.round(Math.min(1,envelope*o.strength/100)*100)/100:1;
    if(engine.factor!==factor&&(factor===0||engine.factor===null||now-engine.filterAt>=32)){
        engine.factor=factor;engine.filterAt=now;paint(engine,factor);
    }
    const drift=engine.active&&!reduced&&o.drift,animatedBeat=o.onBeat&&engine.active&&!reduced&&now-engine.beat.lastBeat<780;
    if(drift&&!o.onBeat){
        if(!engine.driftAnimation){const zoom=o.zoom/100;
            engine.driftAnimation=backdrop.animate([{transform:`translate(-1.5%,-1%) scale(${zoom*1.06})`},{transform:`translate(1.5%,1%) scale(${zoom*1.1})`}],
                {duration:14000,iterations:Infinity,direction:'alternate',easing:'ease-in-out'});}
    }else if(engine.driftAnimation){engine.driftAnimation.cancel();engine.driftAnimation=null;}
    if(!engine.driftAnimation){
        const shake=animatedBeat&&o.radial?Math.sin((now-engine.beat.lastBeat)/32)*factor*5:0;
        const x=drift?Math.sin(now/3200)*factor*1.5:0,y=drift?Math.cos(now/4100)*factor:0;
        style(backdrop,'transform',`translate(${x.toFixed(3)}%,${y.toFixed(3)}%) translateX(${shake.toFixed(2)}px) scale(${(1+(o.zoom/100-1)*factor+(drift?factor*.06:0)).toFixed(4)})`);
    }
    if(animatedBeat)requestDraw(engine);
    else if(engine.frame){cancelAnimationFrame(engine.frame);engine.frame=0;}
}
export function renderAudioEffects(sphere,levels,now=performance.now()){
    const engine=engines.get(sphere);if(!engine?.options.onBeat||!engine.active||!Array.isArray(levels))return;
    engine.beat.feed(levels,now);requestDraw(engine);
}
export function disposeEffects(sphere){
    const engine=engines.get(sphere);if(!engine)return;
    cancelAnimationFrame(engine.frame);engine.driftAnimation?.cancel();engine.observer.disconnect();engine.motionObserver.disconnect();
    engine.motionQuery.removeEventListener('change',engine.changed);document.removeEventListener('visibilitychange',engine.changed);
    engine.svg?.remove();engine.optics.remove();engine.vignette?.remove();engine.grain?.remove();engine.backdrop.style.removeProperty('transform');
    delete sphere.dataset.audioEffects;engines.delete(sphere);
}
