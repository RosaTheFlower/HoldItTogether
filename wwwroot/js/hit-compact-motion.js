// Match the board/readout's visual resize to its surrounding native surface.
export function connectCompactMotion(sphere) {
    let previous=null,animation=null,disposed=false;
    const localRect=element=>{
        const outer=sphere.getBoundingClientRect(),r=element.getBoundingClientRect();
        return {x:r.left-outer.left,y:r.top-outer.top,width:r.width,height:r.height};
    };
    function update(panel) {
        if(disposed)return;
        const root=panel?.querySelector('.chess-menu:not(.shows-settings),.clock-menu[data-clock-view="main"]');
        const focus=root?.querySelector('.chess-board-frame,.clock-digits');
        if(!focus){animation?.cancel();animation=null;previous=null;delete sphere.dataset.hitCompactMotion;return;}
        const key=panel.dataset.hitMenu,compact=root.classList.contains('is-compact');
        if(previous?.key===key&&previous.compact!==compact&&sphere.classList.contains('is-open')&&
            getComputedStyle(sphere).transitionDuration.split(',').some(value=>parseFloat(value)>0)&&
            !matchMedia('(prefers-reduced-motion: reduce)').matches) {
            const from=previous.rect;
            animation?.cancel();
            const to=localRect(focus);
            if(to.width>0&&to.height>0) {
                sphere.dataset.hitCompactMotion='true';
                const easing=getComputedStyle(sphere).getPropertyValue('--sphere-ease').trim()||'ease';
                animation=focus.animate([
                    {transformOrigin:'0 0',transform:`translate(${from.x-to.x}px,${from.y-to.y}px) scale(${from.width/to.width},${from.height/to.height})`},
                    {transformOrigin:'0 0',transform:'translate(0px,0px) scale(1,1)'}
                ],{duration:820,easing});
                const current=animation;
                current.finished.then(()=>{
                    if(animation!==current||disposed)return;
                    animation=null;delete sphere.dataset.hitCompactMotion;
                    previous={key,compact,rect:localRect(focus)};
                }).catch(()=>{});
            }
        }
        previous={key,compact,rect:localRect(focus)};
    }
    return {update,disconnect(){disposed=true;animation?.cancel();delete sphere.dataset.hitCompactMotion;}};
}
