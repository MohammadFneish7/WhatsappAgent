using WhatsappAgent;
namespace WhatsappAgentUI
{
    public partial class Form1 : Form
    {
        Messegner? Messegner = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Messegner_OnQRReady(Image qrbmp)
        {
            pictureBox1.Image = qrbmp;
            textBox1.Invoke(() => textBox1.AppendLine("please scan the QR code using your Whatsapp mobile app to continue login."));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.AppendLine("sending text message...");
                Messegner?.SendMessage(txtmobile.Text, textmsg.Text);
                textBox1.AppendLine("text message sent.");
            }
            catch (Exception ex)
            {
                textBox1.AppendLine(ex.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Title = "Select Image File",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Filter = "Images (*.BMP;*.JPG,*.PNG)|*.BMP;*.JPG;*.PNG;",
                    Multiselect = false
                };

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    textBox1.AppendLine("sending image...");
                    Messegner?.SendMedia(MediaType.IMAGE_OR_VIDEO, txtmobile.Text, openFileDialog1.FileName, textmsg.Text);
                    textBox1.AppendLine("image sent.");
                }
            }
            catch (Exception ex)
            {
                textBox1.AppendLine(ex.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Title = "Select Attachment",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false
                };

                if (openFileDialog1.ShowDialog() == DialogResult.OK) { 
                    textBox1.AppendLine("sending attachment...");
                    Messegner?.SendMedia(MediaType.ATTACHMENT, txtmobile.Text, openFileDialog1.FileName, textmsg.Text);
                    textBox1.AppendLine("attachment sent.");
                }
            }
            catch (Exception ex)
            {
                textBox1.AppendLine(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.AppendLine("logging out...");
                Messegner?.Logout();
                textBox1.AppendLine("logged out.");
            }
            catch (Exception ex)
            {
                textBox1.AppendLine(ex.Message);
            }
            finally
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Messegner?.Dispose();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
           Messegner?.Login();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            textBox1.AppendLine("logging in...");
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if(e.Error != null)
            {
                textBox1.AppendLine(e.Error.ToString());
            }
            else
            {
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                checkBox1.Enabled = false;
                button6.Enabled = false;
                textBox1.AppendLine("starting driver...");
                Messegner = new Messegner(checkBox1.Checked);
                Messegner.OnQRReady += Messegner_OnQRReady;
                textBox1.AppendLine("driver started.");
                button5.Enabled = true;
            }
            catch (Exception ex)
            {
                textBox1.AppendLine(ex.Message);
            }
        }
    }
}