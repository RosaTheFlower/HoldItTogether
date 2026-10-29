import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { readFileSync, writeFileSync, createWriteStream, mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { setTimeout as wait } from 'node:timers/promises';

// Run the native chess tests first to generate Razor markup.
const root = resolve('obj/chess-tests/browser'); mkdirSync(root, { recursive: true });
const board = readFileSync('obj/chess-tests/board.html', 'utf8');
const settings = readFileSync('obj/chess-tests/settings.html', 'utf8');
const soundSettings = readFileSync('obj/chess-tests/sound-settings.html', 'utf8');
const confirmation = readFileSync('obj/chess-tests/confirmation.html', 'utf8');
const extensionConfirmation = readFileSync('obj/extensions-preview.html', 'utf8');
const played = readFileSync('obj/chess-tests/played.html', 'utf8');
const captures = readFileSync('obj/chess-tests/captures.html', 'utf8');
const mate = readFileSync('obj/chess-tests/mate.html', 'utf8');
const e4 = readFileSync('obj/chess-tests/e4.html', 'utf8');
const pendingE4 = readFileSync('obj/chess-tests/e4-pending.html', 'utf8');
const d5 = readFileSync('obj/chess-tests/d5.html', 'utf8');
const beforeCastle = readFileSync('obj/chess-tests/before-castle.html', 'utf8');
const castle = readFileSync('obj/chess-tests/castle.html', 'utf8');
const importUrl = JSON.parse(readFileSync('obj/chess-tests/import-url.json', 'utf8'));
const importTarget = resolve(root, importUrl);
mkdirSync(resolve(importTarget, '..'), { recursive: true });
writeFileSync(importTarget, readFileSync('Modules/HIT.Chess/Assets/chess.js', 'utf8'));
writeFileSync(resolve(root, 'index.html'), `<!doctype html><html lang="en"><head><meta charset="utf-8">
${['app.css', 'css/dynamic-sphere.css', 'css/hit-menus.css', 'css/menus.css', 'css/theme-base.css', 'css/hit-controls.css'].map(file => `<link rel="stylesheet" href="../../../wwwroot/${file}">`).join('')}
<style id="fixture-layout">body{background:#888}.sphere-area{position:absolute;left:40px;top:40px}.dynamic-sphere{width:max-content!important;height:auto!important}.sphere-content{position:relative!important;opacity:1!important;visibility:visible!important}.hit-menu-host{position:relative}</style>
</head><body><div class="sphere-area"><div class="dynamic-sphere is-open is-pinned" id="sphere"><div class="sphere-content">
<header class="sphere-header"><h1 class="sphere-title">Chess</h1></header>
<div class="hit-menu-host"><div class="hit-menu-measure"><section class="hit-menu-panel" data-hit-menu="dev.hit.chess:chess" id="panel">${board}</section></div></div>
</div></div></div><script type="module">
import * as themes from '../../../wwwroot/js/hit-themes.js';
import * as extensions from '../../../wwwroot/js/hit-extensions.js';
import {connectMenuChrome} from '../../../wwwroot/js/hit-menu-chrome.js';
import {connect} from '../../../Modules/HIT.Chess/Assets/chess.js';
import * as nativeSphere from '../../../wwwroot/js/dynamic-sphere.js';
import * as settingsControls from '../../../wwwroot/js/settings-controls.js';settingsControls.connectDocument();
window.errors=[];window.calls=[];window.themes=themes;window.extensions=extensions;window.nativeSphere=nativeSphere;
window.soundStarts=[];window.audioContexts=[];window.soundBuffers=[];
const NativeAudioContext=window.AudioContext;
window.AudioContext=class extends NativeAudioContext {
constructor(...args){super(...args);audioContexts.push(this)}
createBuffer(...args){const buffer=super.createBuffer(...args);soundBuffers.push(buffer);return buffer}
createBufferSource(){const source=super.createBufferSource(),start=source.start.bind(source);source.start=(...args)=>{soundStarts.push(source.buffer.duration);start(...args)};return source}
};
addEventListener('error',e=>errors.push(e.message));
extensions.styles('dev.hit.chess:chess',[{url:'extensions/dev.hit.chess/1.0.0/Layout/css/chess.css',css:${JSON.stringify(readFileSync('Modules/HIT.Chess/Layout/css/chess.css', 'utf8'))}}]);
themes.setMotion(true);
window.mountChess=()=>{const root=document.querySelector('.chess-menu');
window.connection=connect(root,{async invokeMethodAsync(name){calls.push(name);if(name==='CompactAsync'){root.classList.add('is-compact');root.dataset.hitView='compact';delete root.dataset.hitIdleAction;document.querySelector('.sphere-header').hidden=true;document.querySelector('.hit-menu-host').classList.add('is-compact');}}});
window.disconnect=connectMenuChrome(document.getElementById('sphere'));};window.mountChess();
window.ready=true;
</script></body></html>`);

const browser = spawn('C:/Program Files/Google/Chrome/Application/chrome.exe', [
    '--headless', '--no-sandbox', '--disable-gpu', '--no-first-run', '--allow-file-access-from-files', '--remote-debugging-pipe',
    '--window-size=1000,850', '--user-data-dir=' + resolve(root, 'profile')
], { stdio: ['ignore', 'ignore', 'pipe', 'pipe', 'pipe'], windowsHide: true });
browser.stderr.pipe(createWriteStream(resolve(root, 'chromium.log')));
let sequence = 0, buffer = ''; const pending = new Map();
browser.stdio[4].on('data', data => {
    buffer += data.toString(); let index;
    while ((index = buffer.indexOf('\0')) !== -1) {
        const raw = buffer.slice(0, index); buffer = buffer.slice(index + 1); if (!raw) continue;
        const message = JSON.parse(raw), entry = pending.get(message.id);
        if (entry) { clearTimeout(entry.timer); pending.delete(message.id); message.error ? entry.reject(new Error(JSON.stringify(message.error))) : entry.resolve(message.result); }
    }
});
function send(method, params = {}, sessionId) {
    return new Promise((resolve, reject) => {
        const id = ++sequence, timer = setTimeout(() => { pending.delete(id); reject(new Error('CDP timeout: ' + method)); }, 10000);
        pending.set(id, { resolve, reject, timer }); browser.stdio[3].write(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) }) + '\0');
    });
}
try {
    const { targetId } = await send('Target.createTarget', { url: pathToFileURL(resolve(root, 'index.html')).href });
    const { sessionId } = await send('Target.attachToTarget', { targetId, flatten: true });
    const command = (method, params) => send(method, params, sessionId);
    async function evaluate(expression) {
        const { result, exceptionDetails } = await command('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
        if (exceptionDetails) throw new Error(JSON.stringify(exceptionDetails)); return result.value;
    }
    for (let i = 0; i < 100 && !await evaluate('window.ready===true'); i++) await wait(30);
    assert.equal(await evaluate(`(async()=>typeof (await import(${JSON.stringify(importUrl)})).connect)()`), 'function', 'Actual .NET import specifier resolves in the browser');
    const palettes = [];
    for (const file of ['HIT-OBSIDIAN-EYES.css', 'HIT-OLED-OFF-WHITE.css', 'HIT-LIGHT.css']) {
        await evaluate(`themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/' + file, 'utf8'))})`);
        await wait(80);
        assert.equal(await evaluate("document.querySelectorAll('.chess-square').length"), 64);
        assert.equal(await evaluate("document.querySelectorAll('.chess-piece').length"), 32);
        assert.equal(await evaluate("document.querySelector('.chess-menu').scrollWidth > document.querySelector('.chess-menu').clientWidth"), false);
        const colors = await evaluate(`({light:getComputedStyle(document.querySelector('.chess-square.light')).backgroundColor,dark:getComputedStyle(document.querySelector('.chess-square.dark')).backgroundColor,white:getComputedStyle(document.querySelector('.white-piece')).fill,black:getComputedStyle(document.querySelector('.black-piece')).fill})`);
        assert.notEqual(colors.light, colors.dark, JSON.stringify(await evaluate(`({errors,css:document.querySelector('[data-hit-extension]').textContent.slice(0,1800),vars:getComputedStyle(document.querySelector('.chess-menu')).getPropertyValue('--chess-light'),theme:document.documentElement.dataset.hitThemeError})`)));
        assert.notEqual(colors.white, colors.black);
        palettes.push(colors);
        for (const classes of ['theme-delete-dialog', 'theme-delete-dialog extension-dialog']) {
            const backdrop = await evaluate(`(()=>{const d=document.createElement('dialog');d.className=${JSON.stringify(classes)};document.getElementById('panel').append(d);d.showModal();const s=getComputedStyle(d,'::backdrop');const value={color:s.backgroundColor,filter:s.backdropFilter};d.close();d.remove();return value})()`);
            assert.equal(backdrop.color, 'rgba(0, 0, 0, 0)', file + ' has no confirmation dimming');
            assert.equal(backdrop.filter, 'none', file + ' has no confirmation blur');
        }
        const confirmStyle=await evaluate(`(()=>{const template=document.createElement('template');template.innerHTML=${JSON.stringify(confirmation)};const dialog=template.content.querySelector('dialog');panel.append(dialog);themes.setDeleteDialog(dialog,true);const s=getComputedStyle(dialog),button=getComputedStyle(dialog.querySelector('button'));const value={radius:s.borderRadius,padding:s.padding,buttonRadius:button.borderRadius,buttonHeight:button.minHeight,focus:document.activeElement.textContent};dialog.close();dialog.remove();return value})()`);
        assert.deepEqual(confirmStyle,{radius:'16px',padding:'22px',buttonRadius:'8px',buttonHeight:'32px',focus:'Cancel'},'Actual Chess confirmation uses shared Settings geometry in '+file);
        const dialogComparison=await evaluate(`(()=>{
            const style=dialog=>{const s=getComputedStyle(dialog),h=getComputedStyle(dialog.querySelector('h2')),p=getComputedStyle(dialog.querySelector('p')),actions=getComputedStyle(dialog.querySelector('.dialog-actions'));const buttons=[...dialog.querySelectorAll('.dialog-actions button')].map(button=>{const b=getComputedStyle(button);return{width:b.width,height:b.height,padding:b.padding,radius:b.borderRadius,color:b.color,background:b.backgroundColor,border:b.borderColor,font:b.font}});return{width:s.width,padding:s.padding,radius:s.borderRadius,border:s.borderColor,background:s.backgroundColor,color:s.color,font:s.font,title:h.font,titleMargin:h.margin,description:p.font,descriptionColor:p.color,actions:actions.display,gap:actions.gap,actionsMargin:actions.marginTop,buttons}};
            const template=document.createElement('template');template.innerHTML=${JSON.stringify(extensionConfirmation)};
            const reference=template.content.querySelector('dialog'),settingsPanel=document.createElement('section');settingsPanel.className='hit-menu-panel';settingsPanel.dataset.hitMenu='HoldItTogether.Components.HitMenus.HitMenuSettings';settingsPanel.style.position='absolute';settingsPanel.append(reference);document.querySelector('.hit-menu-measure').append(settingsPanel);themes.setDeleteDialog(reference,true);const expected=style(reference);reference.close();settingsPanel.remove();
            template.innerHTML=${JSON.stringify(confirmation)};const chess=template.content.querySelector('dialog');panel.append(chess);themes.setDeleteDialog(chess,true);const actual=style(chess);chess.close();chess.remove();return{expected,actual};
        })()`);
        assert.deepEqual(dialogComparison.actual,dialogComparison.expected,'Chess New game exactly shares the Remove extension visual controls in '+file);
        const actionLayout = await evaluate(`(()=>{const template=document.createElement('template');template.innerHTML=${JSON.stringify(confirmation)};const dialog=template.content.querySelector('dialog');panel.append(dialog);themes.setDeleteDialog(dialog,true);const row=dialog.querySelector('.dialog-actions').getBoundingClientRect(),buttons=[...dialog.querySelectorAll('.dialog-actions button')].map(button=>{const r=button.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height,overflow:button.scrollWidth>button.clientWidth}});return {rowWidth:row.width,buttons}})()`);
        assert.ok(actionLayout.buttons.every(button=>button.width>100&&button.height<=36&&!button.overflow),'Confirmation labels fit on one line in '+file);
        assert.equal(actionLayout.buttons[0].y,actionLayout.buttons[1].y,'Confirmation actions share one row');
        assert.ok(Math.abs(actionLayout.buttons[0].width-actionLayout.buttons[1].width)<1,'Confirmation actions have equal widths');
        assert.ok(Math.abs(actionLayout.buttons.reduce((width,button)=>width+button.width,8)-actionLayout.rowWidth)<1,'Confirmation actions fill their row');
        const dialogShot = await command('Page.captureScreenshot', { format: 'png' });
        writeFileSync(resolve(root, 'confirmation-' + file + '.png'), Buffer.from(dialogShot.data, 'base64'));
        await evaluate("document.querySelector('dialog').close();document.querySelector('dialog').remove()");
        const shot = await command('Page.captureScreenshot', { format: 'png' });
        writeFileSync(resolve(root, file + '.png'), Buffer.from(shot.data, 'base64'));
    }
    assert.notDeepEqual(palettes[0], palettes[2], 'Theme changes board and pieces');
    assert.equal(await evaluate("Math.round(document.querySelector('.chess-menu').getBoundingClientRect().width)"), 360, 'Empty captures leave no horizontal rails');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-board')).borderRadius"), '0px');
    assert.equal(await evaluate("document.querySelector('.chess-board').hasAttribute('data-hit-menu-trigger')"), false);
    await evaluate("window.outerContext=0;document.getElementById('sphere').addEventListener('contextmenu',()=>outerContext++)");
    async function rightGesture(from, to = from) {
        const coords = await evaluate(`(()=>{const r=document.querySelector('[data-square="${from}"]').getBoundingClientRect();const t=document.querySelector('[data-square="${to}"]').getBoundingClientRect();return{x:r.x+r.width/2,y:r.y+r.height/2,tx:t.x+t.width/2,ty:t.y+t.height/2}})()`);
        await command('Input.dispatchMouseEvent', { type: 'mousePressed', x: coords.x, y: coords.y, button: 'right', buttons: 2, clickCount: 1 });
        if (from !== to) await command('Input.dispatchMouseEvent', { type: 'mouseMoved', x: coords.tx, y: coords.ty, button: 'right', buttons: 2 });
        await command('Input.dispatchMouseEvent', { type: 'mouseReleased', x: coords.tx, y: coords.ty, button: 'right', buttons: 0, clickCount: 1 });
    }
    await rightGesture(28);
    assert.equal(await evaluate("document.querySelector('[data-square=\"28\"]').classList.contains('marked-red')"), true, 'Right-click marks a square red');
    await rightGesture(28);
    assert.equal(await evaluate("document.querySelectorAll('.marked-red').length"), 0, 'Right-click toggles mark off');
    await rightGesture(12, 28);
    assert.equal(await evaluate("document.querySelectorAll('.chess-arrow:not(.preview)').length"), 1, 'Right-drag draws arrow');
    await rightGesture(12, 28);
    assert.equal(await evaluate("document.querySelectorAll('.chess-arrow').length"), 0, 'Same arrow toggles off');
    await rightGesture(6, 21);
    assert.equal(await evaluate("document.querySelector('.chess-arrow path').getAttribute('d').split(' L').length"), 3, 'Knight arrow has a corner');
    assert.equal(await evaluate('outerContext'), 0, 'Board context menu does not reach settings');
    await evaluate('connection.sync()');
    assert.equal(await evaluate("document.querySelectorAll('.chess-arrow').length"), 1, 'Rerender preserves annotations until a move');
    await evaluate("document.querySelector('.chess-menu').dataset.position='next';connection.sync()");
    assert.equal(await evaluate("document.querySelectorAll('.chess-arrow').length"), 0, 'Move clears annotations');
    await evaluate(`document.querySelector('.chess-square').classList.add('selected');document.querySelectorAll('.chess-square')[16].classList.add('legal');`);
    assert.notEqual(await evaluate("getComputedStyle(document.querySelector('.selected .chess-piece')).transform"), 'none');
    await wait(5100);
    assert.equal(await evaluate("document.querySelector('.chess-menu').classList.contains('is-compact')"), false, 'Stationary pointer over board prevents compact');
    const header = await evaluate("(()=>{const r=document.querySelector('.sphere-header').getBoundingClientRect();return{x:r.x+10,y:r.y+10}})()");
    await command('Input.dispatchMouseEvent', { type: 'mouseMoved', ...header });
    await evaluate("document.querySelector('.chess-menu').dispatchEvent(new Event('hit-menu-idle'))");
    assert.equal(await evaluate("document.querySelector('.chess-menu').classList.contains('is-compact')"), false, 'Header hover also prevents compact');
    await command('Input.dispatchMouseEvent', { type: 'mouseMoved', x: 900, y: 700 });
    await wait(4800);
    assert.equal(await evaluate("document.querySelector('.chess-menu').classList.contains('is-compact')"), false, 'Leaving starts a fresh five-second delay');
    await wait(400);
    assert.equal(await evaluate("document.querySelector('.chess-menu').classList.contains('is-compact')"), true, 'Pointer away for five seconds compacts');
    assert.equal(await evaluate("Math.round(document.querySelector('.chess-board').getBoundingClientRect().width)"), 152);
    assert.equal(await evaluate("document.querySelectorAll('.chess-square').length"), 64, 'Compact keeps entire board');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-overview')).display"), 'none');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-board')).borderRadius"), '8px');
    const compactShot = await command('Page.captureScreenshot', { format: 'png' });
    writeFileSync(resolve(root, 'compact.png'), Buffer.from(compactShot.data, 'base64'));
    await evaluate(`window.connection.dispose();window.disconnect();window.calls=[];document.querySelector('.chess-menu').dispatchEvent(new Event('hit-menu-idle'));`);
    assert.deepEqual(await evaluate('calls'), [], 'Disposed listener does not call .NET');
    async function show(markup) {
        await evaluate(`connection.dispose();disconnect();document.getElementById('panel').innerHTML=${JSON.stringify(markup)};document.querySelector('.sphere-header').hidden=false;document.querySelector('.hit-menu-host').classList.remove('is-compact');mountChess();`);
    }
    async function screenshot(name) {
        const shot = await command('Page.captureScreenshot', { format: 'png' });
        writeFileSync(resolve(root, name + '.png'), Buffer.from(shot.data, 'base64'));
    }
    await show(played);
    assert.equal(await evaluate("document.querySelectorAll('.last-move').length"), 2);
    assert.equal(await evaluate("document.querySelector('.chess-history .latest strong').textContent"), 'exd5');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-history .latest strong')).fontWeight"), '700');
    assert.equal(await evaluate("document.querySelectorAll('.chess-captures').length"), 1);
    assert.equal(await evaluate("Math.round(document.querySelector('.chess-menu').getBoundingClientRect().width)"), 360);
    assert.equal(await evaluate("document.querySelector('.chess-captured-area').getBoundingClientRect().top>=document.querySelector('.chess-board').getBoundingClientRect().bottom"), true, 'Captured pieces are below the board');
    await rightGesture(35, 43);
    await screenshot('played');
    await show(captures);
    assert.equal(await evaluate("document.querySelectorAll('.chess-captures').length"), 2);
    assert.equal(await evaluate("document.querySelectorAll('.chess-capture-group').length"), 2);
    assert.equal(await evaluate("(()=>{const g=[...document.querySelectorAll('.chess-capture-group')];return Math.abs(g[0].getBoundingClientRect().top-g[1].getBoundingClientRect().top)<1})()"), true, 'Both sides share one capture line');
    assert.equal(await evaluate("Math.round(document.querySelector('.chess-captured-area').getBoundingClientRect().height)"), 36, 'Capture strip occupies only one row');
    assert.equal(await evaluate("[...document.querySelectorAll('.chess-captures')].every(e=>e.scrollWidth>e.clientWidth)"), true, 'Capture rows scroll horizontally');
    assert.equal(await evaluate("[...document.querySelectorAll('[data-chess-scroll]')].every(e=>getComputedStyle(e).scrollbarWidth==='none')"), true, 'All scrollbars are hidden');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-captures'),'::-webkit-scrollbar').display"), 'none');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-capture-stack')).flexDirection"), 'row', 'Captures form rows');
    await evaluate("document.querySelector('.chess-captures').scrollLeft=50;connection.sync()");
    assert.ok(await evaluate("document.querySelector('.chess-captures').scrollLeft") > 0);
    assert.equal(await evaluate("document.querySelector('.chess-capture-frame').dataset.canStart"), 'true');
    assert.equal(await evaluate("document.querySelector('.chess-capture-frame').dataset.canEnd"), 'true');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-capture-frame'),'::before').opacity"), '1');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-capture-frame'),'::after').pointerEvents"), 'none');
    await screenshot('captures');
    await evaluate("document.querySelector('.chess-captures').scrollLeft=0;connection.sync()");
    assert.equal(await evaluate("document.querySelector('.chess-capture-frame').dataset.canStart"), 'false');
    await evaluate("document.querySelector('.chess-captures').scrollLeft=9999;connection.sync()");
    assert.equal(await evaluate("document.querySelector('.chess-capture-frame').dataset.canEnd"), 'false');
    await evaluate("document.querySelector('.chess-history-content').innerHTML=Array.from({length:50},(_,i)=>'<span class=\"chess-history-move\">'+(i+1)+'. Nf3</span>').join('');document.querySelector('.chess-history').scrollLeft=0;connection.sync()");
    assert.equal(await evaluate("document.querySelector('.chess-history-frame').dataset.canStart"), 'false');
    assert.equal(await evaluate("document.querySelector('.chess-history-frame').dataset.canEnd"), 'true');
    await evaluate("document.querySelector('.chess-history').dispatchEvent(new WheelEvent('wheel',{bubbles:true,cancelable:true,deltaY:120}))");
    assert.ok(await evaluate("document.querySelector('.chess-history').scrollLeft") > 0, 'History scrolls horizontally');
    assert.equal(await evaluate("document.querySelector('.chess-history-frame').dataset.canStart"), 'true');
    assert.equal(await evaluate("document.querySelector('.chess-history-frame').dataset.canEnd"), 'true');
    await screenshot('history-scroll');
    await evaluate("document.querySelector('.chess-history').scrollLeft=99999;connection.sync()");
    assert.equal(await evaluate("document.querySelector('.chess-history-frame').dataset.canEnd"), 'false');
    await show(mate);
    assert.equal(await evaluate("document.querySelectorAll('.checkmated .chess-mate-flag').length"), 1);
    assert.notEqual(await evaluate("getComputedStyle(document.querySelector('.checkmated .chess-piece')).transform"), 'none');
    assert.equal(await evaluate("document.querySelector('.checkmated').dataset.square"), '4');
    assert.equal(await evaluate("document.querySelector('.chess-result button').classList.contains('is-primary')"), true);
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-result button')).borderRadius"), '8px');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-result button')).minHeight"), '32px');
    await screenshot('mate');
    // Apply successive actual C# board snapshots, retaining the board DOM as Blazor does.
    await show(board);
    await evaluate(`document.querySelector('.chess-menu').dataset.game=${JSON.stringify(new RegExp('data-game="([^\"]+)"').exec(e4)[1])};connection.sync();themes.setMotion(false)`);
    await command('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'no-preference' }] });
    const activation = await evaluate("(()=>{const r=document.querySelector('[data-square=\"12\"]').getBoundingClientRect();return{x:r.x+r.width/2,y:r.y+r.height/2}})()");
    await command('Input.dispatchMouseEvent', { type: 'mousePressed', ...activation, button: 'left', buttons: 1, clickCount: 1 });
    await command('Input.dispatchMouseEvent', { type: 'mouseReleased', ...activation, button: 'left', buttons: 0, clickCount: 1 });
    await wait(80);
    async function updateBoard(markup, sync = true) {
        await evaluate(`(()=>{const template=document.createElement('template');template.innerHTML=${JSON.stringify(markup)};const next=template.content.querySelector('.chess-menu'),root=document.querySelector('.chess-menu');document.querySelector('.chess-board').innerHTML=next.querySelector('.chess-board').innerHTML;for(const [key,value] of Object.entries(next.dataset))root.dataset[key]=value;${sync ? 'connection.sync()' : ''}})()`);
    }
    const beforeSounds = await evaluate('soundStarts.length');
    await evaluate("document.querySelector('[data-square=\"12\"]').classList.add('selected');connection.sync()");
    await updateBoard(pendingE4, false);
    await wait(40);
    assert.equal(await evaluate("getComputedStyle(document.querySelector('[data-square=\"28\"] .chess-piece')).visibility"), 'hidden', 'Destination cannot flash while the render waits for JS');
    await evaluate('connection.sync()');
    assert.equal(await evaluate("document.querySelectorAll('.chess-moving-piece').length"), 1, 'A real move animates a piece');
    assert.equal(await evaluate("document.querySelector('[data-square=\"28\"] .chess-piece').style.visibility"), 'hidden');
    assert.equal(await evaluate("document.querySelector('.chess-moving-piece').getAnimations()[0].effect.getTiming().duration"), 250);
    assert.ok(await evaluate("document.querySelector('.chess-moving-piece').getAnimations()[0].effect.getKeyframes()[0].transform.includes('scale(1.1)')"), 'Animation preserves the selected lift');
    await wait(80);
    assert.equal(await evaluate("document.querySelectorAll('.chess-moving-piece').length"), 1, 'Move is still animating during its 250ms window');
    await evaluate("document.querySelector('[data-square=\"28\"] .chess-piece').style.removeProperty('visibility')");
    assert.equal(await evaluate("getComputedStyle(document.querySelector('[data-square=\"28\"] .chess-piece')).visibility"), 'hidden', 'Another render cannot reveal the destination during flight');
    assert.equal(await evaluate('soundStarts.length'), beforeSounds, 'Placement sound waits for landing');
    await wait(240);
    assert.equal(await evaluate("document.querySelectorAll('.chess-moving-piece').length"), 0);
    assert.equal(await evaluate("document.querySelector('[data-square=\"28\"] .chess-piece').style.visibility"), '');
    assert.equal(await evaluate('soundStarts.length'), beforeSounds + 1, 'Move sound plays after user activation');
    await updateBoard(d5); await wait(320);
    await updateBoard(played); await wait(600);
    assert.notEqual(await evaluate('soundStarts.at(-1)'), await evaluate('soundStarts.at(-2)'), 'Capture and move use distinct sounds');
    assert.equal(await evaluate("(()=>{const b=soundBuffers.find(b=>b.duration>.23),data=b.getChannelData(0);const rms=(a,z)=>{let sum=0,n=0;for(let i=Math.floor(a*b.sampleRate);i<Math.floor(z*b.sampleRate);i++){sum+=data[i]*data[i];n++}return Math.sqrt(sum/n)};return rms(.078,.098)>rms(.05,.07)*3})()"), true, 'Capture has a clearly separated second clack');
    await show(board);
    await evaluate(`document.querySelector('.chess-menu').dataset.game=${JSON.stringify(new RegExp('data-game="([^\"]+)"').exec(e4)[1])};connection.sync();themes.setMotion(true);window.mutedStarts=soundStarts.length`);
    await evaluate("document.querySelector('.chess-menu').dispatchEvent(new KeyboardEvent('keydown',{key:'Shift',bubbles:true}))");
    // Keep mute in the snapshot to model the persisted C# preference.
    await updateBoard(e4.replace('data-sound-enabled="true"', 'data-sound-enabled="false"'));
    assert.equal(await evaluate("document.querySelectorAll('.chess-moving-piece').length"), 0, 'Reduced motion skips piece animation');
    assert.equal(await evaluate('soundStarts.length===mutedStarts'), true, 'Muted moves are silent');
    await updateBoard(d5.replace('data-sound-enabled="true"', 'data-sound-enabled="false"'));
    await updateBoard(played.replace('data-sound-enabled="true"', 'data-sound-enabled="false"'));
    assert.equal(await evaluate('soundStarts.length===mutedStarts'), true, 'Muted captures are silent too');
    await show(beforeCastle);
    await evaluate('themes.setMotion(false)');
    await updateBoard(castle);
    assert.equal(await evaluate("document.querySelectorAll('.chess-moving-piece').length"), 2, 'Castling animates king and rook');
    await evaluate('themes.setMotion(true)');
    await wait(25);
    assert.equal(await evaluate("document.querySelectorAll('.chess-moving-piece').length"), 0, 'Changing motion preference cancels active animations');
    await show(settings);
    await wait(60);
    assert.equal(await evaluate("document.querySelector('.chess-settings').scrollWidth > document.querySelector('.chess-settings').clientWidth"), false);
    assert.equal(await evaluate("document.querySelector('.chess-settings').textContent.includes('Resign')"), false, 'FF removed');
    assert.deepEqual(await evaluate("[...document.querySelectorAll('#chess-side option')].map(o=>o.textContent)"), ['White', 'Black']);
    assert.equal(await evaluate("document.getElementById('chess-side').value"), 'white');
    assert.deepEqual(await evaluate("[...document.querySelectorAll('#chess-difficulty option')].map(o=>o.textContent)"), ['Easy', 'Medium', 'Hard']);
    assert.equal(await evaluate("document.querySelector('.chess-settings').textContent.includes('Board controls')"), false, 'Only the Board controls settings entry was removed');
    await screenshot('settings');
    await show(soundSettings);
    assert.equal(await evaluate("document.querySelector('[aria-label=\"Move sounds\"]').getAttribute('role')"), 'switch');
    await show(settings);
    await evaluate(`(()=>{const template=document.createElement('template');template.innerHTML=${JSON.stringify(confirmation)};document.querySelector('.chess-menu').append(template.content.querySelector('dialog'));connection.confirm()})()`);
    assert.equal(await evaluate("document.querySelector('dialog').matches(':modal')"), true);
    assert.equal(await evaluate('document.activeElement.textContent'), 'Cancel');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.chess-confirm'),'::backdrop').backgroundColor"), 'rgba(0, 0, 0, 0)');
    await evaluate("document.querySelector('dialog').close();connection.dispose();disconnect()");
    // Exercise the actual animated host without the static screenshot layout overrides.
    await evaluate(`document.getElementById('fixture-layout').remove();themes.setMotion(false);document.body.innerHTML='<div class="sphere-area" id="opening-area" style="position:absolute"><div class="dynamic-sphere is-away" id="opening-sphere"><div class="sphere-content"><header class="sphere-header"><h1>Chess</h1></header><div class="hit-menu-host"><div class="hit-menu-measure"><section class="hit-menu-panel" data-hit-menu="dev.hit.chess:chess" id="opening-panel"><div class="hit-menu-loading" data-hit-layout-pending="true"></div></section></div></div></div></div></div>';window.resizeCalls=[];window.phase='closed';const area=document.getElementById('opening-area');nativeSphere.observe(area,{async invokeMethodAsync(name,...args){if(name==='ResizeWindow'){resizeCalls.push({phase,w:args[0],h:args[1]});area.style.left=(800-args[0])+'px';area.style.top=(780-args[1])+'px';return{x:1,y:1}}}});`);
    await wait(80);
    await evaluate("phase='waiting';document.getElementById('opening-sphere').className='dynamic-sphere is-open'");
    await wait(120);
    assert.ok(await evaluate("Math.max(...resizeCalls.filter(c=>c.phase==='waiting').map(c=>c.h),0)") < 200, 'Loading styles cannot expand the window using unstyled markup');
    await evaluate(`phase='opening';document.getElementById('opening-panel').innerHTML=${JSON.stringify(board)};window.anchors=[];window.sampling=true;requestAnimationFrame(()=>requestAnimationFrame(function sample(){const r=document.getElementById('opening-sphere').getBoundingClientRect();anchors.push({x:r.right,y:r.bottom});if(sampling)requestAnimationFrame(sample)}));`);
    await wait(850);
    await evaluate('sampling=false');
    const sizing = await evaluate("({widths:[...new Set(resizeCalls.filter(c=>c.phase==='opening').map(c=>c.w))],heights:[...new Set(resizeCalls.filter(c=>c.phase==='opening').map(c=>c.h))],dx:Math.max(...anchors.map(p=>p.x))-Math.min(...anchors.map(p=>p.x)),dy:Math.max(...anchors.map(p=>p.y))-Math.min(...anchors.map(p=>p.y))})");
    assert.ok(sizing.widths.length <= 2 && sizing.heights.length <= 2, 'Opening reserves one stable native canvas: ' + JSON.stringify(sizing));
    assert.ok(sizing.dx < 2 && sizing.dy < 2, 'The anchor stays still throughout opening: ' + JSON.stringify(sizing));
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.sphere-content')).transform"), 'none');
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.sphere-content')).filter"), 'none');
    await evaluate("phase='closing';document.getElementById('opening-sphere').className='dynamic-sphere is-away'");
    await wait(850);
    assert.equal(await evaluate("document.getElementById('opening-area').hasAttribute('data-hit-canvas')"), false, 'Closing releases the reserved canvas');
    await evaluate("themes.setMotion(true);phase='instant';document.getElementById('opening-sphere').className='dynamic-sphere is-open'");
    await wait(80);
    assert.ok(await evaluate("document.getElementById('opening-sphere').getBoundingClientRect().width") > 400, 'Reduced motion opens directly at the measured size');
    await evaluate("nativeSphere.disconnect(document.getElementById('opening-area'))");
    assert.deepEqual(await evaluate('errors'), []);
    console.log('PASS: 3 themes, 250ms moves without destination flash, pacing/audio, capture strip, annotations, ready-only opening, fixed native canvas and anchor, reduced motion and cleanup.');
} finally { try { await send('Browser.close'); } catch {} browser.kill(); }
