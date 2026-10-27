let source;
export async function renderIcon() {
    source??=fetch(new URL('../icons/hit.svg',import.meta.url)).then(response=>{if(!response.ok)throw new Error('Could not load the HIT logo.');return response.text();});
    const probe=document.createElement('div');
    probe.className='sphere-area';probe.setAttribute('aria-hidden','true');
    probe.style.cssText='position:fixed;left:-1000px;top:-1000px;visibility:hidden;pointer-events:none';
    probe.innerHTML='<div class="dynamic-sphere is-away" style="transition:none;--hit-pointer-x:0;--hit-pointer-y:0;--hit-pointer-abs-x:0;--hit-pointer-abs-y:0"></div>';
    document.body.append(probe);
    let background,eyes;
    try {
        const face=probe.firstElementChild;
        background=getComputedStyle(face).backgroundColor;
        eyes=getComputedStyle(face,'::before').backgroundColor;
    }finally{probe.remove();}
    const doc=new DOMParser().parseFromString(await source,'image/svg+xml');
    doc.querySelector('[data-logo-surface]').setAttribute('fill',background);
    doc.querySelector('[data-logo-eyes]').setAttribute('fill',eyes);
    const svg=new XMLSerializer().serializeToString(doc);
    const img=new Image();img.src='data:image/svg+xml;charset=utf-8,'+encodeURIComponent(svg);await img.decode();
    const canvas=document.createElement('canvas');canvas.width=canvas.height=256;
    canvas.getContext('2d').drawImage(img,0,0,256,256);
    return {svg,png:canvas.toDataURL('image/png').split(',')[1]};
}
export async function sync(reference) {
    const icon=await renderIcon();
    let link=document.querySelector('link[data-hit-app-icon]');
    if(!link){link=document.createElement('link');link.rel='icon';link.dataset.hitAppIcon='true';document.head.append(link);}
    link.href='data:image/svg+xml;charset=utf-8,'+encodeURIComponent(icon.svg);
    await reference.invokeMethodAsync('SetApplicationIcon',icon.png);
}
