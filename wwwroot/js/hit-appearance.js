import {setDeleteDialog} from './hit-themes.js';
export function dialog(element,visible){setDeleteDialog(element,visible);}
const validations=new Map();
export function cancelValidations(scope){validations.get(scope)?.();validations.delete(scope);}
export async function validateImage(url,scope) {
    const image=new Image();
    try{return await new Promise(resolve=>{
        const timer=setTimeout(()=>resolve(false),5000);
        if(scope)validations.set(scope,()=>{clearTimeout(timer);resolve(false);});
        image.onload=()=>{clearTimeout(timer);resolve(image.naturalWidth>0&&image.naturalHeight>0&&image.naturalWidth<=4096&&image.naturalHeight<=4096);};
        image.onerror=()=>{clearTimeout(timer);resolve(false);};image.src=url;
    });}finally{if(scope)validations.delete(scope);image.onload=image.onerror=null;image.removeAttribute('src');}
}
export async function validateVideo(url,scope){
    const video=document.createElement('video');video.muted=true;video.playsInline=true;video.preload='auto';
    let timer;
    try{return await new Promise(resolve=>{
        const done=value=>{clearTimeout(timer);resolve(value);};
        timer=setTimeout(()=>done(null),10000);
        if(scope)validations.set(scope,()=>done(null));
        video.onerror=()=>done(null);
        video.onloadeddata=()=>{
            if(!video.videoWidth||!video.videoHeight||video.videoWidth>4096||video.videoHeight>4096||video.duration<=0)return done(null);
            try{
                const canvas=document.createElement('canvas');const scale=Math.min(1,1280/video.videoWidth,1280/video.videoHeight);
                canvas.width=Math.max(1,Math.round(video.videoWidth*scale));canvas.height=Math.max(1,Math.round(video.videoHeight*scale));
                canvas.getContext('2d').drawImage(video,0,0,canvas.width,canvas.height);done(canvas.toDataURL('image/jpeg',.85));
            }catch{done(null);}
        };
        video.src=url;video.load();
    });}finally{clearTimeout(timer);if(scope)validations.delete(scope);video.onloadeddata=video.onerror=null;video.pause();video.removeAttribute('src');video.load();}
}
