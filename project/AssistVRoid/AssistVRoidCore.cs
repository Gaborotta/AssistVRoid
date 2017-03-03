using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Threading;
using MyLauncher;

namespace AssistVRoid
{
    public class AssistVRoidCore
    {
        public class VoiceRoidData
        {
            public string system_name; // 管理用の名前
            public string output_name; // ファイル出力時に利用される名前
            public string app_path;    // アプリ(実行ファイル)パス
            public string app_title;   // アプリタイトル

            public string app_setup_talk = "";
            public string play_button_text = " 再生";
            public string play_text_box    = ".RichEdit20W.";

            public int voice_effect_tab_offset_x = 200; // 再生ボタンからの「音声効果」タブの相対位置
            public int voice_effect_tab_offset_y = 60;

            private WindowControllerManager window_controller_manager = null;
            private volatile string save_dir_path = "";

            public class VoiceEffectTemplate
            {
                public string[] status;
            }
            public Dictionary<string, VoiceEffectTemplate> voice_effect_templates = new Dictionary<string, VoiceEffectTemplate>();

            // アプリケーションを起動する
            public  void StartApp()
            {
                if (System.IO.File.Exists(app_path))
                {
                    var wcm = GetWCM(true);
                    if (wcm.GetTop() != null) return; // 起動済みなので省略

                    EasyProcess.StartExe(app_path, true);

                    if (app_setup_talk!="")
                    {
                        Talk(app_setup_talk);
                    }

                    // 音声効果のタブをクリックしておく、押さないと音声効果の各エディットボックスが生成されてないので
                    wcm = GetWCM();
                    if (true && wcm.GetTop()!=null )
                    {
                        SetupVocieEffectTab();
                    }
                }
            }

            public void Talk( string text )
            {
                var wcm = GetWCM();

                var text_box = wcm.GetChiledByClassNameNear(play_text_box);
                text_box.SendText(text);

                var play_button = wcm.GetChiledByText(play_button_text);
                play_button.ClickL();

            }

            // アプリケーションがアクティブであるかどうか
            public bool IsActiveApp()
            {
                // todo : GetWCM の true を なくせないか → あとでアプリを手動起動されたときにうまくアクティブかどうか取得できてない（アクティブにならない）
                var wcm = GetWCM(true);
                var wc_top = wcm.GetTop();

                if (wc_top == null) return false; // 未起動
                if (wc_top.IsActive()) return true;
                return false;

            }

            // 音声を保存する
            public void SaveVoice( string dir_path )
            {
                save_dir_path = dir_path;

                var wc = new WindowControllerManager(app_title);
                if (!wc.IsGetTop())
                {
                    // アプリが起動していない
                    return;
                }

                var play_button = wc.GetChiledByText(" 音声保存");

                Thread thread = new Thread(new ThreadStart(SaveVoice_Dialog));
                thread.Start();

                play_button.ClickL();
            }

            // 音声効果のテンプレートを適応する
            public void DoVoiceEffectTemplate( string template_key )
            {
                if (!voice_effect_templates.ContainsKey(template_key)) return; // キーがないならキャンセル
                var vet = voice_effect_templates[template_key];

                var wcm = GetWCM();
                var hit_text = "音声効果";
                var voice_effect_tab = wcm.GetChiledByText(hit_text);
                if (voice_effect_tab==null)
                {
                    // 音声効果タブが押されたことがない？
                    // 押すだけ押してみる...
                    SetupVocieEffectTab(false);
                    WaitSleep.Do(100);
                    wcm = GetWCM(true);
                }

                var wc_list = new List<WindowController>();
                var MAX = 99;
                for (var i = 0; i < MAX; i++)
                {
                    var edit = wcm.GetChiledByClassNameNear(".EDIT.", i);
                    if (edit == null) break;

                    // 親の親のテキストが該当のものであれば、追加する
                    var edit_o1 = edit.GetOwner();
                    if (edit_o1 == null) continue;
                    var edit_o2 = edit_o1.GetOwner();
                    if (edit_o2 == null) continue;
                    if (edit_o2.text == hit_text)
                    {
                        wc_list.Add(edit);
                    }
                }

                if (wc_list.Count == 4)
                {
                    // 縦の順に並べ替える
                    var wc_list_tmp = new List<WindowController>();
                    while (wc_list.Count > 0)
                    {
                        int top_y = 0;
                        WindowController top_wc = null;
                        foreach (var item in wc_list)
                        {
                            var rc = item.GetRectange();
                            if (top_y == 0 || top_y < rc.Top)
                            {
                                top_wc = item;
                            }
                        }
                        wc_list_tmp.Add(top_wc);
                        wc_list.Remove(top_wc);
                    }
                    wc_list = wc_list_tmp;

                    for (var i = 0; i < 4; i++)
                    {
                        wc_list[i].SendText(vet.status[i]);
                        wc_list[i].Active();
                        WaitSleep.Do(100);
                    }
                    wc_list[0].Active();
                }
            }

