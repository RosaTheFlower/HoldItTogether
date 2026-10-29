import {setDeleteDialog} from './hit-themes.js';
const connections=new WeakMap();
export function move(track,direction){
    const reduced=document.documentElement.dataset.hitMotion==='off'||matchMedia('(prefers-reduced-motion:reduce)').matches;
    track?.scrollBy({left:direction*Math.max(120,track.clientWidth*.8),behavior:reduced?'auto':'smooth'});
}
export function connect(track){
    if(!track||connections.has(track))return;
    const controller=new AbortController();
    track.addEventListener('wheel',event=>{
        if(event.ctrlKey)return;
        const delta=Math.abs(event.deltaX)>Math.abs(event.deltaY)?event.deltaX:event.deltaY;
        if((delta<0&&track.scrollLeft>1)||(delta>0&&track.scrollWidth-track.clientWidth-track.scrollLeft>1)){
            event.preventDefault();event.stopPropagation();track.scrollLeft+=delta;
        }
    },{passive:false,signal:controller.signal});
    track.addEventListener('keydown',event=>{if(event.target!==track)return;if(event.key==='ArrowLeft'||event.key==='ArrowRight'){event.preventDefault();event.stopPropagation();move(track,event.key==='ArrowLeft'?-1:1);}},{signal:controller.signal});
    connections.set(track,controller);
}
export function disconnect(track){connections.get(track)?.abort();connections.delete(track);}
export function dialog(element,visible){setDeleteDialog(element,visible);}
