//Hiroya Herdinanto
//5223600022
using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace CLientMO
{
    public partial class Form1 : Form
    {
        TcpClient client;
        Thread receiveThread;
        bool running = true;

        public Form1()
        {
            InitializeComponent();

            // Tambahkan PictureBox manual
            pictureBox1 = new PictureBox();
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Controls.Add(pictureBox1);

            ConnectToServer();
        }

        // deklarasi di bawah class
        private PictureBox pictureBox1;

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 5000); // connect ke server lokal
                receiveThread = new Thread(ReceiveFrames);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal connect ke server: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReceiveFrames()
        {
            try
            {
                NetworkStream stream = client.GetStream();
                BinaryReader reader = new BinaryReader(stream);

                while (running)
                {
                    // Baca panjang data (int32 = 4 byte)
                    int length = reader.ReadInt32();

                    // Pastikan panjang valid
                    if (length <= 0) continue;

                    // Baca data frame
                    byte[] data = reader.ReadBytes(length);

                    // Convert byte[] ke Image
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        Image frame = Image.FromStream(ms);

                        // Update UI di thread utama
                        if (pictureBox1.InvokeRequired)
                        {
                            pictureBox1.Invoke((MethodInvoker)delegate
                            {
                                pictureBox1.Image?.Dispose(); // buang gambar lama biar ga leak
                                pictureBox1.Image = new Bitmap(frame);
                            });
                        }
                        else
                        {
                            pictureBox1.Image?.Dispose();
                            pictureBox1.Image = new Bitmap(frame);
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Server disconnect
                if (running)
                {
                    MessageBox.Show("Koneksi ke server terputus.",
                                    "Disconnected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saat menerima frame: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;
            try
            {
                client?.Close();
            }
            catch { }
            base.OnFormClosing(e);
        }
    }
}
