import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/idle-input-browser');mkdirSync(root,{recursive:true});
const settings=readFileSync('obj/menu-tests/settings.html','utf8'),player=readFileSync('obj/menu-tests/player.html','utf8');
writeFileSync(resolve(root,'index.html'),`<!doctype html><html lang="en"><head><meta charset="utf-8">
${['app.css','css/dynamic-sphere.css','css/hit-menus.css','css/menus.css','css/theme-base.css','css/audio-shell.css','css/hit-controls.css'].map(file=>`<link rel="stylesheet" href="../../wwwroot/${file}">`).join('')}
<script src="../../wwwroot/js/desktop-input.js"></script>
<style>.sphere-area{position:absolute;left:50px;top:50px}.dynamic-sphere{width:max-content!important;height:auto!important}.sphere-content{position:relative!important;opacity:1!important}.hit-menu-host{position:relative}</style></head><body>
<div class="sphere-area"><div id="sphere" class="dynamic-sphere is-open is-pinned" data-hit-compact-delay="0"><div class="sphere-content"><header class="sphere-header"><h1 class="sphere-title">Settings</h1></header>
<div class="hit-menu-host"><div class="hit-menu-measure"><section class="hit-menu-panel" id="panel" data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuSettings">${settings}</section></div></div>
</div></div></div><script type="module">import {connectMenuChrome} from '../../wwwroot/js/hit-menu-chrome.js';import * as themes from '../../wwwroot/js/hit-themes.js';window.themes=themes;themes.setMotion(false);window.disconnectChrome=connectMenuChrome(sphere);window.calls=[];window.errors=[];window.wheels=0;sphere.addEventListener('wheel',()=>wheels++);addEventListener('error',e=>errors.push(e.message));window.player=${JSON.stringify(player)};window.ready=true;</script></body></html>`);
const browser=await openBrowser(root),{evaluate,command}=browser;
async function moveIn(){const point=await evaluate("(()=>{const r=sphere.getBoundingClientRect();return{x:r.left+r.width/2,y:r.top+60}})()");await command('Input.dispatchMouseEvent',{type:'mouseMoved',...point});return point;}
async function moveOut(){await command('Input.dispatchMouseEvent',{type:'mouseMoved',x:1100,y:1000});}
async function expanded(){assert.equal(await evaluate('sphere.dataset.hitControlsHidden'),undefined);assert.equal(await evaluate("document.querySelector('.sphere-header').inert"),false);}
try{
    await moveOut();await wait(80);await expanded();
    assert.equal(await evaluate("document.querySelector('.hit-menu-panel').inert"),false,'General Settings never compacts, even with immediate delay');
    const tooltip=await evaluate("(()=>{const icon=document.querySelector('.setting-danger-icon'),r=icon.getBoundingClientRect();return document.elementFromPoint(r.left+r.width/2,r.top+r.height/2).closest('[title]')?.title})()");
    assert.equal(tooltip,'Experimental. Use at your own risk!','The danger icon itself exposes its tooltip');
    for(const [className,view]of [['settings-menu','info'],['settings-menu','themes'],['settings-menu','extensions'],['audio-menu','settings'],['audio-menu','controls'],['audio-menu','appearance'],['audio-menu','effects'],['audio-menu','colors'],['clock-menu','settings'],['clock-menu','systemsettings'],['chess-menu','settings'],['notes-menu','main']]){
        await evaluate(`panel.innerHTML='<section class="${className}" data-hit-view="${view}" style="width:360px;height:200px"><button>Setting</button></section>'`);
        await moveIn();await moveOut();await wait(40);await expanded();
        assert.equal(await evaluate('sphere.dataset.hitGenericCompact'),undefined,'No summary compaction for '+className+'/'+view);
    }
    await evaluate("panel.dataset.hitMenu='HoldItTogether.Components.HitMenus.HitMenuAudio';panel.innerHTML=player;sphere.dataset.hitCompactDelay='5000'");
    await moveIn();await wait(5200);await expanded();
    await moveOut();await wait(1000);await expanded();
    await wait(4200);assert.equal(await evaluate('sphere.dataset.hitControlsHidden'),'true','Audio compacts five seconds after leaving');
    await moveIn();await expanded();
    await evaluate("sphere.dataset.hitCompactDelay='0';window.dialog=document.createElement('dialog');dialog.innerHTML='<button>Cancel</button>';panel.append(dialog);dialog.showModal()");
    await moveOut();await wait(80);await expanded();
    await evaluate('dialog.close();dialog.remove()');await wait(50);
    assert.equal(await evaluate('sphere.dataset.hitControlsHidden'),'true','Player compaction resumes after a confirmation closes');
    await moveIn();await evaluate("sphere.dataset.hitCompactDelay='10000'");await moveOut();await wait(80);await expanded();
    await evaluate("sphere.dataset.hitCompactDelay='0'");await wait(40);
    assert.equal(await evaluate('sphere.dataset.hitControlsHidden'),'true','Changing the delay applies to a pending departure');
    await moveIn();await expanded();
    for(const view of ['player','lyrics']){
        await evaluate(`document.querySelector('.audio-menu').dataset.hitView='${view}';document.querySelector('.audio-menu').dataset.hitHideControls='false'`);
        await moveOut();await wait(60);await expanded();
        await evaluate("document.querySelector('.audio-menu').dataset.hitHideControls='true'");await wait(60);
        assert.equal(await evaluate('sphere.dataset.hitControlsHidden'),'true',view+' compaction follows its own Hide buttons preference');
        await moveIn();await expanded();
    }
    for(const [className,view]of [['chess-menu','board'],['clock-menu','main']]){
        await evaluate(`panel.innerHTML='<section class="${className}" data-hit-view="${view}" style="width:360px;height:200px">Content</section>';panel.firstElementChild.addEventListener('hit-menu-idle',()=>{calls.push('${className}');panel.firstElementChild.dataset.hitView='compact'})`);
        await moveIn();await moveOut();await wait(60);
        assert.equal(await evaluate('calls.at(-1)'),className,'The allowed main view receives compaction');
    }
    await evaluate("panel.innerHTML=player");const point=await moveIn();const before=await evaluate('devicePixelRatio');
    await command('Input.dispatchMouseEvent',{type:'mouseWheel',...point,deltaX:0,deltaY:-200,modifiers:2});await wait(40);
    assert.equal(await evaluate('devicePixelRatio'),before);assert.equal(await evaluate('wheels'),0);
    for(const key of ['+','-','=','0'])assert.equal(await evaluate(`window.dispatchEvent(new KeyboardEvent('keydown',{key:${JSON.stringify(key)},ctrlKey:true,bubbles:true,cancelable:true}))`),false);
    await evaluate('disconnectChrome()');assert.deepEqual(await evaluate('errors'),[]);
    console.log('PASS: Settings/configuration/other menus never compact; only player/lyrics, board and main clock; departure timing, modal/hover protection, icon tooltip and zoom guards.');
}finally{await browser.close();}
