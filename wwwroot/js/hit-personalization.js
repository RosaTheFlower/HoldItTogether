export function connectPersonalizationPreview(sphere){
    let concealed=false,previous=null;
    function refresh(){
        const hidden=sphere.dataset.hitPersonalizationHidden==='true';
        if(hidden===concealed)return;
        concealed=hidden;
        const button=sphere.querySelector('.sphere-action-preview');
        if(!hidden)for(const node of sphere.querySelectorAll('.sphere-heading,.hit-menu-measure'))node.inert=false;
        if(hidden){previous=document.activeElement;button?.focus({preventScroll:true});}
        else if(previous?.isConnected&&!previous.closest('[hidden],[inert]'))previous.focus({preventScroll:true});
        else button?.focus({preventScroll:true});
        for(const node of sphere.querySelectorAll('.sphere-heading,.hit-menu-measure'))node.inert=hidden;
    }
    const observer=new MutationObserver(refresh);
    observer.observe(sphere,{attributes:true,attributeFilter:['data-hit-personalization-hidden']});refresh();
    return()=>{observer.disconnect();for(const node of sphere.querySelectorAll('.sphere-heading,.hit-menu-measure'))node.inert=false;};
}