            // 下記のコードが散らばるのを避けるのと、処理の重複を気にして関数にまとめている
            // var wcm = new WindowControllerManager(app_title);
            // is_update_force : true にすると強制的に、新しいWindowControllerManagerを用意する
            private WindowControllerManager GetWCM( bool is_update_force = false)
            {
                if  (window_controller_manager == null) 
                {
                    window_controller_manager = new WindowControllerManager(app_title);
                }
                else if (window_controller_manager.GetTop() == null)
                {
                    window_controller_manager = new WindowControllerManager(app_title);
                }
                else if (is_update_force) {
                    window_controller_manager = new WindowControllerManager(app_title);
                }

                return window_controller_manager;
            }

            // 音声効果のタブをクリックしておく、押さないと音声効果の各エディットボックスが生成されてないので
            private void SetupVocieEffectTab(bool is_top = true )
            {
                var wcm = GetWCM();

                // 再生ボタンの位置から、相対的に「音声効果」のボタンの場所を推定してクリックさせている
                // クリックの座標を知るために、アプリウィンドウの矩形と再生ボタンの矩形について取得
                // 座標がわかれば、あとは強制的にアプリをアクティブにしてクリック命令をする。
                var play_button = wcm.GetChiledByText(play_button_text);
                play_button.ClickL();
                var wc_top = wcm.GetTop();

                var wc_top_rc = wc_top.GetRectange();
                var play_button_rc = play_button.GetRectange();

                var x = play_button_rc.X - wc_top_rc.X + voice_effect_tab_offset_x;
                var y = play_button_rc.Y - wc_top_rc.Y + voice_effect_tab_offset_y;
                var mouse_pos = new Point(x, y);

                wc_top.Active();
                WaitSleep.Do(100);
                wc_top.ClickLActive(mouse_pos.X, mouse_pos.Y);

                // タブを戻しておく
                if (is_top)
                {
                    WaitSleep.Do(100);
                    x = play_button_rc.X - wc_top_rc.X + 20;                        // このあたりのオフセットはいい加減...
                    y = play_button_rc.Y - wc_top_rc.Y + voice_effect_tab_offset_y; //
                    mouse_pos = new Point(x, y);
                    wc_top.ClickLActive(mouse_pos.X, mouse_pos.Y);
                }
            }

            // 音声を保存する際に、別スレッドで保存ダイアログを処理するため
            private void SaveVoice_Dialog()
            {
                while (true)
                {
                    Thread.Sleep(10);
                    var wcm = new WindowControllerManager("音声ファイルの保存");
                    var dlg = wcm.GetTop();
                    if (dlg != null && wcm.GetChiledByClassName("Edit") != null)
                    {
                        var dir = save_dir_path;
                        var name_head = output_name + "_";
                        var file_type = "wav";

                        var text_box = wcm.GetChiledByClassName("Edit");
                        text_box.SendText(CreateNextPathByVoiceRoid(dir, name_head, file_type));

                        var ok_button = wcm.GetChiledByText("保存(&S)");
                        ok_button.ClickL();
                        break;
                    }
                }
            }


            // 
            private string CreateNextPathByVoiceRoid(string dir, string name_head, string type)
            {
                var format = "{0:000}";
                var i = 0;
                var s = dir + @"\" + name_head + string.Format(format, i) + "." + type;
                while (System.IO.File.Exists(s))
                {
                    i++;
                    s = dir + @"\" + name_head + string.Format(format, i) + "." + type;
                }
                return s;
            }
        }
        Input input;
        Dictionary<string, VoiceRoidData> vr_datas = new Dictionary<string, VoiceRoidData>();
        public string output_voice_wav_dir_path = "";
        Script script;

