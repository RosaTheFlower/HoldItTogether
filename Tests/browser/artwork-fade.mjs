import assert from 'node:assert/strict';
import {mkdirSync,writeFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';
const root=resolve('obj/artwork-fade-browser');mkdirSync(root,{recursive:true});
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><head><link rel="stylesheet" href="../../wwwroot/css/audio-shell.css">
<style>.dynamic-sphere{position:relative;width:360px;height:300px;overflow:hidden}.sphere-audio-backdrop{--audio-cover-opacity:.8}</style></head>
<body><div class="sphere-area"><div id="sphere" class="dynamic-sphere is-open"><div class="sphere-audio-backdrop"></div><div id="audio"></div></div></div>
<script type="module">import * as player from '../../wwwroot/js/audio-player.js';import * as themes from '../../wwwroot/js/hit-themes.js';window.player=player;window.themes=themes;
const canvas=document.createElement('canvas');canvas.width=canvas.height=10;const ctx=canvas.getContext('2d');ctx.fillStyle='#446688';ctx.fillRect(0,0,10,10);window.cover=canvas.toDataURL();window.show=()=>player.syncScene(audio,cover,true,true,false,{});window.ready=true;</script></body></html>`);
const browser=await openBrowser(root),{evaluate}=browser;
const opacity=()=>evaluate("Number(getComputedStyle(document.querySelector('.sphere-audio-backdrop')).opacity)");
try{
    await evaluate('show()');
    await wait(180);
    const incoming=await opacity();assert.ok(incoming>0&&incoming<.8,'Cover must fade in rather than jump to full opacity');
    await wait(650);assert.ok(await opacity()>.79);
    await evaluate('window.outgoing=player.clearScene(audio);undefined');
    await wait(180);
    const outgoing=await opacity();assert.ok(outgoing>0&&outgoing<.8,'Cover must fade out before release');
    assert.equal(await evaluate('sphere.dataset.audioCover'),'true','Keep the existing image while fading out');
    await evaluate('window.outgoing');
    await wait(700);
    assert.equal(await evaluate('sphere.dataset.audioCover'),undefined,'Release the cover at the end of the fade');
    await evaluate('show()');await wait(700);
    await evaluate('window.oldClear=player.clearScene(audio);undefined');await wait(120);
    await evaluate('show()');await wait(750);
    assert.equal(await evaluate('sphere.dataset.audioCover'),'true','A pending clear must not erase a rapid reopen');
    assert.ok(await opacity()>.79);
    await evaluate('player.clearScene(audio,true);themes.setMotion(true);show()');
    assert.equal(await evaluate("document.querySelector('.sphere-audio-backdrop').getAnimations().length"),0,'Remove animations disables the fade');
    await evaluate('player.clearScene(audio,true)');
    console.log('PASS: artwork fade in/out, deferred image release, rapid reopen and disabled motion.');
}finally{await browser.close();}
