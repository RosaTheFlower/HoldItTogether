import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {inflateSync} from 'node:zlib';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/menu-tests/browser');mkdirSync(root,{recursive:true});
const views=Object.fromEntries(['settings','placement','info','info-data','themes','artwork','effects','colors','audio-controls','audio-settings','lyrics','lyrics-title','lyrics-source','lyrics-hidden','player'].map(name=>[name,readFileSync(`obj/menu-tests/${name}.html`,'utf8')]));
writeFileSync(resolve(root,'index.html'),`<!doctype html><html lang="en"><head><meta charset="utf-8">
${['app.css','css/dynamic-sphere.css','css/hit-menus.css','css/menus.css','css/theme-base.css','css/audio-shell.css','css/hit-controls.css'].map(file=>`<link rel="stylesheet" href="../../../wwwroot/${file}">`).join('')}
<style>.sphere-area{position:absolute;left:30px;top:30px}.dynamic-sphere{width:max-content!important;height:auto!important}.sphere-content{position:relative!important;opacity:1!important;visibility:visible!important}.hit-menu-host{position:relative}.audio-menu.test-lyrics{height:280px}.hit-menu-viewport.test-scale{--hit-menu-natural-width:360px;--hit-menu-natural-height:280px}</style>
</head><body><div class="sphere-area"><div id="sphere" class="dynamic-sphere is-open"><div class="sphere-audio-backdrop"></div><div class="sphere-content"><header class="sphere-header"><h1 class="sphere-title">HIT</h1><div class="sphere-actions"><button type="button" class="sphere-action sphere-action-reading-cover" data-hit-reading-cover hidden aria-label="Hide artwork for this screen">Cover</button></div></header><div class="hit-menu-host" data-hit-configuration="false"><div class="hit-menu-measure"><section id="panel" class="hit-menu-panel" data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuSettings"></section></div></div></div></div></div>
<script type="module">
import * as themes from '../../../wwwroot/js/hit-themes.js';import * as audio from '../../../wwwroot/js/audio-player.js';import * as extensions from '../../../wwwroot/js/hit-extensions.js';import * as overflow from '../../../wwwroot/js/hit-overflow.js';import * as viewport from '../../../wwwroot/js/hit-menu-viewport.js';
import {connectReadingBackground} from '../../../wwwroot/js/hit-reading-background.js';
window.themes=themes;window.audio=audio;window.extensions=extensions;window.overflow=overflow;window.viewport=viewport;window.views=${JSON.stringify(views)};window.errors=[];window.calls=[];
window.show=(name,audioView=false)=>{const previous=document.querySelector('.audio-menu');if(previous)audio.clearScene(previous);panel.dataset.hitMenu='HoldItTogether.Components.HitMenus.'+(audioView?'HitMenuAudio':'HitMenuSettings');document.querySelector('.hit-menu-host').dataset.hitConfiguration=String(!name.startsWith('lyrics')&&name!=='player');panel.innerHTML=audioView?'<div class="audio-menu" data-hit-view="'+name+'">'+views[name]+'</div>':views[name]};
window.reference={async invokeMethodAsync(...args){calls.push(args)}};addEventListener('error',event=>errors.push(event.message));themes.setMotion(true);overflow.connect();window.disconnectReading=connectReadingBackground(sphere);window.ready=true;
</script></body></html>`);

