import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/lyrics-layout-tests');mkdirSync(root,{recursive:true});
const modes=['down','up','left','right','spotlight','wordrise'];
const views=Object.fromEntries(modes.map(mode=>[mode,readFileSync(`obj/menu-tests/lyrics-layout-${mode}.html`,'utf8')]));
views.settings=readFileSync('obj/menu-tests/audio-lyrics-settings.html','utf8');
views.plain=readFileSync('obj/menu-tests/lyrics-plain.html','utf8');
writeFileSync(resolve(root,'index.html'),`<!doctype html><html lang="en"><head><meta charset="utf-8">
${['app.css','css/dynamic-sphere.css','css/hit-menus.css','css/menus.css','css/theme-base.css','css/audio-shell.css','css/hit-controls.css'].map(file=>`<link rel="stylesheet" href="../../wwwroot/${file}">`).join('')}
<style>body{background:#888}.sphere-area{position:absolute;left:30px;top:30px}.dynamic-sphere{width:max-content!important;height:auto!important}.sphere-content{position:relative!important;opacity:1!important;visibility:visible!important}.hit-menu-host{position:relative}.audio-menu{height:300px}.audio-menu.is-settings{height:auto;width:440px}.audio-lyrics-view{display:flex;flex-direction:column;height:100%;min-height:0}</style>
</head><body><div class="sphere-area"><div class="dynamic-sphere is-open"><div class="sphere-content"><header class="sphere-header"><h1>Lyrics</h1></header><div class="hit-menu-host"><div class="hit-menu-measure"><section class="hit-menu-panel" data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuAudio"><div id="content" class="audio-menu"></div></section></div></div></div></div></div>
<script type="module">
import * as audio from '../../wwwroot/js/audio-player.js';import * as themes from '../../wwwroot/js/hit-themes.js';
window.audio=audio;window.themes=themes;window.views=${JSON.stringify(views)};window.errors=[];addEventListener('error',e=>errors.push(e.message));
window.show=mode=>{if(window.lyrics)audio.disconnectLyrics(lyrics);content.classList.toggle('is-settings',mode==='settings');content.innerHTML=views[mode];window.lyrics=content.querySelector('.lyrics-viewport');if(lyrics&&mode!=='plain')audio.followLyrics(lyrics,5,true,{position:30,playing:false});};themes.setMotion(true);window.ready=true;
</script></body></html>`);
const browser=await openBrowser(root),{evaluate,command}=browser;
try {
    for(const theme of ['HIT-OBSIDIAN-EYES.css','HIT-OLED-OFF-WHITE.css','HIT-LIGHT.css']) {
        await evaluate(`themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/'+theme,'utf8'))})`);
        for(const mode of modes) {
            await evaluate(`show('${mode}')`);await wait(40);
            const geometry=await evaluate(`(()=>{const box=lyrics.getBoundingClientRect(),current=lyrics.querySelector('.is-current').getBoundingClientRect(),past=lyrics.querySelector('[data-line-index="4"]').getBoundingClientRect(),future=lyrics.querySelector('[data-line-index="6"]').getBoundingClientRect();return {cx:current.x+current.width/2,cy:current.y+current.height/2,bx:box.x+box.width/2,by:box.y+box.height*.42,width:box.width,height:box.height,past:{x:past.x,y:past.y},future:{x:future.x,y:future.y},overflow:content.scrollWidth>content.clientWidth}})()`);
            assert.equal(geometry.overflow,false,mode+' keeps the host width');
            if(mode==='left'||mode==='right') {
                assert.ok(Math.abs(geometry.cx-geometry.bx)<2,mode+' centers the current line horizontally');
                assert.equal(geometry.past.x<geometry.future.x,mode==='left');
            } else if(mode==='spotlight') {
                assert.equal(await evaluate("[...lyrics.querySelectorAll('.lyric-line')].filter(line=>line.offsetHeight>0).length"),1);
                assert.ok(Math.abs(geometry.cy-(geometry.by+geometry.height*.08))<2,'Spotlight centers vertically');
            } else {
                assert.ok(Math.abs(geometry.cy-geometry.by)<2,mode+' follows the current line');
                assert.equal(geometry.past.y<geometry.future.y,mode!=='up');
            }
            assert.equal(await evaluate("getComputedStyle(lyrics.querySelector('.is-current')).textAlign"),'left');
        }
        await evaluate("show('settings')");await wait(30);
        assert.equal(await evaluate("document.querySelectorAll('input[type=radio][name=lyrics-layout]').length"),6);
        assert.equal(await evaluate("document.querySelectorAll('.lyrics-layout-preview').length"),6);
        assert.equal(await evaluate("[...document.querySelectorAll('.lyrics-layout-selected')].filter(icon=>getComputedStyle(icon).visibility==='visible').length"),1,'Only the selected preview has a checkmark');
        assert.equal(await evaluate("content.scrollWidth>content.clientWidth"),false);
        await evaluate("document.querySelector('input[aria-label=\"Word rise\"]').focus()");
        await command('Input.dispatchKeyEvent',{type:'keyDown',key:'ArrowLeft',code:'ArrowLeft',windowsVirtualKeyCode:37});
        await command('Input.dispatchKeyEvent',{type:'keyUp',key:'ArrowLeft',code:'ArrowLeft',windowsVirtualKeyCode:37});
        assert.equal(await evaluate('document.activeElement.getAttribute("aria-label")'),'Spotlight','Radio choices retain native keyboard navigation');
        const shot=await command('Page.captureScreenshot',{format:'png'});writeFileSync(resolve(root,theme+'.png'),Buffer.from(shot.data,'base64'));
    }
    await evaluate("show('left');window.outerWheel=0;document.body.addEventListener('wheel',()=>outerWheel++);lyrics.dispatchEvent(new WheelEvent('wheel',{deltaY:80,bubbles:true,cancelable:true}))");
    assert.equal(await evaluate('outerWheel'),0,'Horizontal lyrics own wheel input');
    await evaluate("show('wordrise');themes.setMotion(false);audio.followLyrics(lyrics,5,true,{position:30,playing:false})");await wait(30);
    assert.ok(await evaluate("lyrics.querySelectorAll('.lyric-word').length>1&&[...lyrics.querySelectorAll('.lyric-word')].every(word=>word.closest('.is-current'))"),'Only the current line allocates word spans');
    await evaluate("window.oldWords=[...lyrics.querySelectorAll('.lyric-word')];const line=lyrics.querySelector('.is-current');line.innerHTML=line.innerHTML;audio.followLyrics(lyrics,5,true,{position:30,playing:false})");
    assert.ok(await evaluate("oldWords.every(word=>word.getAnimations().length===0)&&lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches('.lyric-word')).every(a=>a.effect.target.isConnected)"),'Track replacement cancels old word animations even when the keyed line is retained');
    assert.equal(await evaluate('lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches(".lyric-word")).length'),7);
    const budget=await evaluate("Math.max(...lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches('.lyric-word')).map(a=>a.effect.getTiming().delay+a.effect.getTiming().duration))");
    assert.ok(Math.abs(budget-4000)<1,'Words complete in exactly two thirds of the six-second line');
    const timings=await evaluate("lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches('.lyric-word')).map(a=>a.effect.getTiming())");
    for(let index=1;index<timings.length;index++) {
        assert.ok(Math.abs(timings[index].delay-timings[index-1].delay-timings[index-1].duration/2)<1,'Each word starts halfway through its predecessor');
    }
    await evaluate('audio.syncLyricsPlayback(lyrics,32,false)');await wait(25);
    const middle=await evaluate("[...lyrics.querySelectorAll('.lyric-word')].map(word=>getComputedStyle(word).opacity)");
    await wait(100);
    assert.deepEqual(await evaluate("[...lyrics.querySelectorAll('.lyric-word')].map(word=>getComputedStyle(word).opacity)"),middle,'Paused playback freezes word progress');
    assert.ok(Number(middle[0])>.99&&Number(middle.at(-1))<.3,'Words rise sequentially');
    await evaluate('audio.syncLyricsPlayback(lyrics,34,false)');await wait(25);
    assert.ok(await evaluate("[...lyrics.querySelectorAll('.lyric-word')].every(word=>Number(getComputedStyle(word).opacity)>.99)"),'The line is fully readable after two thirds');
    await evaluate("audio.syncLyricsPlayback(lyrics,30,false);lyrics.querySelector('.is-current').dataset.lineEnd='31.5';audio.disconnectLyrics(lyrics);audio.followLyrics(lyrics,5,true,{position:30,playing:false})");
    assert.ok(await evaluate("Math.abs(Math.max(...lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches('.lyric-word')).map(a=>a.effect.getTiming().delay+a.effect.getTiming().duration))-1000)<1"),'Faster lines use proportionally faster words');
    await evaluate('audio.syncLyricsPlayback(lyrics,30.2,true)');await wait(60);
    assert.ok(await evaluate("lyrics.getAnimations({subtree:true}).some(a=>a.effect.target.matches('.lyric-word')&&a.playState==='running')"),'Playing resumes word animation');
    await evaluate("document.querySelector('.sphere-area').dataset.hitResourceSleep='true'");await wait(25);
    assert.ok(await evaluate("lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches('.lyric-word')).every(a=>a.playState!=='running')"),'Resource sleep pauses WAAPI animations');
    await evaluate("delete document.querySelector('.sphere-area').dataset.hitResourceSleep;themes.setMotion(true)");await wait(25);
    assert.equal(await evaluate("lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches('.lyric-word')).length"),0,'Remove animations cancels word effects');
    await evaluate('themes.setMotion(false)');await command('Emulation.setEmulatedMedia',{features:[{name:'prefers-reduced-motion',value:'reduce'}]});await wait(100);
    assert.equal(await evaluate("lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches('.lyric-word')).length"),0,'System reduced motion shows static readable words');
    await command('Emulation.setEmulatedMedia',{features:[]});
    await evaluate('audio.disconnectLyrics(lyrics)');await wait(25);
    assert.equal(await evaluate("lyrics.getAnimations({subtree:true}).filter(a=>a.effect.target.matches('.lyric-word')).length"),0,'Disposal releases animation handles');
    await evaluate("show('plain')");
    assert.equal(await evaluate("lyrics.dataset.lyricsLayout"),'down');
    assert.equal(await evaluate("lyrics.querySelectorAll('button.lyric-line').length"),0,'Untimed lyrics never gain fake seek targets');
    await evaluate("window.keyCalls=[];content.dataset.audioCanToggle='true';window.keyReference={async invokeMethodAsync(name){keyCalls.push(name)}};audio.connectAudioKeys(content,keyReference);audio.connectAudioKeys(content,keyReference);window.pressSpace=(target=document.body,options={})=>{const event=new KeyboardEvent('keydown',{key:' ',code:'Space',bubbles:true,cancelable:true,...options});target.dispatchEvent(event);return event.defaultPrevented}");
    for(const page of ['down','wordrise','settings']) {
        await evaluate(`show('${page}')`);
        assert.equal(await evaluate('pressSpace()'),true,'Space is consumed across Audio pages');
    }
    assert.equal(await evaluate('keyCalls.length'),3,'Repeated connection does not duplicate shortcuts');
    assert.equal(await evaluate('pressSpace(document.body,{repeat:true})'),true,'Holding Space does not scroll');
    for(const options of [{ctrlKey:true},{altKey:true},{shiftKey:true},{metaKey:true},{isComposing:true}]) await evaluate(`pressSpace(document.body,${JSON.stringify(options)})`);
    for(const markup of ['<input type="text">','<input type="range">','<button>Action</button>','<div contenteditable="true">Text</div>']) {
        assert.equal(await evaluate(`(()=>{const node=document.createElement('div');node.innerHTML=${JSON.stringify(markup)};content.append(node);const prevented=pressSpace(node.firstElementChild);node.remove();return prevented})()`),false,'Editing and focused controls retain native Space behavior');
    }
    await evaluate("content.dataset.audioCanToggle='false';pressSpace();content.dataset.audioCanToggle='true';document.querySelector('.hit-menu-panel').hidden=true;pressSpace();document.querySelector('.hit-menu-panel').hidden=false;document.querySelector('.sphere-area').dataset.hitResourceSleep='true';pressSpace();delete document.querySelector('.sphere-area').dataset.hitResourceSleep;document.documentElement.dataset.hitSelectOpen='true';pressSpace();delete document.documentElement.dataset.hitSelectOpen;const dialog=document.createElement('dialog');content.append(dialog);dialog.showModal();pressSpace();dialog.close();dialog.remove()");
    assert.equal(await evaluate('keyCalls.length'),3,'Unsupported, hidden, sleeping and modal states ignore the shortcut');
    await evaluate('audio.disconnectAudioKeys(content);pressSpace()');
    assert.equal(await evaluate('keyCalls.length'),3,'Shortcut listener is released on disposal');
    assert.deepEqual(await evaluate('errors'),[]);
    console.log('PASS: six lyric layouts in three themes, preview radios, word timing, reduced motion, lifecycle cleanup and Audio Space shortcut guards/disposal.');
} finally { await browser.close(); }
