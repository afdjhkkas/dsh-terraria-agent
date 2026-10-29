using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace DshAiMini
{
    /// <summary>
    /// 面板的开关与挂载。
    ///
    /// 快捷键用边沿检测而不是电平：按住不放只算一次，否则一帧开关几十次。
    /// 面板本身挂在 "Vanilla: Mouse Text" 之前，这样它盖在游戏 UI 之上、
    /// 又不会挡住鼠标指针。
    /// </summary>
    public class AiSystem : ModSystem
    {
        public static readonly Keys PanelKey = Keys.F7;

        private static bool _prevPanelKey;
        private static UserInterface _ui;
        private static AiPanel _panel;
        public static bool IsOpen { get; private set; }

        public override void Load()
        {
            if (Main.dedServ) return;
            _panel = new AiPanel();
            _ui = new UserInterface();
            _ui.SetState(null);
        }

        public override void Unload()
        {
            _panel = null;
            _ui = null;
        }

        public static void TogglePanel(bool? force = null)
        {
            bool want = force ?? !IsOpen;
            if (want == IsOpen) return;
            IsOpen = want;
            _ui?.SetState(want ? _panel : null);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.dedServ) return;

            bool down = Main.keyState.IsKeyDown(PanelKey);
            if (down && !_prevPanelKey) TogglePanel();
            _prevPanelKey = down;

            if (IsOpen) _ui?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.dedServ || _ui == null) return;

            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx < 0) idx = layers.Count;

            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "DshAiMini: Panel",
                () =>
                {
                    if (IsOpen) _ui.Draw(Main.spriteBatch, Main.gameTimeCache);
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
