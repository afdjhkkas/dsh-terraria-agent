// 常驻连接版桥客户端。
//
// 为什么需要它：scratch/q.mjs 每调一次都 net.connect 新建一条 TCP，
// 握手 + 关闭的开销 10~50ms，配上 50ms 的轮询间隔，实际周期变成 100~200ms。
// 走路时这个延迟就是"一顿一顿"的根源 —— 每次读到的位置都比预期晚，
// 刹车点算不准，只能一小段一小段试探着走。
//
// 桥的协议是「一行请求 → 一行响应」，回包里没有请求 id，
// 所以这里按「串行」用：一条连接、同时只放一个请求在路上，收到第一行就当它的回包。
// 真并发会被这条规则挡在队列里排队，不会错配。
import net from 'node:net';

let sock = null;
let buf = '';
let current = null;          // { res, timer }
const queue = [];            // 等连接的请求

function ensure() {
  if (sock && !sock.destroyed) return sock;
  sock = net.connect(PORT, '127.0.0.1');
  sock.setNoDelay(true);

  sock.on('data', (d) => {
    buf += d.toString('utf8');
    const i = buf.indexOf('\n');
    if (i < 0) return;
    const line = buf.slice(0, i);
    buf = buf.slice(i + 1);
    const w = current;
    current = null;
    if (w) {
      clearTimeout(w.timer);
      try { w.res(JSON.parse(line)); } catch { w.res({ ok: false, error: 'bad json', raw: line.slice(0, 200) }); }
    }
    pump();
  });

  const failAll = (why) => {
    if (current) { clearTimeout(current.timer); current.res({ ok: false, error: why }); current = null; }
    while (queue.length) queue.shift()({ ok: false, error: why });
  };
  sock.on('error', () => { try { sock.destroy(); } catch {} sock = null; failAll('socket error'); });
  sock.on('close', () => { sock = null; failAll('closed'); });

  return sock;
}

function pump() {
  if (current || !queue.length) return;
  const s = ensure();
  const job = queue.shift();
  current = job;
  job.timer = setTimeout(() => {
    if (current === job) { current = null; job.res({ ok: false, error: 'timeout' }); pump(); }
  }, job.timeout);
  try {
    s.write(JSON.stringify({ cmd: job.cmd, params: job.params }) + '\n');
  } catch (e) {
    clearTimeout(job.timer);
    current = null;
    job.res({ ok: false, error: e.message });
    pump();
  }
}

// 端口：默认 47777（主客户端）。第二个客户端起来后会另开一个端口
// （桥的 -dsh-bridge-port，实际部署在 47779），要控制那个角色就连它。
const PORT = Number(process.env.DSH_BRIDGE_PORT || 47777);

export function call(cmd, params = {}, timeout = 8000) {
  return new Promise((res) => {
    queue.push({ cmd, params, timeout, res, timer: null });
    pump();
  });
}

// ok:false 时必须炸出来，不能当成功。
// 事故：mine 回 {"ok":false,"error":"够不到：离这一格超过 7 格"}，
// 脚本只判 `r.data` 有没有值，于是把"根本没挥动"记成了"挖了但没挖掉"，
// 一路排查到「是不是次数不够」——其实是够不到。命令回 ok 不等于操作发生过。
export function must(cmd, params = {}, timeout = 8000) {
  return call(cmd, params, timeout).then((r) => {
    if (!r || r.ok !== true) throw new Error(cmd + ' 失败: ' + ((r && r.error) || 'unknown'));
    return r.data;
  });
}

export function close() {
  if (sock) { try { sock.end(); } catch {} sock = null; }
}
