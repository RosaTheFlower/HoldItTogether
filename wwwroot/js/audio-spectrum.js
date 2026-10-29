// One full-DPI drawing surface replaces 32 animated DOM layers. Input remains the native 32-band FFT.
const renderers=new WeakMap();
// Scalar preferences survive buffer release on scene/sleep transitions without retaining a renderer.
const preferences=new WeakMap();
const drawers=Object.freeze({bars:drawBars,line:drawLine,smooth:drawSmooth,mirrored:drawMirrored,radial:drawRadial,ring:drawRing,
    wave:drawWave,wavesoft:drawWave,waveribbons:drawWave,wavepulse:drawWave});
const radialX=Float32Array.from({length:32},(_,i)=>Math.cos(i*Math.PI/16-Math.PI/2));
const radialY=Float32Array.from({length:32},(_,i)=>Math.sin(i*Math.PI/16-Math.PI/2));
export function renderSpectrum(element,values){
    if(!element?.isConnected||!Array.isArray(values))return;
    let view=renderers.get(element);
    if(!view){
        const canvas=element.querySelector('canvas'),context=canvas?.getContext('2d');if(!context)return;
        const texture=document.createElement('canvas'),brush=texture.getContext('2d');if(!brush)return;
        const options=preferences.get(element);
        applyOpacity(element,options?.opacity??.55);
        view={canvas,context,texture,brush,current:new Float32Array(32),target:new Float32Array(32),frame:0,width:0,height:0,last:0,
            amplitude:options?.amplitude??1,size:options?.size??1,positionY:options?.positionY??0,flipX:options?.flipX??false,flipY:options?.flipY??false,style:options?.style??'bars',phase:0,draw:drawers[options?.style]??drawBars};
        view.measure=()=>{
            if(view.disposed)return;
            const rect=canvas.getBoundingClientRect(),dpi=devicePixelRatio||1,scale=dpi*rect.width/(canvas.offsetWidth||rect.width||1);
            if(rect.width<=0||rect.height<=0)return;
            view.width=Math.max(0,Math.ceil(rect.width*dpi));view.height=Math.max(0,Math.ceil(rect.height*dpi));
            if(canvas.width!==view.width)canvas.width=view.width;if(canvas.height!==view.height)canvas.height=view.height;
            view.gap=3*scale;view.barWidth=Math.max(1,(view.width-view.gap*31)/32);
            texture.width=Math.ceil(view.barWidth);texture.height=Math.max(1,view.height);
            const gradient=brush.createLinearGradient(0,texture.height,0,0),color=getComputedStyle(canvas).color;
            view.color=color;view.lineWidth=2*scale;
            gradient.addColorStop(0,'transparent');gradient.addColorStop(1,color);
            brush.clearRect(0,0,texture.width,texture.height);brush.fillStyle=gradient;
            brush.beginPath();brush.roundRect(0,0,texture.width,texture.height,[3*scale,3*scale,0,0]);brush.fill();
            // Resizing a canvas clears it. Repaint before this ResizeObserver callback returns,
            // so compact/expand never presents an empty buffer while waiting for the next RAF.
            paint(view);
        };
        view.resize=new ResizeObserver(view.measure);view.resize.observe(canvas);
        view.refresh=()=>view.measure();document.addEventListener('hit-theme-status',view.refresh);
        window.addEventListener('resize',view.refresh);
        document.addEventListener('visibilitychange',view.refresh);
        renderers.set(element,view);view.measure();
    }
    for(let index=0;index<32;index++)view.target[index]=Math.max(0,Math.min(1,Number(values[index])||0));
    request(view);
}
function request(view){if(!view.disposed&&!view.frame)view.frame=requestAnimationFrame(now=>draw(view,now));}
function draw(view,now){
    view.frame=0;
    if(view.disposed||!view.canvas.isConnected||document.hidden||!view.width||!view.height)return;
    const reduced=document.documentElement.dataset.hitMotion==='off'||matchMedia('(prefers-reduced-motion: reduce)').matches;
    const smoothing=reduced?1:1-Math.exp(-Math.min(100,now-(view.last||now-16))/38);view.last=now;
    if(reduced)view.phase=0;
    else if(view.draw===drawWave)view.phase=(view.phase+.035)%(Math.PI*2);
    let moving=false;
    for(let index=0;index<32;index++){
        const delta=view.target[index]-view.current[index];
        if(Math.abs(delta)>.001){view.current[index]+=delta*smoothing;moving=true;}else view.current[index]=view.target[index];
    }
    paint(view);
    if(moving&&!reduced)request(view);
}
function paint(view){
    if(view.disposed||!view.canvas.isConnected||document.hidden||!view.width||!view.height)return;
    view.context.clearRect(0,0,view.width,view.height);view.context.save();
    const radial=view.draw===drawRadial||view.draw===drawRing;
    const centered=radial||view.draw===drawWave||view.draw===drawMirrored;
    const anchorY=centered?view.height/2:view.height;
    view.context.translate(0,view.height*view.positionY);
    if(view.flipX){view.context.translate(view.width,0);view.context.scale(-1,1);}
    if(view.flipY){view.context.translate(0,view.height);view.context.scale(1,-1);}
    const zoom=view.draw===drawWave?1:view.size;
    view.context.translate(view.width/2,anchorY);view.context.scale(zoom,zoom);view.context.translate(-view.width/2,-anchorY);
    view.draw(view);view.context.restore();
}
function levelHeight(view,index){return view.height*view.amplitude*(.025+view.current[index]*.975);}
function drawBars(view){
    for(let index=0;index<32;index++){
        const height=levelHeight(view,index),x=index*(view.barWidth+view.gap);
        view.context.drawImage(view.texture,x,view.height-height,view.barWidth,height);
    }
}
function drawLine(view){
    const context=view.context,inset=view.lineWidth/2;
    context.beginPath();context.strokeStyle=view.color;context.lineWidth=view.lineWidth;context.lineJoin='round';
    for(let index=0;index<32;index++){
        const x=inset+index*Math.max(0,view.width-2*inset)/31;
        const y=Math.max(inset,Math.min(view.height-inset,view.height-levelHeight(view,index)));
        if(index===0)context.moveTo(x,y);else context.lineTo(x,y);
    }
    context.stroke();
}
// Renderer options affect presentation only; every style consumes the same native FFT snapshot.
function traceSmooth(view,sign=1,centered=false){
    const context=view.context,inset=view.lineWidth/2,baseline=centered?view.height/2:view.height;
    const step=Math.max(0,view.width-2*inset)/31;
    let previousX=inset,previousY=baseline-sign*levelHeight(view,0)*(centered ? .5 : 1);
    previousY=Math.max(inset,Math.min(view.height-inset,previousY));
    context.moveTo(previousX,previousY);
    for(let index=1;index<32;index++){
        const x=inset+index*step;
        const y=Math.max(inset,Math.min(view.height-inset,baseline-sign*levelHeight(view,index)*(centered ? .5 : 1)));
        context.quadraticCurveTo(previousX,previousY,(previousX+x)/2,(previousY+y)/2);
        previousX=x;previousY=y;
    }
    context.lineTo(previousX,previousY);
}
function startStroke(view){
    const context=view.context;
    context.beginPath();context.strokeStyle=view.color;context.lineWidth=view.lineWidth;context.lineJoin='round';
    return context;
}
function drawSmooth(view){const context=startStroke(view);traceSmooth(view);context.stroke();}
function drawMirrored(view){
    const context=startStroke(view);traceSmooth(view,1,true);traceSmooth(view,-1,true);context.stroke();
}
function radialRadius(view,index){
    const radius=Math.max(0,(Math.min(view.width,view.height)-view.lineWidth)/2);
    return radius*(.18+.4*view.amplitude*(.025+view.current[index]*.975));
}
function drawRadial(view){
    const context=startStroke(view),x=view.width/2,y=view.height/2;
    const inner=Math.max(0,(Math.min(view.width,view.height)-view.lineWidth)/2)*.18;
    for(let index=0;index<32;index++){
        const outer=radialRadius(view,index);
        context.moveTo(x+radialX[index]*inner,y+radialY[index]*inner);
        context.lineTo(x+radialX[index]*outer,y+radialY[index]*outer);
    }
    context.stroke();
}
function drawRing(view){
    const context=startStroke(view),x=view.width/2,y=view.height/2;
    for(let index=0;index<32;index++){
        const radius=radialRadius(view,index),px=x+radialX[index]*radius,py=y+radialY[index]*radius;
        if(index===0)context.moveTo(px,py);else context.lineTo(px,py);
    }
    context.closePath();context.stroke();
}
function drawWave(view){
    const context=startStroke(view);
    let energy=0;
    for(let index=0;index<32;index++)energy+=view.current[index]*view.current[index];
    energy=Math.sqrt(energy/32);
    const soft=view.style==='wavesoft',ribbons=view.style==='waveribbons',pulse=view.style==='wavepulse';
    const layers=soft?1:pulse?2:3,frequency=soft?2.4:pulse?7:4,phase=view.phase*(soft ? .6 : pulse ? 1.6 : 1);
    // Every variant overscans the full drawing area, even at minimum size. The native surface
    // clips the ends; no waveform endpoint or fixed side gap appears during zoom/compaction.
    for(let layer=0;layer<layers;layer++){
        context.beginPath();context.globalAlpha=ribbons ? .55-layer*.12 : 1-layer*.25;
        context.lineWidth=view.lineWidth*(soft?1.6:1);
        const samples=ribbons?258:129;
        for(let point=0;point<samples;point++){
            const upper=point<=128,t=(upper?point:257-point)/128;
            const envelope=Math.sin(Math.PI*t)**2,position=t*31,index=Math.floor(position);
            const band=view.current[index]+(view.current[Math.min(31,index+1)]-view.current[index])*(position-index);
            const zoomedT=(t-.5)/view.size+.5;
            const carrier=Math.sin(zoomedT*Math.PI*frequency+phase+layer*.6)+(soft?0:.22)*Math.sin(zoomedT*Math.PI*8-phase);
            const thickness=ribbons?(upper?-1:1)*.065:0;
            const excursion=(carrier*.3*(1-layer*.18)+thickness)*envelope*view.amplitude*view.size*energy*(.65+.35*band);
            // Soft vertical fitting keeps the continuous curves visible at both edges even at
            // maximum zoom/height, instead of zooming the entire wave outside its fixed canvas.
            const y=view.height/2+view.height*.46*Math.tanh(excursion/.46);
            const x=(t*1.12-.06)*view.width;
            if(point===0)context.moveTo(x,y);else context.lineTo(x,y);
        }
        if(ribbons){context.closePath();context.fillStyle=view.color;context.fill();}else context.stroke();
    }
    context.globalAlpha=1;
}
function applyOpacity(element,opacity){
    const value=String(opacity);
    if(element.style.getPropertyValue('--audio-visualizer-opacity')!==value)element.style.setProperty('--audio-visualizer-opacity',value);
}
export function configureSpectrum(element,style='bars',heightPercent=100,sizePercent=100,positionYPercent=0,flipX=false,flipY=false,opacityPercent=55){
    if(!element?.isConnected)return;
    const height=Number(heightPercent);
    const size=Number(sizePercent);
    const positionY=Number(positionYPercent);
    const opacity=Number(opacityPercent);
    if(style==='filled')style='smooth'; // Same fallback as the retired persisted enum value.
    const options={style:typeof style==='string'&&Object.hasOwn(drawers,style)?style:'bars',amplitude:(Number.isFinite(height)?Math.max(10,Math.min(200,height)):100)/100,
        size:(Number.isFinite(size)?Math.max(100,Math.min(500,size)):100)/100,
        positionY:(Number.isFinite(positionY)?Math.max(-100,Math.min(100,positionY)):0)/100,flipX:flipX===true,flipY:flipY===true,
        opacity:(Number.isFinite(opacity)?Math.max(0,Math.min(100,opacity)):55)/100};
    preferences.set(element,options);
    applyOpacity(element,options.opacity);
    if(!renderers.has(element))renderSpectrum(element,Array(32).fill(0));
    const view=renderers.get(element);if(!view)return;
    view.draw=drawers[options.style];
    view.amplitude=options.amplitude;
    view.size=options.size;
    view.positionY=options.positionY;
    view.style=options.style;
    view.flipX=options.flipX;view.flipY=options.flipY;
    request(view);
}
export function refreshSpectrum(element){renderers.get(element)?.measure();}
export function disconnectSpectrum(element){
    const view=renderers.get(element);if(!view)return;
    view.disposed=true;
    cancelAnimationFrame(view.frame);view.resize.disconnect();document.removeEventListener('hit-theme-status',view.refresh);
    window.removeEventListener('resize',view.refresh);
    document.removeEventListener('visibilitychange',view.refresh);view.canvas.width=view.canvas.height=0;
    view.texture.width=view.texture.height=0;renderers.delete(element);
}
