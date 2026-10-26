import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { readFileSync, writeFileSync, createWriteStream, mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { setTimeout as wait } from 'node:timers/promises';

const root = resolve('obj/extensions-browser'); mkdirSync(root, { recursive: true });
const markup = readFileSync('obj/extensions-preview.html', 'utf8');
const fixture = `<!doctype html><html lang="en"><head><meta charset="utf-8">${['app.css', 'css/dynamic-sphere.css', 'css/hit-menus.css', 'css/menus.css', 'css/theme-base.css'].map(file => `<link rel="stylesheet" href="../../wwwroot/${file}">`).join('')}
<style>body{background:#b1b5bd}.sphere-area{position:absolute;left:40px;top:40px}</style></head><body>
<div class="sphere-area" id="area"><div class="dynamic-sphere is-open" id="sphere"><div class="sphere-content">
<header class="sphere-header"><h1 class="sphere-title">Extensions</h1><div class="sphere-actions"><button class="sphere-action">×</button></div></header>
<div class="hit-menu-host"><div class="hit-menu-measure"><section class="hit-menu-panel" data-hit-menu="HoldItTogether.Components.HitMenus.HitMenuConfiguracoes">${markup}</section></div></div>
</div></div></div>
<script type="module">
import * as themes from '../../wwwroot/js/hit-themes.js';
import * as extensions from '../../wwwroot/js/hit-extensions.js';
import {observe} from '../../wwwroot/js/dynamic-sphere.js';
window.errors=[];window.calls=[];window.themes=themes;window.extensions=extensions;
addEventListener('error',event=>errors.push(event.message));
window.DotNet={createJSStreamReference(file){return {name:file.name,size:file.size}}};
extensions.connectDrop({async invokeMethodAsync(...args){calls.push(args)}});
themes.setMotion(true);
observe(document.getElementById('area'), {async invokeMethodAsync(name){if(name==='ResizeWindow')return{x:.5,y:.5}}});
window.ready=true;
</script></body></html>`;
writeFileSync(resolve(root, 'index.html'), fixture);

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
    for (const file of ['HIT-OBSIDIAN-EYES.css', 'HIT-OLED-OFF-WHITE.css', 'HIT-LIGHT.css']) {
        await evaluate(`themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/' + file, 'utf8'))})`);
        await wait(160);
        assert.equal(await evaluate("document.documentElement.scrollHeight > innerHeight"), false);
        assert.equal(await evaluate("document.querySelector('.extensions-view').scrollWidth > document.querySelector('.extensions-view').clientWidth"), false);
        assert.equal(await evaluate("document.querySelectorAll('.extension-row').length"), 4);
        const shot = await command('Page.captureScreenshot', { format: 'png' });
        writeFileSync(resolve(root, file + '.png'), Buffer.from(shot.data, 'base64'));
    }
    await evaluate(`window.beforeHeader=getComputedStyle(document.querySelector('.sphere-title')).color;
        extensions.styles('dev.test:one',[{url:'extensions/dev.test/1.0.0/Layout/css/menu.css',css:':root{--sample:1}button{color:rgb(255,0,0)}@keyframes blink{to{opacity:.5}}.eye{animation:blink 1s}'}]);`);
    assert.equal(await evaluate("getComputedStyle(document.querySelector('.sphere-title')).color===beforeHeader"), true);
    assert.equal(await evaluate("document.querySelector('[data-hit-extension]').textContent.includes('hit-theme-dev-test-one-blink')"), true);
    await evaluate("extensions.removeStyles('dev.test:one')");
    assert.equal(await evaluate("document.querySelectorAll('[data-hit-extension]').length"), 0);
    await evaluate(`window.dropFile=name=>{const transfer=new DataTransfer();transfer.items.add(new File(['package'],name));document.getElementById('sphere').dispatchEvent(new DragEvent('dragenter',{bubbles:true,dataTransfer:transfer}));document.getElementById('sphere').dispatchEvent(new DragEvent('drop',{bubbles:true,cancelable:true,dataTransfer:transfer}));};dropFile('example.hitmenu')`);
    await wait(50);
    assert.equal(await evaluate("calls[0][0]"), 'ImportDropped');
    assert.deepEqual(await evaluate("calls[0][1]"), { name: 'example.hitmenu', size: 7 });
    assert.equal(await evaluate("document.querySelectorAll('.hit-drop-overlay').length"), 0);
    await evaluate("dropFile('example.txt')"); await wait(30);
    assert.equal(await evaluate("calls[1][0]"), 'DropError');
    await evaluate("extensions.disconnectDrop();dropFile('ignored.hitmenu')"); await wait(30);
    assert.equal(await evaluate('calls.length'), 2);
    assert.deepEqual(await evaluate('errors'), []);
    console.log('PASS: Extensions layout in all bundled themes, CSS scoping/namespacing, drop stream handoff, invalid files and listener disposal.');
} finally { try { await send('Browser.close'); } catch {} browser.kill(); }
