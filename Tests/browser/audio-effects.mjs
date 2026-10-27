import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/audio-effects-browser');mkdirSync(root,{recursive:true});
writeFileSync(resolve(root,'index.html'),`<!doctype html><html lang="en"><head><meta charset="utf-8">
<link rel="stylesheet" href="../../wwwroot/css/audio-shell.css"><style>body{background:#444}.dynamic-sphere{position:relative;width:400px;height:260px;overflow:hidden;border-radius:24px}.sphere-audio-backdrop{opacity:1}.audio-menu{position:relative}.sphere-header{position:relative}</style></head><body>
<div class="sphere-area"><div id="sphere" class="dynamic-sphere is-open is-pinned" data-audio-cover="true"><div class="sphere-audio-backdrop"></div><header class="sphere-header">Audio</header><section class="hit-menu-panel"><div class="audio-menu" data-hit-view="player" data-hit-hide-controls="true"><button data-hit-idle-controls>Lyrics</button></div></section></div></div>
<script type="module">import * as effects from '../../wwwroot/js/audio-effects.js';import {connectMenuChrome} from '../../wwwroot/js/hit-menu-chrome.js';
window.effects=effects;window.disconnectChrome=connectMenuChrome(sphere);window.errors=[];addEventListener('error',e=>errors.push(e.message));
const canvas=document.createElement('canvas');canvas.width=canvas.height=200;const x=canvas.getContext('2d');for(let i=0;i<10;i++){x.fillStyle=['#bd4525','#65a9b0','#e8dabc'][i%3];x.fillRect(i*20,0,20,200)}sphere.style.setProperty('--audio-artwork','url('+canvas.toDataURL()+')');window.ready=true;</script></body></html>`);
const browser=await openBrowser(root),{evaluate,command}=browser;
try {
    assert.equal(await evaluate(`(()=>{const beat=new effects.BeatEnvelope();for(let t=0;t<2000;t+=50)beat.feed(Array(32).fill(.5),t);return beat.value(2000)})()`),0,'Steady audio does not generate artificial beats');
    const envelope=await evaluate(`(()=>{const beat=new effects.BeatEnvelope();beat.feed(Array(32).fill(0),0);beat.feed(Array(32).fill(.8),300);return [beat.value(300),beat.value(385),beat.value(650),beat.value(1100)]})()`);
    assert.deepEqual(envelope.slice(0,2),[0,1]);assert.ok(envelope[2]>0&&envelope[2]<.3);assert.equal(envelope[3],0,'Envelope settles fully between beats');
    await evaluate("effects.configureEffects(sphere,{blur:12,saturation:10,brightness:180,temperature:90})");
    assert.equal(await evaluate("document.querySelector('.audio-artwork-optics').style.filter"),'blur(12px)','Disabled color adjustments add no filter functions');
    assert.equal(await evaluate("document.querySelectorAll('svg,.audio-artwork-copy,.audio-artwork-lens,.audio-artwork-chromatic,.audio-artwork-grain').length"),1,'Default cover creates one full-quality image layer and no SVG/optional layers');
    await evaluate("effects.configureEffects(sphere,{blur:12,colorEnabled:true,temperature:50})");
    assert.equal(await evaluate("document.querySelectorAll('feTurbulence,feDisplacementMap').length"),0,'Temperature must not construct or run the distortion graph');
    assert.equal(await evaluate("document.querySelectorAll('feColorMatrix').length"),1);
    await evaluate("window.options={blur:12,zoom:145,colorEnabled:true,saturation:40,brightness:120,contrast:130,temperature:40,tint:-20,hue:15,sepia:10,grayscale:5,lens:60,chromatic:8,vignette:50,grain:12,distortion:10};effects.configureEffects(sphere,options)");
    assert.match(await evaluate("document.querySelector('.audio-artwork-optics').style.filter"),/blur\(12px\).*saturate\(0.4\).*brightness\(1.2\)/,'Filters operate independently');
    assert.equal(await evaluate("document.querySelector('feDisplacementMap').getAttribute('scale')"),'10.00');
    assert.ok(await evaluate("Number(document.querySelector('.audio-artwork-vignette').style.opacity)===.5&&Number(document.querySelector('.audio-artwork-grain').style.opacity)===.12"));
    assert.match(await evaluate("document.querySelector('.audio-artwork-lens').style.transform"),/1.3/,'Lens magnifies the image through its feathered region');
    await evaluate("effects.configureEffects(sphere,{...options,radial:true})");
    assert.equal(await evaluate("[...document.querySelectorAll('.audio-artwork-copy')].filter(x=>!x.hidden).length"),7);
    assert.doesNotMatch(await evaluate("document.querySelector('.audio-artwork-optics').style.filter"),/blur\(/,'Radial replaces Gaussian blur');
    await evaluate("effects.configureEffects(sphere,{...options,radial:true,onBeat:true,strength:100});effects.renderAudioEffects(sphere,Array(32).fill(0))");
    assert.equal(await evaluate("document.querySelector('.audio-artwork-optics').style.filter"),'none','Between beats only the radial image stack remains');
    assert.equal(await evaluate("document.querySelector('feDisplacementMap').getAttribute('scale')"),'0.00');
    await wait(210);
    await evaluate("effects.renderAudioEffects(sphere,Array(32).fill(.9))");await wait(105);
    assert.ok(Number(await evaluate("document.querySelector('feDisplacementMap').getAttribute('scale')"))>5,'Real rising bass activates the selected effects');
    assert.ok(await evaluate("!document.querySelector('.sphere-audio-backdrop').style.transform.includes('translateX(0px)')"),'Radial blur shakes sideways on beats');
    await wait(800);
    assert.equal(await evaluate("document.querySelector('feDisplacementMap').getAttribute('scale')"),'0.00','Effects return to the blur baseline after the beat');
    await evaluate("document.documentElement.dataset.hitMotion='off';effects.renderAudioEffects(sphere,Array(32).fill(0));effects.renderAudioEffects(sphere,Array(32).fill(1))");
    assert.equal(await evaluate("document.querySelector('feDisplacementMap').getAttribute('scale')"),'0.00','Reduced motion suppresses beat animation');
    await evaluate("document.documentElement.dataset.hitMotion='on';effects.configureEffects(sphere,options)");
    const shot=await command('Page.captureScreenshot',{format:'png'});writeFileSync(resolve(root,'effects.png'),Buffer.from(shot.data,'base64'));
    await evaluate("effects.configureEffects(sphere,{...options,drift:true});window.changes=[];window.watcher=new MutationObserver(records=>changes.push(...records.filter(r=>r.target!==document.querySelector('.sphere-audio-backdrop'))));watcher.observe(document.querySelector('.sphere-audio-backdrop'),{subtree:true,attributes:true})");
    await wait(250);
    assert.equal(await evaluate('changes.length'),0,'Drift changes only the compositor transform; static filters and hidden layers are untouched');
    assert.equal(await evaluate("document.querySelector('.sphere-audio-backdrop').getAnimations().filter(a=>a.playState==='running').length"),1,'Drift runs as one compositor animation');
    await evaluate('changes=[];watcher.disconnect();window.allChanges=0;watcher=new MutationObserver(records=>allChanges+=records.length);watcher.observe(document.querySelector(".sphere-audio-backdrop"),{subtree:true,attributes:true})');await wait(250);
    assert.equal(await evaluate('allChanges'),0,'Steady drift performs zero per-frame DOM writes');
    await evaluate("watcher.disconnect();window.menu=document.querySelector('.audio-menu');sphere.dataset.hitCompactDelay='0';menu.dispatchEvent(new PointerEvent('pointermove',{bubbles:true,screenX:100,screenY:100}));sphere.dispatchEvent(new PointerEvent('pointerleave',{screenX:100,screenY:100}))");await wait(40);
    assert.equal(await evaluate('sphere.dataset.hitControlsHidden'),'true','Leaving Audio uses the shared compact timer');
    await evaluate("menu.dispatchEvent(new PointerEvent('pointermove',{bubbles:true,screenX:100,screenY:100}))");
    assert.equal(await evaluate('sphere.dataset.hitControlsHidden'),'true','Resize-induced stationary hover does not undo compaction');
    await evaluate("menu.dispatchEvent(new PointerEvent('pointermove',{bubbles:true,screenX:110,screenY:100}))");
    assert.equal(await evaluate('sphere.dataset.hitControlsHidden'),undefined,'Actual pointer movement restores controls');
    await evaluate("effects.disposeEffects(sphere);disconnectChrome()");
    assert.equal(await evaluate("document.querySelectorAll('feDisplacementMap,.audio-artwork-optics').length"),0,'Disposal releases filters, layers and animation');
    assert.deepEqual(await evaluate('errors'),[]);
    console.log('PASS: optical/color effects, radial/real FFT beats, smooth envelope, reduced motion, zero static-filter churn during drift, shared Audio timer and cleanup.');
} finally {await browser.close();}
