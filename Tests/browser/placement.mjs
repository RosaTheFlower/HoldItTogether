import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';
const root=resolve('obj/placement-browser');mkdirSync(root,{recursive:true});
const views={clock:readFileSync('obj/menu-tests/clock.html','utf8'),clockCompact:readFileSync('obj/menu-tests/clock-compact.html','utf8'),
    chess:readFileSync('obj/chess-tests/board.html','utf8'),chessCompact:readFileSync('obj/chess-tests/compact.html','utf8'),audio:readFileSync('obj/menu-tests/player.html','utf8')};
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><head><meta charset="utf-8">
${['app.css','css/dynamic-sphere.css','css/hit-menus.css','css/menus.css','css/theme-base.css','css/audio-shell.css','css/hit-controls.css'].map(file=>`<link rel="stylesheet" href="../../wwwroot/${file}">`).join('')}
<style>.sphere-area{position:absolute;left:0;top:0}</style></head><body><script type="module">
import * as shell from '../../wwwroot/js/dynamic-sphere.js';import * as themes from '../../wwwroot/js/hit-themes.js';import * as extensions from '../../wwwroot/js/hit-extensions.js';
window.shell=shell;window.themes=themes;window.views=${JSON.stringify(views)};window.errors=[];window.calls=[];window.cursor={x:0,y:0};window.grab=null;
window.addEventListener('error',e=>errors.push(e.message));window.addEventListener('pointermove',e=>cursor={x:e.clientX,y:e.clientY},true);
themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/HIT-OBSIDIAN-EYES.css','utf8'))});themes.setMotion(false);
extensions.styles('dev.hit.chess:chess',[{url:'extensions/dev.hit.chess/1.6.1/Layout/css/chess.css',css:${JSON.stringify(readFileSync('Modules/HIT.Chess/Layout/css/chess.css','utf8'))}}]);
window.initialize=async(kind,pivot)=>{
    if(window.area)shell.disconnect(area);
    document.querySelector('.sphere-area')?.remove();window.kind=kind;window.pivot=pivot;
    window.anchor={x:pivot.x===.5?600:pivot.x?1100:100,y:pivot.y===.5?500:pivot.y?950:100};
    document.body.insertAdjacentHTML('beforeend','<div class="sphere-area" id="area"><div class="dynamic-sphere is-open is-pinned" data-hit-compact-delay="10000" id="sphere"><div class="sphere-content"><header class="sphere-header"><h1 class="sphere-title">'+kind+'</h1></header><div class="hit-menu-host" id="host"><div class="hit-menu-measure"><section class="hit-menu-panel" id="panel"></section></div></div></div></div></div>');
    window.area=document.getElementById('area');window.sphere=document.getElementById('sphere');window.host=document.getElementById('host');window.panel=document.getElementById('panel');
    panel.dataset.hitMenu=kind==='chess'?'dev.hit.chess:chess':'HoldItTogether.Components.HitMenus.HitMenu'+(kind==='clock'?'Clock':'Audio');panel.innerHTML=views[kind];
    await shell.observe(area,{async invokeMethodAsync(name,...args){
        calls.push({name,args});
        if(name==='PrepareSurface')return pivot;
        if(name==='ResizeWindow'){
            area.style.left=(anchor.x-args[3]-args[5]*window.pivot.x)+'px';area.style.top=(anchor.y-args[4]-args[6]*window.pivot.y)+'px';
            await new Promise(resolve=>setTimeout(resolve,8));return window.pivot;
        }
        if(name==='BeginDrag'){grab={x:args[0],y:args[1]};return true;}
        if(name==='DragWindow'){
            const r=sphere.getBoundingClientRect(),left=Math.max(0,Math.min(1200-r.width,cursor.x-grab.x)),top=Math.max(0,Math.min(1050-r.height,cursor.y-grab.y));
            area.style.left=(parseFloat(area.style.left)+left-r.left)+'px';area.style.top=(parseFloat(area.style.top)+top-r.top)+'px';anchor={x:left+r.width*window.pivot.x,y:top+r.height*window.pivot.y};
        }
        if(name==='EndDrag'){
            const r=sphere.getBoundingClientRect(),x=(r.left+r.width/2)/1200,y=(r.top+r.height/2)/1050,central=x>=1/3&&x<=2/3&&y>=1/3&&y<=2/3;
            window.pivot=central?{x:.5,y:.5}:{x:x<.5?0:1,y:y<.5?0:1};anchor={x:r.left+r.width*window.pivot.x,y:r.top+r.height*window.pivot.y};return window.pivot;
        }
    }});
};
window.compact=(small)=>{
    if(kind==='audio'){
        if(small){sphere.dataset.hitControlsHidden='true';sphere.dataset.hitIdleView='player';sphere.dataset.hitLayoutAnimating='true';}
        else {delete sphere.dataset.hitControlsHidden;delete sphere.dataset.hitIdleView;}
    }else{
        panel.innerHTML=views[kind+(small?'Compact':'')];host.classList.toggle('is-compact',small);sphere.classList.toggle('is-menu-compact',small);document.querySelector('.sphere-header').hidden=small;
    }
};
window.startSampling=()=>{window.points=[];window.sampling=true;requestAnimationFrame(function sample(){if(!sampling)return;const r=sphere.getBoundingClientRect();points.push({x:r.left+r.width*pivot.x,y:r.top+r.height*pivot.y});requestAnimationFrame(sample)});};
window.ready=true;</script></body></html>`);
const browser=await openBrowser(root),{evaluate,command}=browser;
try{
    for(const kind of ['clock','chess','audio'])for(const pivot of [{x:0,y:0},{x:1,y:0},{x:0,y:1},{x:1,y:1},{x:.5,y:.5}]){
        await evaluate(`await initialize('${kind}',${JSON.stringify(pivot)})`);await wait(880);
        const expected=await evaluate('({...anchor})');
        const fullWidth=await evaluate("panel.querySelector('.chess-board-frame,.clock-digits')?.getBoundingClientRect().width??0");
        await evaluate('startSampling();compact(true);new Promise(requestAnimationFrame)');
        if(kind!=='audio') {
            const firstWidth=await evaluate("panel.querySelector('.chess-board-frame,.clock-digits').getBoundingClientRect().width");
            assert.ok(firstWidth>fullWidth*.85,kind+' content must resize with the shell instead of jumping immediately to compact size');
        }
        await wait(kind==='audio'?1400:880);
        const compactWidth=await evaluate("panel.querySelector('.chess-board-frame,.clock-digits')?.getBoundingClientRect().width??0");
        await evaluate('compact(false);new Promise(requestAnimationFrame)');
        if(kind!=='audio') {
            const firstWidth=await evaluate("panel.querySelector('.chess-board-frame,.clock-digits').getBoundingClientRect().width");
            assert.ok(firstWidth<compactWidth*1.2,kind+' content must expand continuously from its compact size');
        }
        await wait(kind==='audio'?1400:880);await evaluate('sampling=false');
        const error=await evaluate(`Math.max(...points.map(p=>Math.max(Math.abs(p.x-${expected.x}),Math.abs(p.y-${expected.y}))))`);
        assert.ok(error<1.1,kind+' must keep its '+JSON.stringify(pivot)+' anchor through compact/expand; error '+error);
    }
    // Open/close from the same small sphere repeatedly; sample the very first paint as well.
    for(const pivot of [{x:0,y:0},{x:1,y:1},{x:.5,y:.5}]){
        await evaluate(`await initialize('chess',${JSON.stringify(pivot)});sphere.className='dynamic-sphere is-away'`);await wait(900);
        const expected=await evaluate('({...anchor})');
        await evaluate("startSampling();sphere.className='dynamic-sphere is-open is-pinned'");await wait(880);
        await evaluate("sphere.className='dynamic-sphere is-away'");await wait(880);await evaluate('sampling=false');
        const error=await evaluate(`Math.max(...points.map(p=>Math.max(Math.abs(p.x-${expected.x}),Math.abs(p.y-${expected.y}))))`);
        assert.ok(error<1.1,'Sphere opening/closing must not teleport inside the retained canvas: '+error);
    }
    await evaluate("await initialize('clock',{x:1,y:1});compact(true)");await wait(900);
    const grabPoint=await evaluate("(()=>{const r=sphere.getBoundingClientRect();return{x:r.left+r.width/2,y:r.top+r.height/2}})()");
    await command('Input.dispatchMouseEvent',{type:'mousePressed',...grabPoint,button:'left',buttons:1,clickCount:1});
    await command('Input.dispatchMouseEvent',{type:'mouseMoved',x:200,y:200,button:'left',buttons:1});await wait(90);
    const dropped=await evaluate("(()=>{const r=sphere.getBoundingClientRect();return{x:r.left,y:r.top}})()");
    await command('Input.dispatchMouseEvent',{type:'mouseReleased',x:200,y:200,button:'left',buttons:0,clickCount:1});await wait(120);
    const settled=await evaluate("(()=>{const r=sphere.getBoundingClientRect();return{x:r.left,y:r.top,held:sphere.hasAttribute('data-hit-drag-sizing')}})()");
    assert.ok(Math.abs(settled.x-dropped.x)<1.1&&Math.abs(settled.y-dropped.y)<1.1,'Dropping a compact clock must not reposition it when its growth direction changes');
    assert.equal(settled.held,false);assert.ok(await evaluate('grab.x>0&&grab.x<200&&grab.y>0&&grab.y<100'),'The drag grab is relative to the compact visible readout');
    await evaluate("await initialize('chess',{x:1,y:1});themes.setMotion(true);compact(true);new Promise(requestAnimationFrame)");
    assert.equal(await evaluate("sphere.hasAttribute('data-hit-compact-motion')"),false,'Remove animations also disables board/readout resizing');
    await evaluate('shell.disconnect(area)');assert.deepEqual(await evaluate('errors'),[]);
    console.log('PASS: real Clock/Chess/Audio layouts preserve all corner/center anchors through compact/expand, sphere first-frame opening/closing and compact-clock drag/release.');
}finally{await browser.close();}
