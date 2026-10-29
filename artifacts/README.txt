打包产物
========

这里放的是"别人 clone 下来直接能装"的 .tmod，不是开发中间产物。

DshTerrariaBridge.tmod   桥。装上之后 AI 才能用 110 个命令操作游戏。
DshAiMini.tmod           单 mod 版 AI。装上、填一个 API key 就能对话+动手，
                         不需要 DSH、不需要 node、不需要桥。

两个怎么配合
------------
只想试试聊天和工具调用          → 只装 DshAiMini
想让 AI 真的长时间替你玩         → 两个都装（DshAiMini 通过桥调命令）

怎么装
------
把 .tmod 复制到
  <我的文档>/My Games/Terraria/tModLoader/Mods/
然后启动 tModLoader 启用即可。

这两个包是怎么来的
------------------
DshTerrariaBridge：见仓库根目录 README「从源码构建并安装」
DshAiMini         ：见 ai-mod/README.md（同一套流程，工程名换成 DshAiMini）

注意：包和源码不同步是这里最容易犯的错。
     改了 .cs 一定要重新打包并把新包复制到这里，
     否则别人装到的是旧行为（历史上就因此丢过一次坐骑修复）。
     跑 npm run doctor 会检查"包是否落后于源码"。
