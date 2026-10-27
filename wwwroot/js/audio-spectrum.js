// One full-DPI drawing surface replaces 32 animated DOM layers. Input remains the native 32-band FFT.
const renderers=new WeakMap();
export function renderSpectrum(element,values){
    if(!element?.isConnected||!Array.isArray(values))return;
    let view=renderers.get(element);
    if(!view){
        const canvas=element.querySelector('canvas'),context=canvas?.getContext('2d');if(!context)return;
        const texture=document.createElement('canvas'),brush=texture.getContext('2d');if(!brush)return;
        view={canvas,context,texture,brush,current:new Float32Array(32),target:new Float32Array(32),frame:0,width:0,height:0,last:0};
        view.measure=()=>{
            if(view.disposed)return;
            const rect=canvas.getBoundingClientRect(),dpi=devicePixelRatio||1,scale=dpi*rect.width/(canvas.offsetWidth||rect.width||1);
            view.width=Math.max(0,Math.ceil(rect.width*dpi));view.height=Math.max(0,Math.ceil(rect.height*dpi));
            if(canvas.width!==view.width)canvas.width=view.width;if(canvas.height!==view.height)canvas.height=view.height;
            view.gap=3*scale;view.barWidth=Math.max(1,(view.width-view.gap*31)/32);
            texture.width=Math.ceil(view.barWidth);texture.height=Math.max(1,view.height);
            const gradient=brush.createLinearGradient(0,texture.height,0,0),color=getComputedStyle(canvas).color;
            gradient.addColorStop(0,'transparent');gradient.addColorStop(1,color);
            brush.clearRect(0,0,texture.width,texture.height);brush.fillStyle=gradient;
            brush.beginPath();brush.roundRect(0,0,texture.width,texture.height,[3*scale,3*scale,0,0]);brush.fill();
            request(view);
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
    let moving=false;view.context.clearRect(0,0,view.width,view.height);
    for(let index=0;index<32;index++){
        const delta=view.target[index]-view.current[index];
        if(Math.abs(delta)>.001){view.current[index]+=delta*smoothing;moving=true;}else view.current[index]=view.target[index];
        const height=view.height*(.025+view.current[index]*.975),x=index*(view.barWidth+view.gap);
        view.context.drawImage(view.texture,x,view.height-height,view.barWidth,height);
    }
    if(moving&&!reduced)request(view);
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
