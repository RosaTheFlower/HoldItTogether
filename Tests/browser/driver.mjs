import {spawn} from 'node:child_process';
import {createWriteStream,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {pathToFileURL} from 'node:url';
import {setTimeout as wait} from 'node:timers/promises';

export async function openBrowser(directory) {
    mkdirSync(directory,{recursive:true});
    const browser=spawn('C:/Program Files/Google/Chrome/Application/chrome.exe',['--headless','--no-sandbox','--disable-gpu','--no-first-run','--allow-file-access-from-files','--remote-debugging-pipe','--window-size=1200,1100','--user-data-dir='+resolve(directory,'profile')],{stdio:['ignore','ignore','pipe','pipe','pipe'],windowsHide:true});
    browser.stderr.pipe(createWriteStream(resolve(directory,'chromium.log')));
    let sequence=0,buffer='';const pending=new Map();
    browser.stdio[4].on('data',data=>{
        buffer+=data.toString();let end;
        while((end=buffer.indexOf('\0'))!==-1){const raw=buffer.slice(0,end);buffer=buffer.slice(end+1);if(!raw)continue;const message=JSON.parse(raw),entry=pending.get(message.id);if(entry){clearTimeout(entry.timer);pending.delete(message.id);message.error?entry.reject(new Error(JSON.stringify(message.error))):entry.resolve(message.result);}}
    });
    function send(method,params={},sessionId){return new Promise((resolve,reject)=>{const id=++sequence,timer=setTimeout(()=>{pending.delete(id);reject(new Error('CDP timeout: '+method))},10000);pending.set(id,{resolve,reject,timer});browser.stdio[3].write(JSON.stringify({id,method,params,...(sessionId?{sessionId}:{})})+'\0')});}
    const {targetId}=await send('Target.createTarget',{url:pathToFileURL(resolve(directory,'index.html')).href});
    const {sessionId}=await send('Target.attachToTarget',{targetId,flatten:true});
    const command=(method,params)=>send(method,params,sessionId);
    async function evaluate(expression){const {result,exceptionDetails}=await command('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true,replMode:true});if(exceptionDetails)throw new Error(JSON.stringify(exceptionDetails));return result.value;}
    for(let i=0;i<100&&!await evaluate('window.ready===true');i++)await wait(30);
    return {command,evaluate,async close(){try{await send('Browser.close')}catch{}browser.kill()}};
}
