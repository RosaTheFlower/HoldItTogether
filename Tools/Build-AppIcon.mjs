import {mkdirSync,writeFileSync,readFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {openBrowser} from '../Tests/browser/driver.mjs';

// Windows needs ICO resources; the editable artwork remains the SVG source.
const root=resolve('obj/app-icon-build');mkdirSync(root,{recursive:true});
writeFileSync(resolve(root,'index.html'),`<!doctype html><html><body><script>window.ready=true</script></body></html>`);
const browser=await openBrowser(root);
try {
    const source=readFileSync('wwwroot/icons/hit.svg','utf8');
    const images=JSON.parse(await browser.evaluate(`JSON.stringify(await (async()=>{
        const img=new Image();img.src='data:image/svg+xml;charset=utf-8,'+encodeURIComponent(${JSON.stringify(source)});await img.decode();
        return [16,32,48,64,128,256].map(size=>{const c=document.createElement('canvas');c.width=c.height=size;c.getContext('2d').drawImage(img,0,0,size,size);return {size,png:c.toDataURL('image/png').split(',')[1]}});
    })())`));
    const header=Buffer.alloc(6+images.length*16);header.writeUInt16LE(1,2);header.writeUInt16LE(images.length,4);
    const parts=[];let offset=header.length;
    images.forEach(({size,png},index)=>{const bytes=Buffer.from(png,'base64'),entry=6+index*16;header[entry]=header[entry+1]=size===256?0:size;header.writeUInt16LE(1,entry+4);header.writeUInt16LE(32,entry+6);header.writeUInt32LE(bytes.length,entry+8);header.writeUInt32LE(offset,entry+12);parts.push(bytes);offset+=bytes.length;});
    writeFileSync('Resources/hit.ico',Buffer.concat([header,...parts]));
    console.log('Built Resources/hit.ico from wwwroot/icons/hit.svg (16–256 px).');
}finally{await browser.close();}
