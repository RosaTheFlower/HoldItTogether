// DOM scroll ownership. Unconsumed boundary wheel gestures reach the HIT host.
export function consumesWheel(element,event) {
    if (event.ctrlKey || event.target.closest?.('input,select,textarea,button,a,summary,[role=combobox],[contenteditable=true],dialog[open]')) return true;
    const config=element.closest('[data-hit-settings-workspace],.hit-menu-host[data-hit-configuration=true]');
    const style=getComputedStyle(element);
    if(config&&((/auto|scroll/.test(style.overflowY)&&element.scrollHeight>element.clientHeight+1)||(/auto|scroll/.test(style.overflowX)&&element.scrollWidth>element.clientWidth+1)))return true;
    const direction=Math.sign(event.deltaY);
    if (!direction) return false;
    return direction<0 ? element.scrollTop>1 : element.scrollHeight-element.clientHeight-element.scrollTop>1;
}
