// Shared mouse gestures. Native/feature state remains in the host.
export function connectNavigation(sphere, dotnet) {
    const controller=new AbortController(),signal=controller.signal;
    let hold=null,side=null,suppressClick=false,disposed=false,busy=false,contextStarted=null;
    const configuring=()=>sphere.querySelector('.hit-menu-host')?.dataset.hitConfiguration==='true';
    const blocked=()=>!!sphere.querySelector('dialog[open]');
    const call=async(name,...args)=>{
        if(disposed||busy)return;
        busy=true;
        try{await dotnet.invokeMethodAsync(name,...args);}
        catch(error){if(!disposed)console.error('HIT navigation failed.',error);}
        finally{busy=false;}
    };
    function cancel(){if(hold){clearTimeout(hold.timer);hold.title.classList.remove('is-title-holding');hold.title.style.removeProperty('--title-hold-duration');hold.fill?.remove();hold.inverse?.remove();hold=null;}side=null;}
    function down(event){
        if(event.button===2)contextStarted=sphere.querySelector('.hit-menu-host')?.dataset.hitConfiguration==='true';
        if(blocked()||!event.isPrimary)return;
        if((event.button===3||event.button===4)&&sphere.dataset.hitSideButtons==='true'){
            event.preventDefault();event.stopImmediatePropagation();side={id:event.pointerId,button:event.button};return;
        }
        const title=event.target.closest('.sphere-title[data-hit-menu-trigger="settings"]');
        if(event.button!==0||!title||sphere.dataset.hitTitleHold!=='true'||configuring())return;
        cancel();suppressClick=true;
        // Reserve the title gesture so window dragging cannot start at the same time.
        event.preventDefault();event.stopPropagation();
        const current={title,id:event.pointerId,x:event.clientX,y:event.clientY};hold=current;
        const duration=Math.max(300,Math.min(1500,Number(sphere.dataset.hitTitleHoldDelay)||700));
        title.style.setProperty('--title-hold-duration',duration+'ms');
        const fill=document.createElement('span'),inverse=document.createElement('span');
        fill.className='title-hold-fill';inverse.className='title-hold-inverse';
        fill.setAttribute('aria-hidden','true');inverse.setAttribute('aria-hidden','true');
        for(const child of [...title.childNodes])inverse.append(child.cloneNode(true));
        title.append(fill,inverse);current.fill=fill;current.inverse=inverse;
        title.classList.add('is-title-holding');
        current.timer=setTimeout(()=>{
            if(hold!==current||disposed||blocked())return;
            if(configuring()){cancel();return;}
            suppressClick=true;cancel();void call('OpenSettingsFromHold');
        },duration);
    }
    function up(event){
        if(side&&event.pointerId===side.id&&event.button===side.button){
            const direction=side.button===3?-1:1;side=null;
            event.preventDefault();event.stopImmediatePropagation();if(sphere.contains(event.target)&&!blocked())void call('NavigateMouse',direction,false);
        }
        if(hold?.id===event.pointerId){suppressClick=true;cancel();}
    }
    function move(event){if(hold?.id===event.pointerId&&Math.hypot(event.clientX-hold.x,event.clientY-hold.y)>6)cancel();}
    function context(event){
        if(blocked()||event.target.closest('.sphere-title'))return;
        const started=contextStarted;contextStarted=null;
        if(sphere.querySelector('.hit-menu-host')?.dataset.hitConfiguration!=='true')return;
        event.preventDefault();event.stopImmediatePropagation();cancel();if(started!==false)void call('NavigateMouse',-1,true);
    }
    sphere.addEventListener('pointerdown',down,{capture:true,signal});
    window.addEventListener('pointerup',up,{capture:true,signal});
    window.addEventListener('pointermove',move,{capture:true,signal});
    window.addEventListener('pointercancel',cancel,{signal});window.addEventListener('blur',cancel,{signal});
    sphere.addEventListener('pointerleave',()=>{if(hold)cancel();},{signal});
    sphere.addEventListener('wheel',cancel,{capture:true,passive:true,signal});
    sphere.addEventListener('contextmenu',context,{capture:true,signal});
    sphere.addEventListener('click',event=>{if(suppressClick){suppressClick=false;if(event.target.closest('.sphere-title')){event.preventDefault();event.stopImmediatePropagation();}}},{capture:true,signal});
    sphere.addEventListener('auxclick',event=>{if((event.button===3||event.button===4)&&sphere.dataset.hitSideButtons==='true'){event.preventDefault();event.stopImmediatePropagation();}},{capture:true,signal});
    const configurationObserver=new MutationObserver(()=>{if(configuring())cancel();});
    const host=sphere.querySelector('.hit-menu-host');if(host)configurationObserver.observe(host,{attributes:true,attributeFilter:['data-hit-configuration']});
    const preferences=new MutationObserver(()=>{if(sphere.dataset.hitTitleHold!=='true'||sphere.dataset.hitSideButtons!=='true')cancel();});
    preferences.observe(sphere,{attributes:true,attributeFilter:['data-hit-title-hold','data-hit-side-buttons','data-hit-title-hold-delay']});
    return()=>{disposed=true;cancel();controller.abort();preferences.disconnect();configurationObserver.disconnect();};
}
