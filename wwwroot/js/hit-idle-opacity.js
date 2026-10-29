// A single departure timer changes presentation only; native input and resource sleep are independent.
export function connectIdleOpacity(area,sphere) {
    const controller=new AbortController(),signal=controller.signal;
    let timer=0,disposed=false,pressed=false,inside=sphere.matches(':hover'),signature='',options={};
    function read(){try{options=JSON.parse(area.dataset.hitOpacity||'{}');}catch{options={};}}
    function mode(){
        if(!sphere.classList.contains('is-open'))return 'sphere';
        return sphere.classList.contains('is-menu-compact')||sphere.dataset.hitControlsHidden==='true'?'compact':'open';
    }
    function protectedView(){
        const open=sphere.classList.contains('is-open');
        const focused=document.activeElement;
        return document.hidden||pressed||inside||sphere.matches(':hover')||sphere.classList.contains('is-dragging')||
            (document.hasFocus()&&(sphere.matches(':focus-visible')||!!sphere.querySelector(':focus-visible')||
                (sphere.contains(focused)&&focused?.matches('input,select,textarea,[contenteditable="true"]'))))||
            !!sphere.querySelector('dialog[open],.chess-promotion')||
            (open&&(sphere.querySelector('.hit-menu-host')?.dataset.hitConfiguration==='true'||!!sphere.querySelector('.clock-notice')));
    }
    function restore(){clearTimeout(timer);timer=0;delete area.dataset.hitOpacityIdle;}
    function refresh(force=false){
        if(disposed)return;
        const state=mode(),blocked=protectedView();
        const next=JSON.stringify([options,state,blocked]);
        if(!force&&next===signature)return;
        signature=next;restore();
        area.style.setProperty('--hit-idle-opacity',String(Math.max(.1,Math.min(.9,Number(options.opacity??40)/100))));
        area.style.setProperty('--hit-idle-fade',Math.max(0,Math.min(3000,Number(options.duration??650)))+'ms');
        if(!options.enabled||!options[state]||blocked)return;
        timer=setTimeout(()=>{
            timer=0;if(disposed||protectedView()||!options.enabled||!options[mode()])return;
            area.dataset.hitOpacityIdle='true';
        },Math.max(0,Math.min(60000,Number(options.delay??5000))));
    }
    function activity(event){
        if(event.type==='pointerenter')inside=true;
        if(event.type==='pointerdown')pressed=true;
        refresh(true);
    }
    sphere.addEventListener('pointerenter',activity,{signal});
    sphere.addEventListener('pointerleave',()=>{inside=false;refresh(true);},{signal});
    for(const name of ['pointermove','pointerdown','wheel','keydown','input','focusin'])sphere.addEventListener(name,activity,{capture:true,passive:true,signal});
    sphere.addEventListener('focusout',()=>queueMicrotask(()=>refresh(true)),{signal});
    for(const name of ['pointerup','pointercancel'])window.addEventListener(name,()=>{if(pressed){pressed=false;refresh(true);}},{capture:true,signal});
    window.addEventListener('blur',()=>{pressed=false;inside=false;refresh(true);},{signal});
    document.addEventListener('visibilitychange',()=>refresh(true),{signal});
    const observer=new MutationObserver(records=>{
        if(records.some(record=>record.target===area&&record.attributeName==='data-hit-opacity'))read();
        refresh();
    });
    observer.observe(area,{attributes:true,attributeFilter:['data-hit-opacity']});
    observer.observe(sphere,{subtree:true,childList:true,attributes:true,attributeFilter:['class','hidden','open','data-hit-configuration','data-hit-controls-hidden','data-hit-view']});
    read();refresh();
    return()=>{disposed=true;restore();controller.abort();observer.disconnect();area.style.removeProperty('--hit-idle-opacity');area.style.removeProperty('--hit-idle-fade');};
}