        VoiceRoidData script_target_last_vrd = null;

        public AssistVRoidCore(Input input)
        {
            this.input = input;

            // inputをForm1からうけとっているが、このクラスで生成し他方が良い気がする（多重登録に対して処理してないし、その判別の必要性もややこしい）
            input.AddTargetKey( "R", Input.VK.VK_R );
            input.AddTargetKey( "Ctrl_L", Input.VK.VK_LCTRL );
            input.AddTargetKey( "Ctrl_R", Input.VK.VK_RCTRL);
            input.AddTargetKey( "0", Input.VK.VK_0);
            input.AddTargetKey( "1", Input.VK.VK_1);
            input.AddTargetKey( "2", Input.VK.VK_2);
            input.AddTargetKey( "3", Input.VK.VK_3);
            input.AddTargetKey( "4", Input.VK.VK_4);
            input.AddTargetKey( "5", Input.VK.VK_5);
            input.AddTargetKey( "6", Input.VK.VK_6);
            input.AddTargetKey( "7", Input.VK.VK_7);
            input.AddTargetKey( "8", Input.VK.VK_8);
            input.AddTargetKey( "9", Input.VK.VK_9);

            // 設定ファイルの読み込み
            script = new Script("init.txt", _ScriptLineAnalyze);
            script.Run("Setup");


        }

        public void MainLoop()
        {
            foreach (var vr in vr_datas)
            {
                if (vr.Value.IsActiveApp())
                {
                    var key_r = input.GetKeyStatus("R");
                    var key_ctrl_l = input.GetKeyStatus("Ctrl_L");
                    var key_ctrl_r = input.GetKeyStatus("Ctrl_R");

                    var is_ctrl = (key_ctrl_l.state == Input.KeyState.Down || key_ctrl_r.state == Input.KeyState.Down);

                    // CTRL + R
                    if (is_ctrl && (key_r.state == Input.KeyState.Down_Now))
                    {
                        vr.Value.SaveVoice(output_voice_wav_dir_path);
                    }
                    else
                    {
                        // CTRL + 0~9
                        string[] key_no_s = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                        foreach ( var key_no in key_no_s)
                        {
                            var key_no_status = input.GetKeyStatus(key_no);
                            if (is_ctrl && (key_no_status.state == Input.KeyState.Down_Now))
                            {
                                vr.Value.DoVoiceEffectTemplate(key_no);
                            }
                        }
                    }

                }
            }

        }

        // 登録されている全てのアプリケーションを起動する
        public void StartAppAll()
        {
            foreach( var vr in vr_datas)
            {
                vr.Value.StartApp();
            }
        }

        private bool _ScriptLineAnalyze(Script.ScriptLineToken t)
        {
            switch (t.command[0])
            {
                case "AddVoiceRoid":
                    {
                        var vrd = new VoiceRoidData();
                        vrd.system_name = t.GetString(1);
                        vrd.output_name = t.GetString(2);
                        vrd.app_title   = t.GetString(3);
                        vrd.app_path    = t.GetString(4);
                        vrd.app_setup_talk = "";
                        vr_datas.Add(vrd.system_name, vrd);

                        script_target_last_vrd = vrd;
                    }
                    return true;
                case "$":
                    {
                        switch (t.command[1])
                        {
                            case "SetupVoice":
                                {
                                    if (script_target_last_vrd != null) {
                                        script_target_last_vrd.app_setup_talk = t.GetString(2);
                                    }
                                }
                                return true;
                            case "VoiceEffect":
                                {
                                    if (script_target_last_vrd != null)
                                    {
                                        var vet = new VoiceRoidData.VoiceEffectTemplate();
                                        var key = t.GetString(2);
                                        string[] st1 = new string[4];
                                        st1[0] = t.GetString(3);
                                        st1[1] = t.GetString(4);
                                        st1[2] = t.GetString(5);
                                        st1[3] = t.GetString(6);

                                        vet.status = st1;
                                        script_target_last_vrd.voice_effect_templates.Add(key, vet);
                                    }
                                }
                                return true;
                        }
                        return false;
                    }
            }
            return false;
        }
    }
}
