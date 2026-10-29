import assert from 'node:assert/strict';
import {mkdirSync,writeFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/resource-saving-browser');mkdirSync(root,{recursive:true});
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><head>
<link rel="stylesheet" href="../../wwwroot/css/dynamic-sphere.css">
<style>@keyframes resource-test{from{opacity:.2}to{opacity:.8}}#moving{animation:resource-test 1s infinite alternate}.audio-spectrum{width:300px;height:100px}.audio-spectrum canvas{width:100%;height:100%}</style>
</head><body><div class="sphere-area" id="area"><div class="dynamic-sphere"><div id="moving">HIT</div><div id="audio"><div class="audio-spectrum"><canvas></canvas></div></div></div></div>
<script type="module">import * as sphere from '../../wwwroot/js/dynamic-sphere.js';import * as audio from '../../wwwroot/js/audio-player.js';window.api={sphere,audio};window.ready=true;</script></body></html>`);
const browser=await openBrowser(root),{evaluate}=browser;
try{
    await evaluate("api.sphere.setPointer(area,.5,.5);area.dataset.hitResourceSleep='true'");
    assert.equal(await evaluate("getComputedStyle(document.querySelector('#moving')).animationPlayState"),'paused');
    await wait(80); // Allow the compositor to settle on the paused frame.
    const opacity=await evaluate("getComputedStyle(document.querySelector('#moving')).opacity");
    await wait(150);
    assert.equal(await evaluate("getComputedStyle(document.querySelector('#moving')).opacity"),opacity);
    await evaluate("api.sphere.setPointer(area,-.5,-.5)");
    assert.equal(await evaluate("document.querySelector('.dynamic-sphere').style.getPropertyValue('--hit-pointer-x')"),'0.5');
    await evaluate("area.dataset.hitResourceSleep='false';api.sphere.setPointer(area,-.5,-.5)");
    assert.equal(await evaluate("getComputedStyle(document.querySelector('#moving')).animationPlayState"),'running');
    assert.equal(await evaluate("document.querySelector('.dynamic-sphere').style.getPropertyValue('--hit-pointer-x')"),'-0.5');
    await evaluate("api.audio.syncSpectrum(audio);api.audio.renderAudioFrame(audio,document.querySelector('.audio-spectrum'),Array(32).fill(.8))");
    await wait(100);
    assert.ok(await evaluate("document.querySelector('canvas').width>0"));
    await evaluate("api.audio.clearScene(audio)");
    assert.equal(await evaluate("document.querySelector('canvas').width"),0);
    await evaluate("api.audio.renderAudioFrame(audio,document.querySelector('.audio-spectrum'),Array(32).fill(.6))");
    await wait(100);
    assert.ok(await evaluate("document.querySelector('canvas').width>0"));
    await evaluate("api.audio.clearScene(audio)");
    console.log('PASS: sleeping CSS animation pause, blocked pointer writes, wake and spectrum buffer release/recreation.');
}finally{await browser.close();}
