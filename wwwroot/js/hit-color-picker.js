import {setDeleteDialog} from './hit-themes.js';
const connections=new WeakMap();
let active=null;
export function disconnect(dialog){
    const state=connections.get(dialog);if(!state)return;
    state.controller.abort();state.observer.disconnect();state.disposed=true;
    cancelAnimationFrame(state.frame);setDeleteDialog(dialog,false);
    if(active===dialog)active=null;
    connections.delete(dialog);
}
export function connect(dialog,button,open,dotnet){
    if(!open){const wasOpen=dialog.open;disconnect(dialog);if(wasOpen&&button.isConnected)button.focus({preventScroll:true});return;}
    if(connections.has(dialog))return;
    if(active&&active!==dialog){connections.get(active)?.close();disconnect(active);}
    active=dialog;
    const controller=new AbortController(),signal=controller.signal;
    const state={controller,disposed:false,frame:0,pending:null,busy:false,observer:null,close:()=>{if(!state.disposed)dotnet.invokeMethodAsync('Close');}};
    const panel=dialog.querySelector('.hit-color-sv');
    async function flush(){
        state.frame=0;if(state.busy||state.disposed||!state.pending)return;
        const value=state.pending;state.pending=null;state.busy=true;
        try{await dotnet.invokeMethodAsync('SetSaturationBrightness',...value);}finally{state.busy=false;if(state.pending&&!state.disposed)state.frame=requestAnimationFrame(flush);}
    }
    function update(e){const r=panel.getBoundingClientRect();state.pending=[Math.max(0,Math.min(100,(e.clientX-r.left)/r.width*100)),Math.max(0,Math.min(100,100-(e.clientY-r.top)/r.height*100))];if(!state.frame)state.frame=requestAnimationFrame(flush);}
    panel.addEventListener('pointerdown',e=>{if(e.button!==0)return;e.preventDefault();panel.focus();panel.setPointerCapture(e.pointerId);update(e);},{signal});
    panel.addEventListener('pointermove',e=>{if(panel.hasPointerCapture(e.pointerId))update(e);},{signal});
    panel.addEventListener('pointerup',e=>{if(panel.hasPointerCapture(e.pointerId)){update(e);panel.releasePointerCapture(e.pointerId);}},{signal});
    panel.addEventListener('keydown',e=>{if(e.key.startsWith('Arrow'))e.preventDefault();},{signal});
    dialog.addEventListener('keydown',e=>{if(e.key==='Escape'){e.preventDefault();e.stopImmediatePropagation();state.close();}},{capture:true,signal});
    dialog.addEventListener('pointerdown',e=>{const r=dialog.getBoundingClientRect();if(e.target===dialog&&(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom))state.close();},{signal});
    document.addEventListener('wheel',e=>{if(!dialog.contains(e.target)){e.preventDefault();e.stopImmediatePropagation();}},{capture:true,passive:false,signal});
    state.observer=new MutationObserver(()=>{if(dialog.closest('[hidden],[inert]'))state.close();});
    state.observer.observe(document.querySelector('.sphere-area')??document.body,{subtree:true,attributes:true,attributeFilter:['hidden','inert']});
    connections.set(dialog,state);setDeleteDialog(dialog,true);
}
