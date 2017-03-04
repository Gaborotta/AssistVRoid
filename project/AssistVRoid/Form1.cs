using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MyLauncher;
using System.Threading;

namespace AssistVRoid
{
    public partial class Form1 : Form
    {
        private Input input = new Input();
        private AssistVRoidCore assist_vroid_core;

        string config_file = @"config.txt";
        List<Common.ConfigConectUI> config_conect_ui = new List<Common.ConfigConectUI>(); // 設定とUIの接続と保存・読み込みの汎用化

        public Form1()
        {
            assist_vroid_core = new AssistVRoidCore(input);
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Common.SettingTextBoxDragEvent(textBox1, true);

            // コンフィグファイルとUIの関連付けと読み込み
            config_conect_ui.Add(new Common.ConfigConectUI("Main.保存先のディレクトリ", textBox1));
            Common.LoadConfig(config_file, config_conect_ui);

        }

        public void MainLoop()
        {
            input.Update();
            assist_vroid_core.MainLoop();
            Thread.Sleep(33); // WaitSleep.Doを使うと、処理が離れるのでバグる
        }

        private void button1_Click(object sender, EventArgs e)
        {
            assist_vroid_core.StartAppAll();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            assist_vroid_core.output_voice_wav_dir_path = ((TextBox)sender).Text;
            Common.SaveConfig(config_file, config_conect_ui);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            assist_vroid_core.ReSetup();
        }
    }
}
