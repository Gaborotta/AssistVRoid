using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices; // DllImport

namespace AssistVRoid
{
    public class Input
    {
        [DllImport("user32")]
        private static extern Int16 GetAsyncKeyState(int vKey);

        public enum VK : UInt32
        {
            VK_F10 = 0x79,        // F10 キー
            VK_F11 = 0x7A,        // F11 キー
            VK_MULTIPLY = 0x6A,   // *　キー
            VK_LCTRL = 0xA2,
            VK_RCTRL = 0xA3,
            VK_R = 0x52,
            VK_0 = 0x30,
            VK_1 = 0x31,
            VK_2 = 0x32,
            VK_3 = 0x33,
            VK_4 = 0x34,
            VK_5 = 0x35,
            VK_6 = 0x36,
            VK_7 = 0x37,
            VK_8 = 0x38,
            VK_9 = 0x39,
        }

        public enum KeyState
        {
            Free,
            Down_Now,
            Down,
            Up,
        }

        public class KeyStatus
        {
            public KeyState state = KeyState.Free;
            UInt32 vk_id; // 仮想キーコード(WinAPI)
            string key;

            public KeyStatus( string key, UInt32 vk_id )
            {
                this.key = key;
                this.vk_id = vk_id;
            }

            public void Update()
            {
                var key_state = GetAsyncKeyState((int)vk_id);
                if (state == KeyState.Free)
                {
                    if (key_state < 0) // キーが押されている
                    {
                        state = KeyState.Down_Now;
                    }
                }
                else if (state == KeyState.Down || state == KeyState.Down_Now)
                {
                    if (key_state < 0)
                    {
                        state = KeyState.Down;
                    }
                    else
                    {
                        state = KeyState.Up;
                    }
                }
                else if (state == KeyState.Up)
                {
                    if (key_state < 0)
                    {
                        state = KeyState.Down_Now;
                    }
                    else
                    {
                        state = KeyState.Free;
                    }
                }

            }
        }

        Dictionary<string, KeyStatus> keys = new Dictionary<string, KeyStatus>();

        public Input()
        {

        }

        public void Update()
        {
            foreach( var ks in keys )
            {
                ks.Value.Update();
            }
        }

        // 監視するキーを追加する
        public void AddTargetKey( string key_name, VK vk )
        {
            var ks = new KeyStatus(key_name, (UInt32)vk);
            keys.Add(key_name, ks);
        }

        public KeyStatus GetKeyStatus( string key_name )
        {
            if (!keys.ContainsKey(key_name)) return null;
            return keys[key_name];
        }
    }
}
