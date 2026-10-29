import {connectEntries,disconnectEntries} from './hit-view-transitions.js';
const connections=new WeakMap();
let nextPickerId=0;
export function connect(root){
    if(!root)return;
    if(connections.has(root)){connections.get(root).refresh();return;}
    const controls=new Map(),abort=new AbortController();let popup=null,frame=0,retiring=null;
    const finishClosing=()=>{
        if(!retiring)return;
        clearTimeout(retiring.timer);
        retiring.element.removeEventListener('animationend',retiring.finish);
        retiring.element.removeEventListener('animationcancel',retiring.finish);
        retiring.element.remove();retiring=null;
    };
    const close=(restore=false,focus=false,animate=true)=>{
        if(!animate)finishClosing();
        const current=popup;popup=null;delete root.dataset.hitSelectOpen;
        root.querySelectorAll('[role=combobox]').forEach(b=>{b.setAttribute('aria-expanded','false');b.removeAttribute('aria-controls');});
        if(restore)current?._restore?.();if(focus)current?._button?.focus();
        if(!current)return;
        if(!animate||!root.isConnected||root.closest('[hidden],[inert]')||!root.contains(current._input)||
            document.documentElement.dataset.hitMotion==='off'||matchMedia('(prefers-reduced-motion: reduce)').matches){current.remove();return;}
        finishClosing();
        const previous=getComputedStyle(current);
        current.style.setProperty('--settings-picker-from-opacity',previous.opacity);
        current.style.setProperty('--settings-picker-from-transform',previous.transform);
        if(current.contains(document.activeElement))document.activeElement.blur();
        current.inert=true;current.setAttribute('aria-hidden','true');current.removeAttribute('role');
        current.classList.add('is-closing');
        const animationName=getComputedStyle(current).animationName;
        if(animationName==='none'){current.remove();return;}
        const finish=event=>{if(event.target===current&&event.animationName===animationName)finishClosing();};
        retiring={element:current,finish,timer:setTimeout(finishClosing,200)};
        current.addEventListener('animationend',finish);current.addEventListener('animationcancel',finish);
    };
    const sidebar=root.querySelector('.settings-sidebar');
    const highlight=sidebar?Object.assign(document.createElement('span'),{className:'settings-topic-highlight'}):null;
    if(highlight){highlight.setAttribute('aria-hidden','true');sidebar.prepend(highlight);}
    const updateHighlight=()=>{
        const active=sidebar?.querySelector('.settings-topic[aria-current=page]');
        if(!highlight||!active||root.closest('[hidden],[inert]'))return;
        const y=active.offsetTop+'px',h=active.offsetHeight+'px';
        if(highlight.style.getPropertyValue('--topic-y')!==y)highlight.style.setProperty('--topic-y',y);
        if(highlight.style.height!==h)highlight.style.height=h;
        if(!highlight.dataset.ready){highlight.getBoundingClientRect();highlight.dataset.ready='true';}
    };
    const sidebarSize=new ResizeObserver(updateHighlight);if(sidebar)sidebarSize.observe(sidebar);
    const updateRange=input=>{
        const min=Number(input.min)||0,max=Number(input.max)||100;
        const fill=Math.max(0,Math.min(100,(Number(input.value)-min)/(max-min||1)*100))+'%';
        if(input.style.getPropertyValue('--range-fill')!==fill)input.style.setProperty('--range-fill',fill);
    };
    root.addEventListener('input',event=>{if(event.target.matches('input[type=range]'))updateRange(event.target);},{signal:abort.signal});
    const scan=()=>{
        frame=0;updateHighlight();root.querySelectorAll('input[type=range]').forEach(updateRange);
        if(popup&&(!root.isConnected||root.closest('[hidden],[inert]')||!root.contains(popup._input)||popup._input.matches(':disabled')||popup._options.some(option=>!popup._input.contains(option))))close(false,false,false);
        for(const [node,dispose]of controls)if(!root.contains(node)){dispose();controls.delete(node);}
        root.querySelectorAll('select,input[type=range]').forEach(input=>{
            if(controls.has(input)||input.closest('.settings-number'))return;
            const signal=new AbortController();
            if(input.tagName==='SELECT'){
                const button=document.createElement('button');button.type='button';button.className='settings-combobox';button.setAttribute('role','combobox');button.setAttribute('aria-haspopup','listbox');button.setAttribute('aria-expanded','false');
                button.setAttribute('aria-label',input.getAttribute('aria-label')||'Choose an option');
                input.classList.add('settings-native-select');input.setAttribute('aria-hidden','true');const tab=input.getAttribute('tabindex');input.tabIndex=-1;input.after(button);
                const sync=()=>{const text=input.selectedOptions[0]?.textContent||'—';if(button.textContent!==text)button.textContent=text;const disabled=input.disabled||input.options.length===0;if(button.disabled!==disabled)button.disabled=disabled;};sync();
                const open=()=>{
                    if(popup?._input===input){close(false,true);return;}
                    if(input.matches(':disabled')||!input.options.length)return;finishClosing();close(false,false,false);sync();
                    const options=[...input.options].filter(option=>!option.hidden),original=input.selectedIndex;
                    if(!options.length)return;
                    const disabled=option=>option.disabled||option.closest('optgroup')?.disabled;
                    let index=Math.max(0,options.indexOf(input.selectedOptions[0]));
                    const rows=Math.min(3,options.length%2?options.length:options.length+1),middle=Math.floor(rows/2),height=rows*36+6;
                    popup=document.createElement('div');const list=popup;
                    list.className='settings-select-popup';list.role='listbox';list.tabIndex=-1;
                    list.id='settings-picker-'+(++nextPickerId);list.setAttribute('aria-label',button.getAttribute('aria-label'));
                    list._input=input;list._button=button;list._options=options;
                    const rect=button.getBoundingClientRect(),bounds=root.getBoundingClientRect(),sx=bounds.width/root.offsetWidth||1,sy=bounds.height/root.offsetHeight||1;
                    list.style.height=height+'px';list.style.width=rect.width/sx+'px';
                    list.style.left=Math.max(0,(rect.left-bounds.left)/sx)+'px';
                    list.style.top=Math.max(4,Math.min(root.clientHeight-height-4,(rect.top-bounds.top+rect.height/2)/sy-height/2))+'px';
                    const wrap=value=>(value%options.length+options.length)%options.length;
                    const items=Array.from({length:rows},(_,slot)=>{
                        const item=document.createElement('button');item.type='button';item.role='option';item.tabIndex=-1;item.id=list.id+'-'+slot;
                        item.addEventListener('click',()=>{choose(wrap(index+slot-middle));close(false,true);});list.append(item);return item;
                    });
                    const render=()=>{
                        items.forEach((item,slot)=>{
                            const position=wrap(index+slot-middle),option=options[position];
                            if(item.textContent!==option.textContent)item.textContent=option.textContent;
                            if(item.disabled!==!!disabled(option))item.disabled=!!disabled(option);
                            const selected=String(slot===middle);if(item.getAttribute('aria-selected')!==selected)item.setAttribute('aria-selected',selected);
                            item.setAttribute('aria-posinset',String(position+1));item.setAttribute('aria-setsize',String(options.length));
                        });
                        list.setAttribute('aria-activedescendant',items[middle].id);
                    };
                    const choose=value=>{
                        if(disabled(options[value]))return;index=value;
                        if(input.selectedIndex!==options[index].index){input.selectedIndex=options[index].index;input.dispatchEvent(new Event('change',{bubbles:true}));}
                        sync();render();
                    };
                    const step=direction=>{
                        for(let distance=1;distance<=options.length;distance++){
                            const next=wrap(index+distance*direction);if(!disabled(options[next])){choose(next);return;}
                        }
                    };
                    list._restore=()=>{if(input.options[original]&&input.selectedIndex!==original){input.selectedIndex=original;input.dispatchEvent(new Event('change',{bubbles:true}));sync();}};
                    let travel=0,lastWheel=0;
                    list._wheel=event=>{
                        if(event.ctrlKey||event.metaKey)return;
                        if(event.timeStamp-lastWheel>180)travel=0;lastWheel=event.timeStamp;
                        const delta=(event.deltaY||event.deltaX)*(event.deltaMode===1?36:event.deltaMode===2?height:1);
                        if(travel&&Math.sign(travel)!==Math.sign(delta))travel=0;
                        travel+=Math.max(-36,Math.min(36,delta));
                        if(Math.abs(travel)>=36){step(Math.sign(travel));travel=0;}
                    };
                    list.addEventListener('keydown',event=>{
                        event.stopPropagation();
                        if(event.key==='Escape'){event.preventDefault();close(true,true);}
                        else if(event.key==='Enter'||event.key===' '){event.preventDefault();close(false,true);}
                        else if(event.key==='ArrowDown'||event.key==='ArrowUp'){event.preventDefault();step(event.key==='ArrowDown'?1:-1);}
                        else if(event.key==='Tab')close();
                        else if(event.key==='Home'||event.key==='End'){
                            event.preventDefault();const enabled=options.map((option,i)=>disabled(option)?-1:i).filter(i=>i>=0);
                            if(enabled.length)choose(event.key==='Home'?enabled[0]:enabled.at(-1));
                        }
                    });
                    root.append(list);root.dataset.hitSelectOpen='true';button.setAttribute('aria-expanded','true');button.setAttribute('aria-controls',list.id);render();list.focus();
                };
                button.addEventListener('click',open,{signal:signal.signal});button.addEventListener('keydown',e=>{if(e.key==='ArrowDown'||e.key==='ArrowUp'){e.preventDefault();open();}},{signal:signal.signal});
                input.addEventListener('change',sync,{signal:signal.signal});
                controls.set(input,()=>{signal.abort();button.remove();input.classList.remove('settings-native-select');input.removeAttribute('aria-hidden');if(tab===null)input.removeAttribute('tabindex');else input.setAttribute('tabindex',tab);close(false,false,false);});
                button._syncSetting=sync;
            }else{
                const number=document.createElement('input');number.type='number';number.className='settings-inline-number';number.setAttribute('aria-label',(input.getAttribute('aria-label')||'Setting')+' value');
                let field=number;
                if(input.dataset.settingUnit){
                    field=document.createElement('span');field.className='settings-inline-value';
                    const unit=document.createElement('span');unit.className='settings-inline-unit';unit.textContent=input.dataset.settingUnit;
                    field.append(number,unit);
                }
                input.after(field);
                const sync=()=>{number.value=input.value;number.min=input.min;number.max=input.max;number.step=input.step;const min=Number(input.min)||0,max=Number(input.max)||100;input.style.setProperty('--range-fill',Math.max(0,Math.min(100,(Number(input.value)-min)/(max-min||1)*100))+'%');const disabled=input.matches(':disabled');if(number.disabled!==disabled)number.disabled=disabled;};sync();
                input.addEventListener('input',sync,{signal:signal.signal});
                number.addEventListener('change',()=>{if(number.value===''||!Number.isFinite(number.valueAsNumber)){sync();return;}input.value=number.value;input.dispatchEvent(new Event('input',{bubbles:true}));input.dispatchEvent(new Event('change',{bubbles:true}));sync();},{signal:signal.signal});
                controls.set(input,()=>{signal.abort();field.remove();});number._syncSetting=sync;
            }
        });
        root.querySelectorAll('.settings-combobox,.settings-inline-number').forEach(node=>node._syncSetting?.());
        root.querySelectorAll('button small,label.hit-button small').forEach(small=>{if(small.hidden)return;const button=small.closest('button,label.hit-button');if(!button.title)button.title=small.textContent.trim();small.hidden=true;});
        root.querySelectorAll('label').forEach(label=>{
            const control=label.querySelector('input,select');if(!control||control.type==='radio'||control.type==='file'||label.classList.contains('hit-button')||label.querySelector('small'))return;
            const title=control.getAttribute('aria-label')||label.querySelector('span')?.textContent?.trim();if(!title)return;
            const small=document.createElement('small');small.className='setting-description';small.textContent=control.type==='color'?`Choose the ${title.toLowerCase()} used by the current appearance.`:control.type==='checkbox'?`Enable or disable ${title.toLowerCase()} for HIT.`:`Adjust ${title.toLowerCase()}; the displayed value is saved when changed.`;
            (label.querySelector('span')||label).append(small);
        });
    };
    const schedule=()=>{if(!frame)frame=requestAnimationFrame(scan);};
    const observer=new MutationObserver(schedule);observer.observe(root,{subtree:true,childList:true,attributes:true,attributeFilter:['disabled','value','selected','aria-current']});
    const visibility=new MutationObserver(()=>{if(root.closest('[hidden],[inert]'))close(false,false,false);});
    const panel=root.closest('.hit-menu-panel');if(panel)visibility.observe(panel,{attributes:true,attributeFilter:['hidden','inert']});
    document.addEventListener('pointerdown',e=>{if(popup&&!popup.contains(e.target)&&!e.target.closest('.settings-combobox'))close();},{signal:abort.signal});
    document.addEventListener('wheel',event=>{
        if(!popup)return;if(!root.isConnected||root.closest('[hidden],[inert]')){close(false,false,false);return;}
        event.preventDefault();event.stopPropagation();
        if(popup.contains(event.target)||popup._button.contains(event.target))popup._wheel(event);
    },{capture:true,passive:false,signal:abort.signal});
    document.addEventListener('visibilitychange',()=>{if(document.hidden)close(false,false,false);},{signal:abort.signal});
    connections.set(root,{refresh:scan,dispose:()=>{abort.abort();observer.disconnect();visibility.disconnect();sidebarSize.disconnect();highlight?.remove();cancelAnimationFrame(frame);close(false,false,false);controls.forEach(dispose=>dispose());connections.delete(root);}});scan();
}
export function disconnect(root){connections.get(root)?.dispose();}

