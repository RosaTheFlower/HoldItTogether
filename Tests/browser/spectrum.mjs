import assert from 'node:assert/strict';
import {mkdirSync,writeFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';
const root=resolve('obj/spectrum-browser');mkdirSync(root,{recursive:true});
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><head><style>#spectrum{position:relative;width:400px;height:200px}canvas{position:absolute;inset:0;width:100%;height:100%;color:rgb(210,130,80)}</style></head><body><div id="spectrum"><canvas></canvas></div><script type="module">import * as spectrum from '../../wwwroot/js/audio-spectrum.js';window.spectrumApi=spectrum;window.errors=[];addEventListener('error',e=>errors.push(e.message));window.ready=true;</script></body></html>`);
const browser=await openBrowser(root),{evaluate,command}=browser;
try{
    await evaluate('spectrumApi.renderSpectrum(spectrum,Array(32).fill(.8))');await wait(350);
    assert.equal(await evaluate("document.querySelectorAll('#spectrum > *').length"),1,'One surface replaces 32 animated elements');
    assert.ok(await evaluate("document.querySelector('canvas').getContext('2d').getImageData(5,100,1,1).data[3]>0"),'Actual FFT levels are drawn');
    await evaluate("window.writes=0;window.observer=new MutationObserver(records=>writes+=records.length);observer.observe(spectrum,{subtree:true,attributes:true,childList:true})");
    for(let index=0;index<8;index++){await evaluate(`spectrumApi.renderSpectrum(spectrum,Array.from({length:32},(_,i)=>(i%8+${index%3})/10))`);await wait(20);}
    await wait(350);assert.equal(await evaluate('writes'),0,'Live spectrum updates produce no DOM mutations');
    await command('Emulation.setDeviceMetricsOverride',{width:1200,height:1100,deviceScaleFactor:2,mobile:false});
    await evaluate('spectrumApi.refreshSpectrum(spectrum)');await wait(30);
    assert.equal(await evaluate("document.querySelector('canvas').width"),800,'Canvas preserves native DPI resolution');
    await evaluate("spectrum.style.transform='scale(.25)';spectrum.style.transformOrigin='top left';spectrumApi.refreshSpectrum(spectrum)");
    assert.equal(await evaluate("document.querySelector('canvas').width"),200,'Audio scaling keeps the appropriate physical resolution');
    await evaluate('observer.disconnect();spectrumApi.disconnectSpectrum(spectrum)');
    assert.equal(await evaluate("document.querySelector('canvas').width"),0,'Disposal releases the drawing buffer');
    assert.deepEqual(await evaluate('errors'),[]);
    console.log('PASS: one full-DPI spectrum canvas, real-band rendering, zero frame DOM mutations, content scaling and buffer/listener disposal.');
}finally{await browser.close();}
