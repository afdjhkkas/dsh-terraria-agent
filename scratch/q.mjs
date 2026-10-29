import net from 'node:net';
export function call(cmd, params = {}, timeout = 8000) {
  return new Promise((res) => {
    const s = net.connect(47777, '127.0.0.1', () => {
      s.write(JSON.stringify({ cmd, params }) + '\n');
    });
    let buf = '';
    const t = setTimeout(() => { try{s.destroy();}catch{}; res({ ok:false, error:'timeout' }); }, timeout);
    s.on('data', d => {
      buf += d;
      if (buf.includes('\n')) {
        clearTimeout(t);
        const line = buf.split('\n')[0];
        try { res(JSON.parse(line)); } catch { res({ ok:false, error:'bad json', raw:line.slice(0,200) }); }
        s.end();
      }
    });
    s.on('error', e => { clearTimeout(t); res({ ok:false, error:e.message }); });
  });
}
