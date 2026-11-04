import net from 'node:net';
import { call, close as closeQ } from './qc.mjs';
const PORT = Number(process.env.DSH_BRIDGE_PORT || 47777);
export function send(cmd, params = {}, timeout = 6000) {
  return new Promise((res) => {
    const s = net.connect(PORT, '127.0.0.1');
    s.setNoDelay(true);
    let buf = '';
    const t = setTimeout(() => { try { s.destroy(); } catch {} res({ ok: false, error: 'timeout' }); }, timeout);
    s.on('data', (d) => { buf += d.toString('utf8'); const i = buf.indexOf('\n'); if (i < 0) return; clearTimeout(t); try { res(JSON.parse(buf.slice(0, i))); } catch { res({ ok: false, error: 'bad json' }); } try { s.end(); } catch {} });
    s.on('error', (e) => { clearTimeout(t); res({ ok: false, error: e.message }); });
    s.write(JSON.stringify({ cmd, params }) + '\n');
  });
}
export async function duo(cmd1, p1, cmd2, p2, delay = 120) {
  const a = send(cmd1, p1);
  await new Promise((r) => setTimeout(r, delay));
  const b = send(cmd2, p2);
  const [ra, rb] = await Promise.all([a, b]);
  return { first: ra, second: rb };
}
