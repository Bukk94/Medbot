using System;
using System.Windows.Forms;
using System.Threading;

namespace Medbot {
    public partial class MainFrame : Form {
        IBotClient bot;

        /// <summary>
        /// _Test interface for API
        /// </summary>
        public MainFrame() {
            InitializeComponent();

            bot = new BotClient(this);
            Thread thread = new Thread(new ThreadStart(bot.Start));
            thread.Start();

            bot.OnMessageReceived += Bot_OnMessageReceived;
        }

        public delegate void UpdateMessageBoxDelegate(object sender, Events.OnMessageArgs e);
        private void Bot_OnMessageReceived(object sender, Events.OnMessageArgs e) {
            if (messageBox.InvokeRequired) {
                messageBox.BeginInvoke(new UpdateMessageBoxDelegate(Bot_OnMessageReceived), sender, e);
            } else {
                messageBox.AppendText(e.Sender.Username + ": " + e.Message + Environment.NewLine);
                messageBox.ScrollToCaret();
            }
        }


        public delegate void UpdateTextBoxDelegate(string text);
        public void ConsoleAppendText(string text) {
            if (textBox.InvokeRequired) {
                textBox.BeginInvoke(new UpdateTextBoxDelegate(ConsoleAppendText), text);
            } else {
                textBox.AppendText(Environment.NewLine + text);
                textBox.ScrollToCaret();
            }
        }

        private void MainFrame_FormClosing(object sender, FormClosingEventArgs e) {
            bot.Disconnect();
        }
    }
}

//public void TestJson() {
//    //var jsonX = @"{'created_at':'2017-07-02T14:58:38Z','_links':{'self':'https://api.twitch.tv/kraken/users/zendy444/follows/channels/bukk94'},'notifications':false,'user':{'display_name':'Zendy444','_id':66308387,'name':'zendy444','type':'user','bio':null,'created_at':'2014-07-14T09:30:30Z','updated_at':'2017-07-06T19:01:28Z','logo':null,'_links':{'self':'https://api.twitch.tv/kraken/users/zendy444'}}}";
//    //dynamic stuff = JObject.Parse(jsonX);
//    //var x = stuff.user;
//    var ux = Followers.FollowersClass.GetFollowers("bukk94", 3);
//}