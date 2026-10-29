import assert from 'node:assert/strict';
import {mkdirSync,writeFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {inflateSync} from 'node:zlib';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/spectrum-composition');mkdirSync(root,{recursive:true});
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><head>
<link rel="stylesheet" href="../../wwwroot/css/menus/HitMenuAudio.css">
<style>body{margin:0}.audio-menu{width:400px!important;height:240px;--audio-scene-inset-top:0px;--audio-scene-inset-side:0px;--audio-scene-inset-bottom:0px}
.audio-spectrum{inset:0!important}.audio-scene{background:var(--test-background)!important;transition:none!important}button{position:relative;z-index:1}
.audio-player-content{position:absolute!important;left:40px;top:40px;width:120px;height:30px;background:rgb(17,31,47)}</style></head><body>
<section class="hit-menu-panel" data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAudio">
<div id="root" class="audio-menu" data-spectrum-style="bars" data-spectrum-blend="normal" style="--audio-visualizer-opacity:1"><div class="audio-scene"></div><button onclick="window.clicks++">Play</button><div class="audio-player-content"></div>
<div class="audio-spectrum-layer"><div class="audio-spectrum"><canvas width="400" height="240"></canvas></div></div></div></section>
<script type="module">import * as spectrum from '../../wwwroot/js/audio-spectrum.js';window.spectrumApi=spectrum;window.clicks=0;const context=document.querySelector('canvas').getContext('2d');context.fillStyle='rgb(180,90,50)';context.fillRect(0,0,400,240);window.ready=true;</script></body></html>`);
function pixel(png){
    const buffer=Buffer.from(png,'base64'),chunks=[];
    for(let offset=8;offset<buffer.length;){const length=buffer.readUInt32BE(offset),type=buffer.toString('ascii',offset+4,offset+8);if(type==='IDAT')chunks.push(buffer.subarray(offset+8,offset+8+length));offset+=length+12;}
    return [...inflateSync(Buffer.concat(chunks)).subarray(1,4)];
}
const modes={normal:(_b,s)=>s,invert:b=>255-b,difference:(b,s)=>Math.abs(b-s),
    overlay:(b,s)=>b<128?2*b*s/255:255-2*(255-b)*(255-s)/255,
    screen:(b,s)=>255-(255-b)*(255-s)/255,multiply:(b,s)=>b*s/255,exclusion:(b,s)=>b+s-2*b*s/255};
const browser=await openBrowser(root),{evaluate,command}=browser;
try{
    for(const background of [[60,100,140],[15,15,15],[230,230,230]]){
        for(const [mode,blend] of Object.entries(modes)){
            await evaluate(`root.style.setProperty('--test-background','rgb(${background.join(',')})');root.dataset.spectrumBlend='${mode}'`);await wait(40);
            const screenshot=await command('Page.captureScreenshot',{format:'png',clip:{x:200,y:160,width:1,height:1,scale:1}});
            const actual=pixel(screenshot.data),expected=background.map((b,i)=>Math.round(blend(b,[180,90,50][i])));
            assert.ok(actual.every((v,i)=>Math.abs(v-expected[i])<=2),`${mode}: ${actual} should match ${expected}`);
            const ui=await command('Page.captureScreenshot',{format:'png',clip:{x:60,y:50,width:1,height:1,scale:1}});
            assert.deepEqual(pixel(ui.data),[17,31,47],`${mode} blends above the cover but never above player content`);
            assert.equal(await evaluate("getComputedStyle(document.querySelector('.audio-spectrum-layer')).pointerEvents"),'none');
        }
    }
    await evaluate("root.dataset.spectrumBlend='normal';root.classList.add('has-spectrum');root.dataset.audioPalette='true';root.style.setProperty('--test-background','rgb(60,100,140)')");
    for(const opacity of [0,.25,1]){
        await evaluate(`root.style.setProperty('--audio-visualizer-opacity',${opacity})`);await wait(40);
        const screenshot=await command('Page.captureScreenshot',{format:'png',clip:{x:200,y:160,width:1,height:1,scale:1}});
        const expected=[60,100,140].map((b,i)=>Math.round(b*(1-opacity)+[180,90,50][i]*opacity));
        assert.ok(pixel(screenshot.data).every((v,i)=>Math.abs(v-expected[i])<=2),'Opacity applies directly even with an artwork palette');
    }
    await command('Input.dispatchMouseEvent',{type:'mousePressed',x:18,y:10,button:'left',clickCount:1});
    await command('Input.dispatchMouseEvent',{type:'mouseReleased',x:18,y:10,button:'left',clickCount:1});
    assert.equal(await evaluate('clicks'),1,'Composition must not intercept player controls');
    await evaluate("root.style.setProperty('--audio-art-foreground','rgb(201,202,203)')");
    for(const opacity of [0,37,100]){
        await evaluate(`spectrumApi.configureSpectrum(root.querySelector('.audio-spectrum'),'bars',100,100,0,false,false,${opacity})`);await wait(30);
        assert.equal(await evaluate("Number(getComputedStyle(root.querySelector('.audio-spectrum')).opacity)"),opacity/100,'Runtime opacity updates the actual style without root/palette replacement');
        assert.equal(await evaluate("root.style.getPropertyValue('--audio-art-foreground')"),'rgb(201,202,203)');
    }
    await evaluate("spectrumApi.disconnectSpectrum(root.querySelector('.audio-spectrum'))");
    console.log('PASS: blend formulas over the cover, preserved UI pixels, independent opacity with artwork palette and pointer passthrough.');
}finally{await browser.close();}
