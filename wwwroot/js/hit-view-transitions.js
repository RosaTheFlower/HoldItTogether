// Presentation only: stable page markers prevent animation restarts on ordinary rerenders.
let observer,preferences,frame=0,motion;
const states=new Map();
function scan(){
    frame=0;
    const sphere=document.querySelector('.dynamic-sphere');
    const enabled=sphere?.dataset.hitEntryAnimation!=='false'&&document.documentElement.dataset.hitMotion!=='off'&&!motion.matches;
    for(const [panel,state]of states)if(!panel.isConnected){state.animation?.cancel();states.delete(panel);}
    for(const panel of document.querySelectorAll('.hit-menu-panel')){
        const visible=!panel.closest('[hidden],[inert]');
        const target=panel.querySelector('.settings-content')||panel;
        const key=[...panel.querySelectorAll('[data-hit-view],.chess-settings')].map(node=>node.dataset.hitView||'settings').join('/');
        const previous=states.get(panel);
        if(enabled&&visible&&previous?.visible&&previous.key===key)continue;
        if(!enabled||!visible)previous?.animation?.cancel();
        const state={visible,key,animation:enabled&&visible?previous?.animation:null};states.set(panel,state);
        if(!enabled||!visible||(previous?.visible&&previous.key===key))continue;
        previous?.animation?.cancel();
        const animation=target.animate([{opacity:0,transform:'translateY(4px)'},{opacity:1,transform:'none'}],{duration:180,easing:'ease-out'});
        state.animation=animation;
        animation.finished.then(()=>{if(state.animation===animation){animation.cancel();state.animation=null;}}).catch(()=>{});
    }
}
function schedule(){if(!frame)frame=requestAnimationFrame(scan);}
export function connectEntries(){
    if(observer)return;
    motion=matchMedia('(prefers-reduced-motion:reduce)');motion.addEventListener('change',schedule);
    observer=new MutationObserver(schedule);
    observer.observe(document.querySelector('.sphere-area')||document.body,{subtree:true,childList:true,attributes:true,attributeFilter:['hidden','inert','data-hit-view','data-hit-entry-animation']});
    preferences=new MutationObserver(schedule);preferences.observe(document.documentElement,{attributes:true,attributeFilter:['data-hit-motion']});
    scan();
}
export function disconnectEntries(){
    observer?.disconnect();observer=null;preferences?.disconnect();preferences=null;motion?.removeEventListener('change',schedule);
    cancelAnimationFrame(frame);frame=0;
    for(const state of states.values())state.animation?.cancel();states.clear();
}
