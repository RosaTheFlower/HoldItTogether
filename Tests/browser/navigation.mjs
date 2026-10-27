import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/navigation-browser');mkdirSync(root,{recursive:true});
const settings=readFileSync('obj/menu-tests/settings.html','utf8'),player=readFileSync('obj/menu-tests/player.html','utf8');
writeFileSync(resolve(root,'index.html'),`<!doctype html><html lang="en"><head><meta charset="utf-8">
${['app.css','css/dynamic-sphere.css','css/hit-menus.css','css/menus.css','css/theme-base.css','css/audio-shell.css','css/hit-controls.css'].map(file=>`<link rel="stylesheet" href="../../wwwroot/${file}">`).join('')}
<style>body{background:#2ca7df}.sphere-area{position:absolute;left:30px;top:30px}</style></head><body>
<div class="sphere-area" id="area"><div class="dynamic-sphere is-away" id="sphere"><div class="sphere-audio-backdrop"></div><div class="sphere-content"><header class="sphere-header"><h1 class="sphere-title">Settings</h1></header><div class="hit-menu-host"><div class="hit-menu-measure">
<section class="hit-menu-panel" data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuSettings">${settings}</section>
<section class="hit-menu-panel" data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAudio" hidden>${player}</section>
<section class="hit-menu-panel" data-hit-menu="clock-fixture" hidden><div style="width:360px;height:180px">Clock</div></section>
</div></div></div></div></div>
<script type="module">import * as themes from '../../wwwroot/js/hit-themes.js';import * as shell from '../../wwwroot/js/dynamic-sphere.js';import * as viewport from '../../wwwroot/js/hit-menu-viewport.js';
window.themes=themes;window.shell=shell;window.calls=[];window.errors=[];window.samples=[];window.active=0;window.phase='closed';window.initialAlignment=null;window.zoomResets=0;
addEventListener('error',event=>errors.push(event.message));themes.setMotion(false);viewport.observe(document.querySelector('.hit-menu-viewport'));
shell.observe(area,{async invokeMethodAsync(name,...args){if(name==='PrepareSurface'){zoomResets++;return{x:1,y:1}}if(name==='ResizeWindow'){initialAlignment??=[area.style.getPropertyValue('--sphere-align-x'),area.style.getPropertyValue('--sphere-align-y')];calls.push({phase,width:args[0],height:args[1],left:args[3],top:args[4],surfaceWidth:args[5],surfaceHeight:args[6]});await new Promise(resolve=>setTimeout(resolve,25));area.style.left=(1050-args[0])+'px';area.style.top=(970-args[1])+'px';return{x:1,y:1}}}});
sphere.addEventListener('wheel',event=>{event.preventDefault();active=(active+1)%3;document.querySelectorAll('.hit-menu-panel').forEach((panel,index)=>panel.hidden=index!==active);document.querySelector('.sphere-title').textContent=['Settings','Audio','Clock'][active]});
window.sampling=false;window.sample=()=>{if(!sampling)return;const panel=document.querySelector('.hit-menu-panel:not([hidden])'),style=getComputedStyle(sphere);samples.push({open:sphere.classList.contains('is-open'),background:style.backgroundColor,content:Number(getComputedStyle(document.querySelector('.sphere-content')).opacity),panel:Number(getComputedStyle(panel).opacity),animation:getComputedStyle(panel).animationName});requestAnimationFrame(sample)};
window.ready=true;</script></body></html>`);

const browser=await openBrowser(root),{evaluate,command}=browser;
try {
    await wait(80);
    assert.deepEqual(await evaluate('initialAlignment'),['end','end'],'The corner anchor is ready before the first native resize/reveal');
    assert.equal(await evaluate('area.hasAttribute("data-hit-geometry-pending")'),false,'Initial geometry becomes visible after native placement');
    for(const file of ['HIT-OBSIDIAN-EYES.css','HIT-OLED-OFF-WHITE.css','HIT-LIGHT.css']) {
        await evaluate(`themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/'+file,'utf8'))});sphere.className='dynamic-sphere is-open is-pinned';document.querySelectorAll('.hit-menu-panel').forEach((panel,index)=>panel.hidden=index!==0);active=0`);
        await wait(850);
        await evaluate("calls=[];samples=[];phase='scroll';sampling=true;requestAnimationFrame(sample)");
        const point=await evaluate("(()=>{const r=sphere.getBoundingClientRect();return{x:r.left+60,y:r.top+80}})()");
        for(let index=0;index<12;index++) {await command('Input.dispatchMouseEvent',{type:'mouseWheel',...point,deltaX:0,deltaY:120});await wait(35);}
        await wait(720);await evaluate('sampling=false');
        const result=await evaluate("({frames:samples.length,bad:samples.filter(s=>!s.open||s.content<.99||s.panel<.99||s.animation!=='none'),sizes:[...new Set(calls.map(c=>c.width+','+c.height))]})");
        assert.ok(result.frames>10);assert.deepEqual(result.bad,[],'Navigation must never fade content to the empty background in '+file);assert.ok(result.sizes.length<=1,'Wheel navigation retains one native canvas: '+JSON.stringify(result));
    }
    await evaluate("area.style.setProperty('--hit-open-background','rgba(17,19,23,.15)')");await wait(750);
    assert.deepEqual(await evaluate("(()=>{const c=document.createElement('canvas');c.width=c.height=1;const x=c.getContext('2d');x.fillStyle=getComputedStyle(sphere).backgroundColor;x.fillRect(0,0,1,1);return [...x.getImageData(0,0,1,1).data]})()"),[17,19,23,255],'An active surface does not reveal the desktop through an alpha theme');
    await evaluate("sphere.className='dynamic-sphere is-away'");await wait(850);
    assert.equal(await evaluate('area.hasAttribute("data-hit-canvas")'),false,'Closing releases the retained native canvas');
    await evaluate('shell.disconnect(area)');assert.deepEqual(await evaluate('errors'),[]);
    assert.ok(await evaluate('zoomResets')>=2,'Startup and opening both request 100% native page zoom');
    console.log('PASS: rapid wheel navigation in three themes retains opaque content and a stable native canvas; closing releases it.');
} finally {await browser.close();}
