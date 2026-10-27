// Only playback/reading, the chess board and the main clock can compact.
export function connectMenuChrome(sphere, dotnet) {
    let timer=0, layoutTimer=0, disposed=false, root=null, view='', mode='', eligible=false;
    let hidden=false, pointerInside=sphere.matches(':hover'), pointer=null, leftAt=performance.now(), pending=false,pressed=false;
    const targets=new Map();
    const delay=()=>Math.max(0,Math.min(10000,Number(sphere.dataset.hitCompactDelay ?? 5000)));
    let configuredDelay=delay();
    function endLayout() { clearTimeout(layoutTimer); delete sphere.dataset.hitLayoutAnimating; }
    function beginLayout() {
        clearTimeout(layoutTimer);
        if(document.documentElement.dataset.hitMotion==='off'||matchMedia('(prefers-reduced-motion: reduce)').matches)return endLayout();
        sphere.dataset.hitLayoutAnimating='true';layoutTimer=setTimeout(endLayout,1300);
    }
    function conceal(node) {
        if(!node||targets.has(node))return;
        targets.set(node,{inert:node.inert,aria:node.getAttribute('aria-hidden')});
        node.inert=true;node.setAttribute('aria-hidden','true');
    }
    function applyTargets() {
        conceal(sphere.querySelector('.sphere-header'));
        if(mode==='controls')for(const node of root.querySelectorAll('[data-hit-idle-controls]'))conceal(node);
    }
    function show(animate=false) {
        if(hidden&&animate)beginLayout();else if(!animate)endLayout();
        hidden=false;delete sphere.dataset.hitControlsHidden;delete sphere.dataset.hitIdleView;
        for(const [node,old]of targets){node.inert=old.inert;if(old.aria===null)node.removeAttribute('aria-hidden');else node.setAttribute('aria-hidden',old.aria);}
        targets.clear();
    }
    function canCompact() {
        return eligible&&!pressed&&!pointerInside&&!sphere.matches(':hover')&&!sphere.querySelector('dialog[open],.chess-promotion')&&!pending;
    }
    function arm() {
        clearTimeout(timer);timer=0;
        if(disposed||hidden||!canCompact())return;
        timer=setTimeout(async()=>{
            timer=0;if(disposed||!canCompact())return;
            if(mode==='menu'){
                pending=true;
                try{if(!dotnet||await dotnet.invokeMethodAsync('CompactCurrentMenu')===false)root.dispatchEvent(new CustomEvent('hit-menu-idle'));}
                catch(error){if(!disposed)console.error('HIT menu compaction failed.',error);}
                finally{pending=false;}
                return;
            }
            hidden=true;beginLayout();
            sphere.dataset.hitControlsHidden='true';sphere.dataset.hitIdleView=view;
            applyTargets();
        },Math.max(0,delay()-(performance.now()-leftAt)));
    }
    function refresh() {
        const nextDelay=delay(),delayChanged=configuredDelay!==nextDelay;configuredDelay=nextDelay;
        const panel=sphere.querySelector('.hit-menu-panel:not([hidden])');
        const nextRoot=panel?.querySelector('[data-hit-view]')??panel;
        const nextView=nextRoot?.dataset.hitView??'';
        const nextMode=nextRoot?.classList.contains('audio-menu')&&nextRoot.dataset.hitHideControls==='true'&&['player','lyrics'].includes(nextView)?'controls':
            (nextRoot?.classList.contains('chess-menu')&&nextView==='board')||
            (nextRoot?.classList.contains('clock-menu')&&nextView==='main')?'menu':'';
        const nextEligible=!!nextMode&&sphere.classList.contains('is-open')&&sphere.classList.contains('is-pinned')&&
            !sphere.classList.contains('is-dragging')&&!sphere.classList.contains('is-menu-compact');
        const changed=nextRoot!==root||nextView!==view||nextMode!==mode||nextEligible!==eligible;
        if(changed){show();leftAt=performance.now();}
        root=nextRoot;view=nextView;mode=nextMode;eligible=nextEligible;
        if(changed||delayChanged||(!timer&&!hidden))arm();
        if(hidden)applyTargets();
        if(sphere.querySelector('dialog[open]')){if(hidden)show(true);clearTimeout(timer);timer=0;}
    }
    function enter(event) {pointerInside=true;clearTimeout(timer);timer=0;activity(event);}
    function leave(event) {pointerInside=false;pointer={x:event.screenX,y:event.screenY};leftAt=performance.now();arm();}
    function activity(event) {
        if(event.type==='pointerdown')pressed=true;
        if(event.type==='pointerup')pressed=false;
        if(event.type==='pointermove'||event.type==='pointerenter'){
            const next={x:event.screenX,y:event.screenY},unchanged=pointer&&next.x===pointer.x&&next.y===pointer.y;
            pointer=next;pointerInside=true;clearTimeout(timer);timer=0;
            if(unchanged)return;
        }
        if(hidden&&mode==='controls')show(true);
        if(!pointerInside){leftAt=performance.now();arm();}
    }
    function release(){pressed=false;if(!pointerInside){leftAt=performance.now();arm();}}
    window.addEventListener('pointerup',release,true);window.addEventListener('pointercancel',release,true);
    sphere.addEventListener('pointerenter',enter);sphere.addEventListener('pointerleave',leave);
    const events=['pointermove','pointerdown','pointerup','wheel','focusin','input','keydown'];
    for(const name of events)sphere.addEventListener(name,activity,{capture:true,passive:true});
    const observer=new MutationObserver(refresh);
    observer.observe(sphere,{subtree:true,childList:true,attributes:true,attributeFilter:['class','hidden','open','data-hit-view','data-hit-hide-controls','data-hit-compact-delay']});
    refresh();
    return()=>{
        disposed=true;clearTimeout(timer);observer.disconnect();
        sphere.removeEventListener('pointerenter',enter);sphere.removeEventListener('pointerleave',leave);
        window.removeEventListener('pointerup',release,true);window.removeEventListener('pointercancel',release,true);
        for(const name of events)sphere.removeEventListener(name,activity,true);
        show();
    };
}