const browser=await openBrowser(root),{evaluate,command}=browser;
function pixel(png) {
    const buffer=Buffer.from(png,'base64'),data=[];let colorType;
    for(let offset=8;offset<buffer.length;){const length=buffer.readUInt32BE(offset),type=buffer.toString('ascii',offset+4,offset+8);if(type==='IHDR')colorType=buffer[offset+17];if(type==='IDAT')data.push(buffer.subarray(offset+8,offset+8+length));offset+=length+12;}
    assert.ok(colorType===2||colorType===6);
    const raw=inflateSync(Buffer.concat(data));return [...raw.subarray(1,4)];
}
try {
    for(const file of ['HIT-OBSIDIAN-EYES.css','HIT-OLED-OFF-WHITE.css','HIT-LIGHT.css']) {
        await evaluate(`themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/'+file,'utf8'))});show('settings')`);await wait(60);
        assert.equal(await evaluate("getComputedStyle(document.querySelector('select')).color===getComputedStyle(document.querySelector('.setting-row')).color"),true,'Select and settings labels inherit one theme palette');
        const switchStyle=await evaluate("(()=>{const element=document.querySelector('.switch');element.checked=false;const s=getComputedStyle(element);return {w:s.width,h:s.height,border:s.borderColor,bg:s.backgroundColor}})()");
        assert.ok(await evaluate("(()=>{const label=getComputedStyle(document.querySelector('.setting-compact-delay strong')),other=getComputedStyle(document.querySelector('.setting-row strong'));return label.fontSize===other.fontSize&&label.fontWeight===other.fontWeight&&label.color===other.color})()"),'Compact after matches the readable settings label');
        for(const view of ['artwork','audio-settings','audio-controls','placement']) {
            await evaluate(`show('${view}',${view!=='placement'})`);await wait(30);
            const style=await evaluate("(()=>{const element=document.querySelector('input[type=checkbox]');element.checked=false;const s=getComputedStyle(element);return {w:s.width,h:s.height,border:s.borderColor,bg:s.backgroundColor}})()");
            assert.deepEqual(style,switchStyle,'All menu switches share geometry and theme colors');
            if(view==='placement') {
                assert.deepEqual(await evaluate("(()=>{const range=document.querySelector('input[type=range]');return[range.min,range.max,range.value]})()"),['85','100','98']);
            }
            if(view==='audio-controls'&&file==='HIT-LIGHT.css'){
                const shot=await command('Page.captureScreenshot',{format:'png'});writeFileSync(resolve(root,'audio-controls.png'),Buffer.from(shot.data,'base64'));
            }
        }
        await evaluate("show('audio-settings',true)");
        assert.equal(await evaluate("[...document.querySelectorAll('input[type=range]')].filter(x=>x.max==='200').every(x=>x.min==='25')"),true);
        await evaluate("show('effects',true)");
        assert.equal(await evaluate("document.querySelectorAll('.audio-effect-row').length"),7);
        assert.equal(await evaluate("getComputedStyle(document.querySelector('.audio-effects-scroll')).scrollbarWidth"),'none');
        assert.equal(await evaluate("document.querySelectorAll('input[aria-label=\"Saturation\"]').length"),0,'Colors live on their own screen');
        await evaluate("show('colors',true)");
        assert.equal(await evaluate("document.querySelectorAll('.audio-effect-row').length"),8);
        assert.equal(await evaluate("document.querySelector('[aria-label=\"Enable color adjustments\"]').checked"),false);
        assert.ok(await evaluate("[...document.querySelectorAll('.audio-color-sliders input')].every(x=>x.matches(':disabled'))"),'Color sliders stay inactive until the switch is enabled');
        assert.ok(await evaluate("document.querySelector('.audio-effects-scroll').scrollHeight>document.querySelector('.audio-effects-scroll').clientHeight"),'Color adjustments stay bounded and scrollable');
        if(file==='HIT-LIGHT.css') {
            const shot=await command('Page.captureScreenshot',{format:'png'});writeFileSync(resolve(root,'colors.png'),Buffer.from(shot.data,'base64'));
            await evaluate("show('effects',true)");
            const effectShot=await command('Page.captureScreenshot',{format:'png'});writeFileSync(resolve(root,'effects.png'),Buffer.from(effectShot.data,'base64'));
        }
        await evaluate("show('info')");
        assert.equal(await evaluate("Math.abs(document.querySelector('.info-github').getBoundingClientRect().width-document.querySelector('.info-view').getBoundingClientRect().width)<1"),true);
        await evaluate("show('info-data')");
        assert.ok(await evaluate("[...document.querySelectorAll('.hit-link-row small')].every(s=>{const range=document.createRange();range.selectNode(s.parentElement.firstChild);return s.getBoundingClientRect().top-range.getBoundingClientRect().bottom>=3})"),'Info descriptions sit below their titles with a gap');
        await evaluate("show('themes');document.querySelector('.theme-list').style.maxHeight='65px'");await wait(60);
        assert.equal(await evaluate("getComputedStyle(document.querySelector('.theme-list')).scrollbarWidth"),'none');
        assert.equal(await evaluate("document.querySelector('.theme-list').style.getPropertyValue('--hit-overflow-bottom')"),'18px');
        await evaluate("document.querySelector('.theme-list').scrollTop=9999");await wait(40);
        assert.equal(await evaluate("document.querySelector('.theme-list').style.getPropertyValue('--hit-overflow-top')"),'18px');
        await evaluate("themes.setDeleteDialog(document.querySelector('dialog'),true)");
        assert.equal(await evaluate("document.activeElement.hasAttribute('data-dialog-cancel')"),true);
        assert.equal(await evaluate("getComputedStyle(document.querySelector('dialog'),'::backdrop').backgroundColor"),'rgba(0, 0, 0, 0)');
        assert.equal(await evaluate("getComputedStyle(document.querySelector('dialog')).backgroundColor===getComputedStyle(document.querySelector('select')||document.querySelector('dialog')).backgroundColor"),true);
        await evaluate("themes.setDeleteDialog(document.querySelector('dialog'),false)");
        const shot=await command('Page.captureScreenshot',{format:'png'});writeFileSync(resolve(root,file+'.png'),Buffer.from(shot.data,'base64'));
    }
    // Validate the actual rendered surface, not just the presence of palette attributes.
    await evaluate("window.cover=(()=>{const c=document.createElement('canvas');c.width=c.height=60;const x=c.getContext('2d');x.fillStyle='#7733bb';x.fillRect(0,0,60,60);x.fillStyle='#ff7733';x.fillRect(0,0,30,30);return c.toDataURL()})()");
    for(const file of ['HIT-OBSIDIAN-EYES.css','HIT-OLED-OFF-WHITE.css','HIT-LIGHT.css']) {
        await evaluate(`themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/'+file,'utf8'))});show('lyrics',true);window.artRoot=document.querySelector('.audio-menu');artRoot.classList.add('test-lyrics');artRoot.insertAdjacentHTML('afterbegin','<div class="audio-scene"><div class="audio-artwork-veil"></div><div class="audio-spectrum"></div></div>')`);
        for(const global of [false,true]) {
            await evaluate(`artRoot.removeAttribute('data-audio-neutral-lyrics');await audio.syncScene(artRoot,cover,true,true,true,{effects:{colorEnabled:true},blur:12,saturation:90,distortion:0,lyricsCover:true,global:${global}})`);
            const style=await evaluate("({background:getComputedStyle(sphere).backgroundColor,palette:sphere.style.getPropertyValue('--audio-art-background'),filter:getComputedStyle(document.querySelector('.audio-artwork-optics')).filter})");
            assert.equal(style.background.replaceAll(' ',''),style.palette.replaceAll(' ',''),'Light cannot wash out the artwork surface');
            assert.match(style.filter,/blur\(12px\)/,'Blur works at zero distortion');assert.match(style.filter,/saturate\(0.9\)/,'Saturation works at zero distortion');
            await evaluate(`await audio.syncScene(artRoot,null,true,true,true,{effects:{colorEnabled:true},reuseArtwork:true,blur:0,saturation:0,distortion:0,lyricsCover:true,global:${global}})`);
            assert.match(await evaluate("getComputedStyle(document.querySelector('.audio-artwork-optics')).filter"),/saturate\(0\)/,'Zero saturation remains a valid independent effect');
            await evaluate(`artRoot.dataset.audioNeutralLyrics='true';await audio.syncScene(artRoot,null,true,true,true,{reuseArtwork:true,neutralLyrics:true,global:${global}})`);
            assert.equal(await evaluate("getComputedStyle(sphere).backgroundImage"),'none');
            assert.equal(await evaluate("getComputedStyle(document.querySelector('.audio-scene')).display"),'none');
            const expected=file==='HIT-LIGHT.css'?[255,255,255]:[0,0,0];
            const clip=await evaluate("(()=>{const r=sphere.getBoundingClientRect();return{x:r.left+9,y:Math.floor(r.top+r.height/2),width:1,height:1,scale:1}})()");
            const shot=await command('Page.captureScreenshot',{format:'png',clip});assert.deepEqual(pixel(shot.data),expected,'Neutral lyrics pixels are fully black/white in '+file);
        }
    }
    await evaluate("show('lyrics',true)");
    assert.ok(await evaluate("(()=>{const a=document.querySelector('.lyrics-track-name').getBoundingClientRect(),b=document.querySelector('.lyrics-provider').getBoundingClientRect();return Math.abs(a.top-b.top)<.5&&Math.abs(a.height-b.height)<.5})()"),'Lyrics title/source share one aligned line');
    for(const [name,title,source] of [['lyrics-title',1,0],['lyrics-source',0,1],['lyrics-hidden',0,0]]) {
        await evaluate(`show('${name}',true)`);assert.equal(await evaluate("document.querySelectorAll('.lyrics-track-name').length"),title);assert.equal(await evaluate("document.querySelectorAll('.lyrics-provider').length"),source);
    }
    await evaluate("show('effects',true);window.readingRoot=document.querySelector('.audio-menu');await audio.syncScene(readingRoot,cover,true,true,false,{global:true});window.beforeReadingCalls=calls.length");await wait(30);
    assert.equal(await evaluate("document.querySelector('[data-hit-reading-cover]').hidden"),false,'Configuration screens expose the header cover button');
    await evaluate("document.querySelector('[data-hit-reading-cover]').click()");await wait(20);
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.sphere-audio-backdrop')).display"),'none','The header button hides only the background cover');
    assert.equal(await evaluate('sphere.dataset.audioCover'),'true','The saved cover request remains intact');
    await evaluate("readingRoot.append(document.createTextNode(' '))");await wait(20);
    assert.equal(await evaluate('sphere.dataset.audioReadingHidden'),'true','Rerenders on the same screen retain the temporary choice');
    await evaluate("document.querySelector('[data-hit-reading-cover]').click()");await wait(20);
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.sphere-audio-backdrop')).display"),'block','The same button restores the cover');
    await evaluate("document.querySelector('[data-hit-reading-cover]').click();show('artwork',true);await audio.syncScene(document.querySelector('.audio-menu'),cover,true,true,false,{global:true})");await wait(30);
    assert.equal(await evaluate('sphere.dataset.audioReadingHidden'),undefined,'Changing configuration screens resets the temporary reading override');
    assert.equal(await evaluate('calls.length===beforeReadingCalls'),true,'Reading overrides never call the persistence API');
    await evaluate("show('lyrics',true)");await wait(20);
    assert.equal(await evaluate("document.querySelector('[data-hit-reading-cover]').hidden"),true,'The temporary reading button is absent from the player and lyrics');
    // Measure the actual C# player/viewport markup before applying each content scale.
    await evaluate("panel.dataset.hitMenu='HoldItTogether.Components.HitMenus.HitMenuAudio';panel.innerHTML=views.player;window.scaled=document.querySelector('.hit-menu-scale');window.measured=document.querySelector('.hit-menu-viewport');viewport.observe(measured)");await wait(50);
    for(const factor of [.25,1,2]) {
        await evaluate(`scaled.style.setProperty('--hit-menu-scale-x','${factor}');scaled.style.setProperty('--hit-menu-scale-y','${factor}')`);await wait(40);
        assert.ok(Math.abs(await evaluate("measured.getBoundingClientRect().width")-360*factor)<1,'Player viewport follows 25–200% scale');
        if(factor===.25)assert.ok(await evaluate("document.querySelector('.hit-menu-measure').getBoundingClientRect().width<200"),'The shell can shrink below its former 360px minimum');
    }
    await evaluate('viewport.disconnect(measured)');
    // Real scaled lyric geometry: current verse must center in layout coordinates at all sizes.
    await evaluate("show('lyrics',true);window.lyricRoot=document.querySelector('.audio-menu');lyricRoot.classList.add('test-lyrics');const overlay=document.createElement('div');overlay.className='audio-overlay-view';overlay.append(lyricRoot.firstElementChild);lyricRoot.append(overlay);window.lyricViewport=document.querySelector('.lyrics-viewport')");
    for(const factor of [.25,1,2]) {
        await evaluate(`lyricRoot.style.transform='scale(${factor})';lyricRoot.style.transformOrigin='top left';audio.followLyrics(lyricViewport,15,true)`);await wait(70);
        const deviation=await evaluate("(()=>{const a=document.querySelector('[data-line-index=\"15\"]').getBoundingClientRect(),b=lyricViewport.getBoundingClientRect();return Math.abs((a.top+a.height/2-b.top)/b.height-.42)})()");
        assert.ok(deviation<.025,'Scaled current verse stays centered: '+factor+' deviation '+deviation);
    }
    await evaluate("lyricRoot.style.transform='none';audio.syncLyricsTimeline(lyricRoot,reference,{enabled:true,position:30,duration:120,thickness:3,canSeek:true,generation:7})");
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.sphere-lyrics-timeline'),'::before').height"),'3px');
    assert.ok(await evaluate("Math.abs(document.querySelector('.sphere-lyrics-timeline').getBoundingClientRect().bottom-sphere.getBoundingClientRect().bottom)<2"));
    await evaluate("(()=>{const r=document.querySelector('.sphere-lyrics-timeline input');r.dispatchEvent(new PointerEvent('pointerdown'));r.value='60';r.dispatchEvent(new Event('input'));r.dispatchEvent(new Event('change'))})()");await wait(30);
    assert.deepEqual(await evaluate('calls.at(-1)'),['SeekFromTimeline',7,60]);
    await evaluate("audio.syncLyricsTimeline(lyricRoot,reference,{enabled:false})");
    assert.equal(await evaluate("document.querySelector('.sphere-lyrics-timeline')"),null);
    // Cover is shell-owned, so content scaling cannot change its coverage or effects.
    await evaluate("audio.syncScene(lyricRoot,cover,true,true,true,{blur:12,zoom:150,saturation:90,distortion:20,drift:true,lyricsCover:true,neutralLyrics:false})");
    assert.equal(await evaluate('sphere.dataset.audioCover'),'true');
    assert.match(await evaluate("getComputedStyle(document.querySelector('.audio-artwork-optics')).filter"),/blur\(12px\)/);
    assert.ok(await evaluate("document.querySelector('.sphere-audio-backdrop').getBoundingClientRect().width>=sphere.getBoundingClientRect().width"));
    await evaluate("window.previousSurface=getComputedStyle(sphere).backgroundColor;window.NativeImage=window.Image;window.Image=class extends NativeImage{set src(value){setTimeout(()=>{super.src=value},150)}};const c=document.createElement('canvas');c.width=c.height=10;const x=c.getContext('2d');x.fillStyle='#11bb44';x.fillRect(0,0,10,10);void(window.pendingCover=audio.syncScene(lyricRoot,c.toDataURL(),true,true,true,{lyricsCover:true}))");
    await wait(40);
    assert.equal(await evaluate("getComputedStyle(sphere).backgroundColor"),await evaluate('previousSurface'),'Loading another cover retains the last surface instead of flashing the theme');
    await evaluate('await pendingCover;window.Image=NativeImage');
    await evaluate("audio.syncScene(lyricRoot,null,true,true,true,{reuseArtwork:true,neutralLyrics:true})");
    assert.equal(await evaluate('sphere.dataset.audioLyricsNeutral'),'true');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.sphere-audio-backdrop')).display"),'none');
    await evaluate("audio.disconnectLyrics(lyricViewport);audio.clearScene(lyricRoot);overflow.disconnect();disconnectReading()");
    assert.equal(await evaluate("document.querySelectorAll('feDisplacementMap').length"),0);
    assert.deepEqual(await evaluate('errors'),[]);
    console.log('PASS: shared controls/themes, settings layouts, hidden overflow hints, safe confirmations, 25–200% lyric centering, shell artwork/effects, seekable edge timeline and cleanup.');
} finally { await browser.close(); }
