using System;
using System.Windows.Forms;
using System.Threading;

namespace Medbot {
    public partial class MainFrame : Form {
        
        /// <summary>
        /// Test interface for API
        /// </summary>
        public MainFrame() {
            InitializeComponent();

            IBotClient bot = new BotClient(this);
            Thread thread = new Thread(new ThreadStart(bot.Start));
            thread.Start();
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
