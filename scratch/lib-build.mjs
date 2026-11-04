import { call } from './qc.mjs';
export const sleep = ms => new Promise(s=>setTimeout(s,ms));
export const tile = async (x,y) => (await call('tile',{x,y})).data;
export const ppos = async () => (await call('state')).data.player.tile.map(v=>+v.toFixed(2));
export async function placeCells(cells, type, {reach=3.6, guard=80}={}) {
  let rem = cells.map(c=>[...c]); const log=[];
  for (let g=0; g<guard && rem.length; g++){
    const p = await ppos(); let done=0;
    for (const [x,y] of rem.slice()){
      const d = Math.abs(x - p[0]);
      const dy = Math.abs(y - p[1]);
      if (d < 1.2) continue;
      if (d > reach || dy > reach) continue;
      const t = await tile(x,y);
      if (t.hasTile && (type===null || t.type===type)) { rem = rem.filter(c=>!(c[0]===x&&c[1]===y)); continue; }
      if (t.hasTile && [3,73].includes(t.type)) { await call('mine',{x,y}); await sleep(700); }
      await call('place',{x,y,type}); await sleep(950);
      const t2 = await tile(x,y);
      if (t2.hasTile && (type===null || t2.type===type)) { rem = rem.filter(c=>!(c[0]===x&&c[1]===y)); done++; }
    }
    if (!done && rem.length){
      const [nx,ny] = rem[0];
      await call('moveto',{x:nx+2.0, ms:2000}); await sleep(500);
      const p2 = await ppos();
      if (Math.abs(p2[0]-(nx+2.0))>1.5){
        await call('jump',{ms:400}); await sleep(700);
        await call('moveto',{x:nx+2.0, ms:2000}); await sleep(400);
      }
    }
  }
  log.push('remaining='+rem.length);
  return { rem, log };
}
