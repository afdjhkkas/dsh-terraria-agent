import { call } from './qc.mjs';
export const sleep = ms => new Promise(s=>setTimeout(s,ms));
export const tile = async (x,y) => (await call('tile',{x,y})).data;
export const ppos = async () => (await call('state')).data.player.tile.map(v=>+v.toFixed(2));
export async function wallCells(cells, type=93, {guard=90}={}) {
  let rem = cells.map(c=>[...c]);
  for (let g=0; g<guard && rem.length; g++){
    const p = await ppos(); let done=0;
    for (const [x,y] of rem.slice()){
      if (Math.abs(x-p[0]) < 1.0 && Math.abs(y-p[1]) < 1.2) continue;
      if (Math.abs(x-p[0]) > 3.8 || Math.abs(y-p[1]) > 5.5) continue;
      const t = await tile(x,y);
      if (t.wall === type) { rem = rem.filter(c=>!(c[0]===x&&c[1]===y)); continue; }
      await call('wall',{x,y,type}); await sleep(900);
      if ((await tile(x,y)).wall === type) { rem = rem.filter(c=>!(c[0]===x&&c[1]===y)); done++; }
    }
    if (!done && rem.length){
      const [nx,ny] = rem[0];
      await call('moveto',{x:nx+1.5,ms:1800}); await sleep(400);
      const p2 = await ppos();
      if (Math.abs(p2[0]-(nx+1.5))>1.2 || Math.abs(p2[1]-287.7)>1.5){
        await call('jump',{ms:420}); await call('move',{dir:'right',ms:200}); await sleep(800);
      }
    }
  }
  return rem;
}
