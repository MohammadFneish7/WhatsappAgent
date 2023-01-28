using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsappAgentUI
{
    internal static class Extensions
    {
        public static void AppendLine(this TextBox textBox, string line)
        {
            textBox.Text = (textBox.Text + Environment.NewLine + line.Replace("\n", Environment.NewLine)).Trim();
            if (textBox.Visible)
            {
                textBox.SelectionStart = textBox.TextLength;
                textBox.ScrollToCaret();
            }
        }
    }
}
