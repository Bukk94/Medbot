using System;
using System.Windows.Forms;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace Medbot {
    public partial class MainFrame : Form {

        /// <summary>
        /// _Test interface for API
        /// </summary>
        public MainFrame() {
            InitializeComponent();
            //TestJson();

            IBotClient bot = new BotClient(this);
            Thread thread = new Thread(new ThreadStart(bot.Start));
            thread.Start();
        }

        public void TestJson() {
            //var jsonX = @"{'created_at':'2017-07-02T14:58:38Z','_links':{'self':'https://api.twitch.tv/kraken/users/zendy444/follows/channels/bukk94'},'notifications':false,'user':{'display_name':'Zendy444','_id':66308387,'name':'zendy444','type':'user','bio':null,'created_at':'2014-07-14T09:30:30Z','updated_at':'2017-07-06T19:01:28Z','logo':null,'_links':{'self':'https://api.twitch.tv/kraken/users/zendy444'}}}";
            //dynamic stuff = JObject.Parse(jsonX);
            //var x = stuff.user;
            
            var ux = Followers.FollowersClass.GetFollowers("bukk94", 3);
        }

        public delegate void UpdateTextBoxDelegate(string text);
        public void ConsoleAppendText(string text) {
            if (textBox.InvokeRequired) {
                textBox.BeginInvoke(new UpdateTextBoxDelegate(ConsoleAppendText), new Object[] { text });
            } else {
                textBox.AppendText(Environment.NewLine + text);
                textBox.ScrollToCaret();
            }
        }
    }
}
