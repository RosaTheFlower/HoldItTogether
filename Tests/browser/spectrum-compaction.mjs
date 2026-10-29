import assert from 'node:assert/strict';
import {mkdirSync,readFileSync,writeFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/spectrum-compaction');mkdirSync(root,{recursive:true});
const player=readFileSync('obj/menu-tests/player-spectrum.html','utf8');
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><head>
${['app.css','css/dynamic-sphere.css','css/hit-menus.css','css/menus.css','css/theme-base.css','css/audio-shell.css','css/hit-controls.css'].map(file=>`<link rel="stylesheet" href="../../wwwroot/${file}">`).join('')}
<style>.sphere-area{position:absolute;left:30px;top:30px}.dynamic-sphere{width:max-content!important;height:auto!important}.sphere-content{position:relative!important;opacity:1!important;visibility:visible!important}.hit-menu-host{position:relative}</style></head><body>
<div class="sphere-area"><div id="sphere" class="dynamic-sphere is-open is-pinned" data-hit-layout-animating="true" style="--hit-focus-duration:500ms">
<div class="sphere-content"><header class="sphere-header"><h1>Audio</h1></header><div class="hit-menu-host"><div class="hit-menu-measure"><section class="hit-menu-panel" data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAudio">${player}</section></div></div></div></div></div>
<script type="module">
import * as audio from '../../wwwroot/js/audio-spectrum.js';
window.audio=audio;window.surface=document.querySelector('.audio-spectrum');window.canvas=surface.querySelector('canvas');
audio.configureSpectrum(surface,'wave',100,100);audio.renderSpectrum(surface,Array(32).fill(.6));
window.errors=[];addEventListener('error',e=>errors.push(e.message));
window.samples=[];window.observer=new ResizeObserver(()=>{
    if(!canvas.width||!canvas.height)return;
    const pixels=canvas.getContext('2d').getImageData(0,0,canvas.width,canvas.height).data;
    let ink=false;for(let i=3;i<pixels.length;i+=4)if(pixels[i]){ink=true;break;}
    samples.push({width:canvas.width,height:canvas.height,ink});
});observer.observe(canvas);
window.compact=small=>{sphere.dataset.hitControlsHidden=String(small);sphere.dataset.hitIdleView='player';};
window.topInset=()=>parseFloat(getComputedStyle(document.querySelector('.audio-scene')).top);
window.ready=true;
</script></body></html>`);
const browser=await openBrowser(root),{evaluate}=browser;
try{
    await wait(400);await evaluate('samples.length=0');
    const full=await evaluate('canvas.height');
    const before=await evaluate('topInset()');
    const after=await evaluate('compact(true);topInset()');
    assert.ok(Math.abs(after-before)<2,'Scene inset follows animated progress instead of jumping on the compact flag');
    await wait(650);
    const compact=await evaluate('canvas.height');
    assert.ok(compact<full,'The retained canvas follows the real shrinking player');
    await evaluate('compact(false)');await wait(650);
    assert.equal(await evaluate('canvas.height'),full,'Expansion restores the original canvas dimensions');
    await evaluate('compact(true)');await wait(140);await evaluate('compact(false)');await wait(650);
    const samples=await evaluate('samples');
    assert.ok(samples.length>10,'Exercise multiple actual ResizeObserver deliveries throughout compact/expand');
    assert.ok(samples.every(sample=>sample.ink),'Every resized backing buffer is painted in the same observer cycle');
    assert.ok(new Set(samples.map(s=>s.height)).size>10,'The canvas follows intermediate geometry, not just endpoints');
    assert.equal(await evaluate('canvas===surface.querySelector("canvas")'),true,'Compaction retains the same canvas node');
    assert.deepEqual(await evaluate('errors'),[]);
    await evaluate('observer.disconnect();audio.disconnectSpectrum(surface)');
    assert.equal(await evaluate('canvas.width'),0,'Final teardown still releases the buffer');
    console.log('PASS: actual Audio compaction/expansion, continuous inset, intermediate sizes, same-cycle nonempty buffers, interrupted transition and disposal.');
}finally{await browser.close();}
