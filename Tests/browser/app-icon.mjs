import assert from 'node:assert/strict';
import {readFileSync,writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {openBrowser} from './driver.mjs';
const root=resolve('obj/app-icon-browser');mkdirSync(root,{recursive:true});
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><head>
<link rel="stylesheet" href="../../wwwroot/css/dynamic-sphere.css"></head><body><script type="module">
import * as themes from '../../wwwroot/js/hit-themes.js';import * as icons from '../../wwwroot/js/hit-app-icon.js';
window.themes=themes;window.icons=icons;window.calls=[];window.reference={async invokeMethodAsync(...args){calls.push(args)}};window.ready=true;
</script></body></html>`);
const browser=await openBrowser(root);
try {
    const colors=[];
    for(const theme of ['HIT-OBSIDIAN-EYES.css','HIT-OLED-OFF-WHITE.css','HIT-LIGHT.css']) {
        await browser.evaluate(`themes.applyBase(${JSON.stringify(readFileSync('wwwroot/themes/'+theme,'utf8'))});await icons.sync(reference)`);
        const color=await browser.evaluate("(()=>{const svg=new DOMParser().parseFromString(decodeURIComponent(document.querySelector('link[data-hit-app-icon]').href.split(',')[1]),'image/svg+xml');return [svg.querySelector('[data-logo-surface]').getAttribute('fill'),svg.querySelector('[data-logo-eyes]').getAttribute('fill')]})()");
        colors.push(color);
    }
    assert.equal(new Set(colors.map(color=>color[0])).size,3,'Each theme updates the icon surface');
    assert.notEqual(colors[0][1],colors[2][1],'Light switches the face to dark eyes');
    assert.equal(await browser.evaluate("calls.length===3&&calls.every(c=>c[0]==='SetApplicationIcon'&&c[1].startsWith('iVBOR'))"),true);
    assert.equal(await browser.evaluate("document.querySelectorAll('.sphere-area').length"),0,'Theme probes never remain on screen');
    console.log('PASS: SVG face, live Dark/OLED/Light icons, native PNG handoff and probe cleanup.');
}finally{await browser.close();}
