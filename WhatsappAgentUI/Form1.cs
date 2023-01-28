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

        private void Form1_Load(object sender, EventArgs e)
        {
            Messegner = new Messegner();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.AppendLine("sending text message...");
                Messegner?.SendMessage("70434962", "Hello dear");
                textBox1.AppendLine("text message sent.");
            }
            catch (Exception ex)
            {
                textBox1.Text = ex.Message;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.AppendLine("sending image...");
                Messegner?.SendMedia(MediaType.IMAGE_OR_VIDEO, "70434962", "C:\\Users\\96170\\Desktop\\WhatsApp Image 2022-11-28 at 19.20.48.jpg", "this is an image");
                textBox1.AppendLine("image sent.");
            }
            catch (Exception ex)
            {
                textBox1.Text = ex.Message;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.AppendLine("sending attachment...");
                Messegner?.SendMedia(MediaType.ATTACHMENT, "70434962", "C:\\Users\\96170\\Desktop\\WhatsApp Image 2022-11-28 at 19.20.48.jpg", "");
                textBox1.AppendLine("attachment sent.");
            }
            catch (Exception ex)
            {
                textBox1.Text = ex.Message;
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
                textBox1.Text = ex.Message;
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
    }
}