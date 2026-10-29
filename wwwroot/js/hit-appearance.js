export async function validateImage(url) {
    const image=new Image();
    try{return await new Promise(resolve=>{
        const timer=setTimeout(()=>resolve(false),5000);
        image.onload=()=>{clearTimeout(timer);resolve(image.naturalWidth>0&&image.naturalHeight>0&&image.naturalWidth<=4096&&image.naturalHeight<=4096);};
        image.onerror=()=>{clearTimeout(timer);resolve(false);};image.src=url;
    });}finally{image.onload=image.onerror=null;image.removeAttribute('src');}
}
