import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {setTimeout as wait} from 'node:timers/promises';
import {openBrowser} from './driver.mjs';

const root=resolve('obj/shared-settings-browser');mkdirSync(root,{recursive:true});
const views=Object.fromEntries(['settings','audio-workspace','clock-workspace','notes-workspace','audio-size','audio-lyrics-settings','visualizer','effects','colors','clock-pomodoro','clock-world-settings'].map(name=>[name,readFileSync(`obj/menu-tests/${name}.html`,'utf8')]));
views.chess=readFileSync('obj/chess-tests/settings.html','utf8');
const styles={chess:readFileSync('Modules/HIT.Chess/Layout/css/chess.css','utf8'),notes:readFileSync('Modules/HIT.Example/Layout/css/notes.css','utf8')};
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><head>${['app.css','css/dynamic-sphere.css','css/hit-menus.css','css/menus.css','css/theme-base.css','css/hit-controls.css'].map(file=>`<link rel="stylesheet" href="../../wwwroot/${file}">`).join('')}
<style>.sphere-area{position:absolute;left:30px;top:30px}.dynamic-sphere{width:max-content!important;height:auto!important;overflow:visible!important}.sphere-content{position:relative!important;opacity:1!important;visibility:visible!important}.hit-menu-host{position:relative!important}</style>
</head><body><div class="sphere-area"><div class="dynamic-sphere is-open" data-hit-setting-descriptions="true"><div class="sphere-content"><div class="hit-menu-host"><div class="hit-menu-measure"><section id="panel" class="hit-menu-panel"></section></div></div></div></div></div><script type="module">
import * as controls from '../../wwwroot/js/settings-controls.js';import * as themes from '../../wwwroot/js/hit-themes.js';import * as extensions from '../../wwwroot/js/hit-extensions.js';import * as viewport from '../../wwwroot/js/hit-menu-viewport.js';import * as overflow from '../../wwwroot/js/hit-overflow.js';
window.controls=controls;window.themes=themes;window.views=${JSON.stringify(views)};window.errors=[];addEventListener('error',e=>errors.push(e.message));
extensions.styles('dev.hit.chess:chess',[{css:${JSON.stringify(styles.chess)}}]);extensions.styles('dev.hit.example:notes',[{css:${JSON.stringify(styles.notes)}}]);
window.show=(name,id)=>{document.querySelectorAll('.hit-menu-viewport').forEach(viewport.disconnect);panel.dataset.hitMenu=id;panel.innerHTML=views[name];document.querySelectorAll('.hit-menu-viewport').forEach(viewport.observe);};
controls.connectDocument();overflow.connect();window.ready=true;
</script></body></html>`);
const browser=await openBrowser(root),{evaluate,command}=browser;
try {
    for(const theme of ['HIT-OBSIDIAN-EYES.css','HIT-OLED-OFF-WHITE.css','HIT-LIGHT.css']){
        await evaluate(`themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/'+theme,'utf8'))})`);
        for(const [view,id,count] of [['settings','HoldItTogether.Components.HitMenus.HitMenuSettings',11],['audio-workspace','HoldItTogether.Components.HitMenus.HitMenuAudio',8],['clock-workspace','HoldItTogether.Components.HitMenus.HitMenuClock',3],['notes-workspace','dev.hit.example:notes',2]]){
            await evaluate(`show('${view}','${id}')`);await wait(80);
            assert.equal(await evaluate("document.querySelectorAll('.settings-topic').length"),count,`${view} exposes every topic`);
            assert.equal(await evaluate("document.querySelector('.settings-topic[aria-current=page]').textContent"),'General',`${view} highlights the active topic`);
            assert.equal(await evaluate("document.querySelector('.settings-content h2').textContent"),'General',`${view} displays the active page title`);
            assert.ok(await evaluate("const r=document.querySelector('.settings-workspace').getBoundingClientRect();Math.abs(r.width-610)<1&&Math.abs(r.height-420)<1"),`${view} shares workspace dimensions`);
            assert.ok(await evaluate("const root=document.querySelector('.settings-workspace').getBoundingClientRect();const content=document.querySelector('.settings-content').getBoundingClientRect();content.right<=root.right+1&&content.height<=root.height+1"),`${view} keeps its options pane bounded`);
            assert.equal(await evaluate("document.querySelector('.settings-workspace').dataset.descriptions"),'true','Host description preferences also reach independent modules');
            assert.equal(await evaluate("document.querySelectorAll('.settings-workspace .hit-back,.settings-workspace .audio-view-back,.settings-workspace .chess-back').length"),0,'The workspace uses shared header navigation');
            if(view==='chess'){
                assert.equal(await evaluate("document.querySelectorAll('.settings-combobox').length"),3,'Independent Chess receives the host wheel controls automatically');
                await evaluate("document.querySelector('.settings-combobox').click()");await wait(40);
                assert.equal(await evaluate("document.querySelectorAll('.settings-select-popup button').length"),3);
                await command('Input.dispatchKeyEvent',{type:'keyDown',key:'Escape'});await wait(220);
            }
            const screenshot=await command('Page.captureScreenshot',{format:'png'});writeFileSync(resolve(root,`${view}-${theme}.png`),Buffer.from(screenshot.data,'base64'));
        }
    }
    await evaluate("show('audio-workspace','HoldItTogether.Components.HitMenus.HitMenuAudio')");await wait(60);
    for(const page of ['audio-size','audio-lyrics-settings','visualizer','effects','colors']){
        await evaluate(`document.querySelector('.settings-content').innerHTML=views['${page}']`);await wait(60);
        assert.equal(await evaluate("const root=document.querySelector('.settings-workspace');root.querySelectorAll('input[type=range]').length===root.querySelectorAll('.settings-inline-number,.settings-number-controls input[type=number]').length"),true,`${page}: every slider retains editable numeric entry`);
        assert.ok(await evaluate("[...document.querySelectorAll('.settings-content input[type=range]')].every(range=>{const row=range.closest('.settings-number');if(!row)return false;const label=row.querySelector('.settings-slider-label').getBoundingClientRect(),r=range.getBoundingClientRect(),n=row.querySelector('input[type=number]').getBoundingClientRect();return label.right<=r.left+1&&Math.abs(n.top+n.height/2-r.top-r.height/2)<2;})"),page+': sliders share one inline row');
        if(page==='visualizer')assert.ok(await evaluate("[...document.querySelectorAll('.audio-visualizer-flips button')].every(button=>getComputedStyle(button).whiteSpace==='nowrap'&&button.scrollWidth<=button.clientWidth+1)"),'Orientation buttons retain single-line labels');
        assert.ok(await evaluate("const pane=document.querySelector('.settings-content');pane.scrollWidth<=pane.clientWidth+1"),`${page} does not overflow horizontally`);
        for(const label of await evaluate("[...document.querySelectorAll('select')].map(x=>x.getAttribute('aria-label'))")){
            assert.ok(await evaluate(`document.querySelector('button.settings-combobox[aria-label=${JSON.stringify(label)}]')!==null`),'Feature selects use the shared custom picker');
        }
    }
    await evaluate("show('clock-workspace','HoldItTogether.Components.HitMenus.HitMenuClock');document.querySelector('.settings-content').innerHTML=views['clock-pomodoro']");await wait(60);
    assert.equal(await evaluate("document.querySelectorAll('.settings-number-controls').length"),4,'All Pomodoro durations are visible without hidden groups');
    await evaluate("document.querySelector('.settings-content').innerHTML=views['clock-world-settings']");await wait(60);
    assert.ok(await evaluate("document.querySelector('.clock-world-list')!==null"),'World clocks have a directly reachable topic');
    await evaluate("document.querySelector('.dynamic-sphere').dataset.hitSettingDescriptions='false'");await wait(60);
    assert.equal(await evaluate("document.querySelector('.settings-workspace').dataset.descriptions"),'false');
    await evaluate("panel.innerHTML=''");await wait(60);
    assert.equal(await evaluate("document.querySelectorAll('.settings-select-popup,.settings-combobox').length"),0,'Removed workspaces release their transient controls');
    assert.deepEqual(await evaluate('errors'),[]);
    await evaluate('controls.disconnectDocument()');
    console.log('PASS: Settings/Audio/Clock/Notes share layout and topics in three themes, automatic extension controls, editable feature ranges, bounded scrolling and cleanup.');
}finally{await browser.close();}
