using System;
using System.Windows.Forms;
using System.Drawing;
using Medbot;
using SystemTimer = System.Timers.Timer;

namespace Medbot_UI
{
    public partial class MainFrame : Form
    {
        private readonly IBotClient _bot;
        private readonly SystemTimer _timer;

        public delegate void UpdateLabelDelegate(object sender, TimeSpan e);
        public delegate void UpdateMessageBoxDelegate(object sender, Medbot.Events.OnMessageArgs e);
        public delegate void UpdateTextBoxDelegate(string text);

        public MainFrame()
        {
            InitializeComponent();

            _bot = new BotClient();
            _bot.OnMessageReceived += Bot_OnMessageReceived;
            _bot.OnConsoleOuput += Bot_OnConsoleOuput;
            _bot.OnUptimeTick += Bot_OnUptimeTick;
            _timer = new SystemTimer();

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        private void Bot_OnConsoleOuput(object sender, Medbot.Events.OnMessageArgs e)
        {
            ConsoleAppendText(e.Message);
        }

        private void Bot_OnUptimeTick(object sender, TimeSpan e)
        {
            if (messageBox.InvokeRequired)
            {
                messageBox.BeginInvoke(new UpdateLabelDelegate(Bot_OnUptimeTick), sender, e);
            }
            else
            {
                uptimeLabel.Text = ToHumanReadableString(e);
            }
        }

        private void Bot_OnMessageReceived(object sender, Medbot.Events.OnMessageArgs e)
        {
            if (messageBox.InvokeRequired)
            {
                messageBox.BeginInvoke(new UpdateMessageBoxDelegate(Bot_OnMessageReceived), sender, e);
            }
            else
            {
                messageBox.AppendText(e.Sender.Username + ": " + e.Message + Environment.NewLine);
                messageBox.ScrollToCaret();
            }
        }

        public void ConsoleAppendText(string text)
        {
            if (textBox.InvokeRequired)
            {
                textBox.BeginInvoke(new UpdateTextBoxDelegate(ConsoleAppendText), text);
            }
            else
            {
                textBox.AppendText(Environment.NewLine + text);
                textBox.ScrollToCaret();
            }
        }

        private void MainFrame_FormClosing(object sender, FormClosingEventArgs e)
        {
            _bot?.Disconnect();
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (!_bot.IsBotRunning)
            {
                _bot.Start();
                startStopButton.Text = "Stop";
                botInfoStatus.Text = "Bot is LIVE";
                colorPanel.BackColor = Color.Green;
                _timer.Start();

                infoLabelChannel.Text = _bot.DeployedChannel;
            }
            else
            {
                startStopButton.Text = "Start";
                botInfoStatus.Text = "Bot is IDLE";
                colorPanel.BackColor = Color.Red;

                infoLabelChannel.Text = "--NONE--";
                uptimeLabel.Text = "---";
                _bot.Stop();
            }
        }

        public string ToHumanReadableString(TimeSpan t)
        {
            if (t.TotalSeconds <= 1)
            {
                return $@"{t:s\.ff} seconds";
            }
            if (t.TotalMinutes <= 1)
            {
                return $@"{t:%s} seconds";
            }
            if (t.TotalHours <= 1)
            {
                return $@"{t:%m} minutes";
            }
            if (t.TotalDays <= 1)
            {
                return $@"{t:%h} hours";
            }

            return $@"{t:%d} days";
        }
    }
}

//public void TestJson() {
//    //var jsonX = @"{'created_at':'2017-07-02T14:58:38Z','_links':{'self':'https://api.twitch.tv/kraken/users/zendy444/follows/channels/bukk94'},'notifications':false,'user':{'display_name':'Zendy444','_id':66308387,'name':'zendy444','type':'user','bio':null,'created_at':'2014-07-14T09:30:30Z','updated_at':'2017-07-06T19:01:28Z','logo':null,'_links':{'self':'https://api.twitch.tv/kraken/users/zendy444'}}}";
//    //dynamic stuff = JObject.Parse(jsonX);
//    //var x = stuff.user;
//    var ux = Followers.FollowersClass.GetFollowers("bukk94", 3);
//}