let documentObserver,documentFrame=0;
const workspaces=new Set();
function scanDocument(){
    documentFrame=0;
    for(const root of workspaces)if(!root.isConnected){disconnect(root);workspaces.delete(root);}
    for(const root of document.querySelectorAll('[data-hit-settings-workspace]'))if(!workspaces.has(root)){
        workspaces.add(root);connect(root);
    }
    const descriptions=document.querySelector('.dynamic-sphere')?.dataset.hitSettingDescriptions;
    if(descriptions)for(const root of workspaces)if(root.dataset.descriptions!==descriptions)root.dataset.descriptions=descriptions;
}
export function connectDocument(){
    if(documentObserver)return;
    connectEntries();
    connectEntries();
    documentObserver=new MutationObserver(()=>{if(!documentFrame)documentFrame=requestAnimationFrame(scanDocument);});
    documentObserver.observe(document.querySelector('.sphere-area')||document.body,{childList:true,subtree:true,attributes:true,attributeFilter:['data-hit-setting-descriptions']});
    scanDocument();
}
export function disconnectDocument(){
    disconnectEntries();
    documentObserver?.disconnect();documentObserver=null;cancelAnimationFrame(documentFrame);documentFrame=0;
    workspaces.forEach(disconnect);workspaces.clear();
}
