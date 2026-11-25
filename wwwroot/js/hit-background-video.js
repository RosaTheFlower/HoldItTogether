const connections=new WeakMap();
export function disconnect(root){connections.get(root)?.();connections.delete(root);}
export function connect(root,dotnet=null){
    disconnect(root);
    const sphere=root.closest('.dynamic-sphere'),area=sphere?.closest('.sphere-area');
    const motion=matchMedia('(prefers-reduced-motion:reduce)'),videos=new Map();
    let timer=0,disposed=false,lastSuspended=null;
    function release(video){video.pause();video.removeAttribute('src');video.load();videos.delete(video);}
    function isShown(){
        if(!sphere||!root.isConnected||root.closest('[hidden],[inert]'))return false;
        const style=getComputedStyle(root);
        return root.getClientRects().length>0&&style.display!=='none'&&style.visibility!=='hidden'&&Number(style.opacity)>0&&Number(getComputedStyle(sphere).opacity)>0;
    }
    function canPlay(shown=isShown()){
        const permitted=root.dataset.hitBackgroundActive===undefined?sphere?.classList.contains('is-open'):root.dataset.hitBackgroundActive==='true';
        return permitted&&shown&&area?.dataset.hitResourceSleep!=='true'&&!document.hidden;
    }
    function refresh(){
        if(disposed)return;
        const fixed=motion.matches||document.documentElement.dataset.hitMotion==='off';
        const shown=isShown(),suspended=fixed||document.hidden||!shown;
        if(dotnet&&suspended!==lastSuspended){lastSuspended=suspended;dotnet.invokeMethodAsync('SetPresentationSuspended',suspended).catch(()=>{});}
        const visible=canPlay(shown);
        const present=new Set(root.querySelectorAll('[data-background-video]'));
        for(const video of videos.keys())if(!present.has(video))release(video);
        for(const video of present){
            let state=videos.get(video);if(!state){state={pending:false};videos.set(video,state);}
            if(!video.muted)video.muted=true;
            if(video.hidden!==fixed)video.hidden=fixed;
            if(!visible||fixed){video.pause();continue;}
            if(!video.getAttribute('src'))video.src=video.dataset.src;
            if(video.paused&&!state.pending){state.pending=true;video.play().catch(()=>{}).finally(()=>{state.pending=false;if(disposed||!root.contains(video)||motion.matches||document.documentElement.dataset.hitMotion==='off'||!canPlay())video.pause();});}
        }
    }
    const observer=new MutationObserver(records=>{
        refresh();
        if(records.some(r=>r.type==='attributes'&&r.attributeName?.startsWith('data-audio-'))){clearTimeout(timer);timer=setTimeout(refresh,700);}
    });
    const transitioned=event=>{if(event.target===root&&event.propertyName==='opacity')refresh();};
    root.addEventListener('transitionend',transitioned);
    observer.observe(document.documentElement,{subtree:true,childList:true,attributes:true,attributeFilter:['class','hidden','inert','data-hit-geometry-pending','data-hit-background-active','data-hit-motion','data-hit-resource-sleep','data-audio-cover','data-audio-background-blend','data-audio-reading-hidden','data-audio-cover-hiding','data-audio-lyrics-neutral']});
    motion.addEventListener('change',refresh);document.addEventListener('visibilitychange',refresh);refresh();
    connections.set(root,()=>{disposed=true;clearTimeout(timer);observer.disconnect();root.removeEventListener('transitionend',transitioned);motion.removeEventListener('change',refresh);document.removeEventListener('visibilitychange',refresh);for(const video of videos.keys())release(video);});
}
