import { call } from './q.mjs';
const sleep = ms => new Promise(r => setTimeout(r, ms));

export async function state() { return (await call('state', {})).data; }

export async function walkTiles(dir, tiles, deadlineMs = 40000) {
  const st0 = await state();
  const x0 = st0.player.pos[0], y0 = st0.player.pos[1];
  const tgt = dir === 'right' ? x0 + tiles : x0 - tiles;
  const t0 = Date.now();
  let still = 0;
  await call('move', { dir, ms: Math.min(3000, Math.abs(tiles) * 400 + 300) });
  while (Date.now() - t0 < deadlineMs) {
    await sleep(60);
    const s = await state();
    const x = s.player.pos[0], vx = Math.abs(s.player.velocity[0]);
    const left = Math.abs(tgt - x);
    const brake = vx * 0.62 + 0.10;
    if (left <= brake) { await call('stop', {}); break; }
    if (Math.abs(x - x0) < 0.05 && Math.abs(s.player.velocity[0]) < 0.01) {
      if (++still >= 6) { await call('stop', {}); return { ok: false, reason: 'blocked', from: x0, to: x }; }
    } else still = 0;
  }
  await sleep(250);
  const s1 = await state();
  return { ok: true, from: +x0.toFixed(2), to: +s1.player.pos[0].toFixed(2), dy: +(s1.player.pos[1] - y0).toFixed(2) };
}

export async function jumpN(n, ms = 2200) {
  for (let i = 0; i < n; i++) await call('jump', { ms });
}
