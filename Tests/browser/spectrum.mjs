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
    await evaluate(`window.ink=()=>{const c=spectrum.querySelector('canvas'),pixels=c.getContext('2d').getImageData(0,0,c.width,c.height).data;let top=c.height,count=0;for(let y=0;y<c.height;y++)for(let x=0;x<c.width;x++)if(pixels[(y*c.width+x)*4+3]){top=Math.min(top,y);count++;}return {top,count};};spectrumApi.renderSpectrum(spectrum,Array(32).fill(.8))`);
    await wait(350);
    const bars=await evaluate('ink()');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'bars',50)");await wait(80);
    const shortBars=await evaluate('ink()');
    assert.ok(shortBars.top>bars.top+60,'Bar height changes immediately without another FFT update');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'line',100)");await wait(80);
    const line=await evaluate('ink()');
    assert.ok(line.count>0&&line.count<bars.count/4,'Line renders a stroke instead of filled bars');
    assert.ok(Math.abs(line.top-bars.top)<4,'Both styles use the same vertical amplitude');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'line',30)");await wait(80);
    assert.ok((await evaluate('ink()')).top>140,'Line honors the height control');
    assert.equal(await evaluate('writes'),0,'Style and height changes do not mutate DOM');
    await evaluate("observer.disconnect();spectrumApi.disconnectSpectrum(spectrum);spectrumApi.renderSpectrum(spectrum,Array(32).fill(.8))");await wait(350);
    const resumed=await evaluate('ink()');
    assert.ok(resumed.top>140&&resumed.count<bars.count/4,'Recreated buffers retain line style and height after sleep/scene release');
    await evaluate("spectrumApi.renderSpectrum(spectrum,Array.from({length:32},(_,i)=>.1+(i%7)/10))");await wait(350);
    const shapes=new Set();
    for(const style of ['bars','line','smooth','mirrored','radial','ring']){
        await evaluate(`spectrumApi.configureSpectrum(spectrum,'${style}',50)`);await wait(60);
        const small=await evaluate('ink()');
        await evaluate(`spectrumApi.configureSpectrum(spectrum,'${style}',200)`);await wait(60);
        const large=await evaluate('ink()');
        assert.ok(large.count>0,`${style} draws actual spectrum data`);
        assert.ok(large.top<small.top-5,`${style} supports amplitude above 100 percent`);
        assert.equal(await evaluate('spectrum.getBoundingClientRect().height'),200,'Amplification never resizes the player');
        shapes.add(JSON.stringify(large));
        await evaluate('spectrumApi.disconnectSpectrum(spectrum);spectrumApi.renderSpectrum(spectrum,Array.from({length:32},(_,i)=>.1+(i%7)/10))');await wait(350);
        assert.deepEqual(await evaluate('ink()'),large,`${style} and 200 percent survive buffer release`);
    }
    assert.equal(shapes.size,6,'Remaining spectrum styles produce distinct images from the same input');
    await command('Emulation.setEmulatedMedia',{features:[{name:'prefers-reduced-motion',value:'reduce'}]});
    await evaluate("spectrumApi.configureSpectrum(spectrum,'wave',50);spectrumApi.renderSpectrum(spectrum,Array(32).fill(.8))");await wait(80);
    const smallWave=await evaluate('ink()');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'wave',200)");await wait(80);
    assert.ok((await evaluate('ink()')).top<smallWave.top-20,'Wave has a continuous audio-driven height envelope');
    await evaluate('spectrumApi.renderSpectrum(spectrum,Array(32).fill(0))');await wait(80);
    assert.ok((await evaluate('ink()')).top>=98,'Wave collapses to the central baseline in silence');
    const waves=new Set();
    for(const style of ['wave','wavesoft','waveribbons','wavepulse']){
        for(const size of [100,200,300,400,500]){
            await evaluate(`spectrumApi.configureSpectrum(spectrum,'${style}',100,${size});spectrumApi.renderSpectrum(spectrum,Array(32).fill(.8))`);await wait(50);
            const edges=await evaluate(`(()=>{const c=spectrum.querySelector('canvas'),p=c.getContext('2d').getImageData(0,0,c.width,c.height).data;let left=0,right=0;for(let y=0;y<c.height;y++){left+=p[(y*c.width)*4+3];right+=p[(y*c.width+c.width-1)*4+3];}return {left,right};})()`);
            assert.ok(edges.left>0&&edges.right>0,`${style} spans both edges at ${size}%`);
            if(size===100)waves.add(JSON.stringify(await evaluate('ink()')));
        }
    }
    assert.equal(waves.size,4,'Wave variants draw distinct curves');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'ring',30,100,0);spectrumApi.renderSpectrum(spectrum,Array(32).fill(.3))");await wait(50);
    const centered=await evaluate('ink()');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'ring',30,100,25)");await wait(50);
    const shifted=await evaluate('ink()');
    assert.ok(Math.abs(shifted.top-centered.top-50)<=1,'Y is a percentage of the menu drawing height and independent of zoom');
    await evaluate('spectrumApi.disconnectSpectrum(spectrum);spectrumApi.renderSpectrum(spectrum,Array(32).fill(.3))');await wait(50);
    assert.deepEqual(await evaluate('ink()'),shifted,'Y survives buffer release');
    await evaluate(`window.centroid=()=>{const c=spectrum.querySelector('canvas'),p=c.getContext('2d').getImageData(0,0,c.width,c.height).data;let mass=0,xSum=0,ySum=0;for(let y=0;y<c.height;y++)for(let x=0;x<c.width;x++){const a=p[(y*c.width+x)*4+3];mass+=a;xSum+=a*x;ySum+=a*y;}return {x:xSum/mass,y:ySum/mass};};spectrumApi.configureSpectrum(spectrum,'bars',30,100,0,false,false);spectrumApi.renderSpectrum(spectrum,Array.from({length:32},(_,i)=>.1+i/40))`);await wait(50);
    const upright=await evaluate('centroid()');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'bars',30,100,0,true,false)");await wait(50);
    const flipX=await evaluate('centroid()');
    assert.ok(Math.abs(upright.x+flipX.x-399)<1,'X flips the actual asymmetric spectrum horizontally');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'bars',30,100,0,true,true)");await wait(50);
    const ceiling=await evaluate('centroid()');
    assert.ok(Math.abs(upright.y+ceiling.y-199)<1&&ceiling.y<30,'Y anchors the bars at the ceiling');
    assert.ok(Math.abs(ceiling.x-flipX.x)<1,'Y preserves the independent X orientation');
    await evaluate("spectrumApi.configureSpectrum(spectrum,'bars',30,100,20,true,true)");await wait(50);
    const movedCeiling=await evaluate('centroid()');
    assert.ok(Math.abs(movedCeiling.y-ceiling.y-40)<1,'Positive Y still moves downward after inversion');
    await evaluate('spectrumApi.disconnectSpectrum(spectrum);spectrumApi.renderSpectrum(spectrum,Array.from({length:32},(_,i)=>.1+i/40))');await wait(50);
    assert.deepEqual(await evaluate('centroid()'),movedCeiling,'Both flips and Y are retained through buffer release');
    for(const style of ['radial','ring']){
        await evaluate(`spectrumApi.configureSpectrum(spectrum,'${style}',30,100);spectrumApi.renderSpectrum(spectrum,Array(32).fill(.3))`);await wait(80);
        const normalSize=await evaluate('ink()');
        await evaluate(`spectrumApi.configureSpectrum(spectrum,'${style}',30,500)`);await wait(80);
        const enlarged=await evaluate('ink()');
        assert.ok(enlarged.top<normalSize.top-40,`${style}: size enlarges the base radius independently of amplitude`);
        assert.equal(await evaluate('spectrum.getBoundingClientRect().height'),200,'500 percent size stays inside existing native bounds');
        await evaluate('spectrumApi.disconnectSpectrum(spectrum);spectrumApi.renderSpectrum(spectrum,Array(32).fill(.3))');await wait(80);
        assert.deepEqual(await evaluate('ink()'),enlarged,'Independent 500 percent size survives released buffers');
    }
    await evaluate("spectrumApi.configureSpectrum(spectrum,'ring',200);spectrumApi.renderSpectrum(spectrum,Array(32).fill(0))");await wait(80);
    const silentRing=await evaluate('ink()');
    await evaluate('spectrumApi.renderSpectrum(spectrum,Array(32).fill(1))');await wait(80);
    assert.ok((await evaluate('ink()')).top<silentRing.top-50,'Reduced motion still follows real input and silence contracts to the baseline');
    await command('Emulation.setEmulatedMedia',{features:[]});
    await command('Emulation.setDeviceMetricsOverride',{width:1200,height:1100,deviceScaleFactor:2,mobile:false});
    await evaluate('spectrumApi.refreshSpectrum(spectrum)');await wait(30);
    assert.equal(await evaluate("document.querySelector('canvas').width"),800,'Canvas preserves native DPI resolution');
    await evaluate("spectrum.style.transform='scale(.25)';spectrum.style.transformOrigin='top left';spectrumApi.refreshSpectrum(spectrum)");
    assert.equal(await evaluate("document.querySelector('canvas').width"),200,'Audio scaling keeps the appropriate physical resolution');
    await evaluate('observer.disconnect();spectrumApi.disconnectSpectrum(spectrum)');
    assert.equal(await evaluate("document.querySelector('canvas').width"),0,'Disposal releases the drawing buffer');
    assert.deepEqual(await evaluate('errors'),[]);
    console.log('PASS: six spectrum styles, four full-width Wave variants, 100-500% size, Y, retained options, reduced motion, DPI and disposal.');
}finally{await browser.close();}
