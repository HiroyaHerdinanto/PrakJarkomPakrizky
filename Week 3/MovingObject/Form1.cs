//Hiroya Herdinanto
//5223600022
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace MovingObject
{
    public partial class Form1 : Form
    {
        // MovingObject variables
        Pen red = new Pen(Color.Red);
        Rectangle rect = new Rectangle(20, 20, 30, 30);
        SolidBrush fillBlue = new SolidBrush(Color.Blue);
        int slide = 10;

        // Socket server
        TcpListener server;
        List<TcpClient> clients = new List<TcpClient>();
        bool running = true;

        public Form1()
        {
            InitializeComponent();

            // Timer setup
            timer1.Interval = 50;
            timer1.Enabled = true;

            // Start server
            StartServer();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            back();
            rect.X += slide;
            Invalidate();

            // Push frame ke client
            PushFrame();
        }

        private void back()
        {
            if (rect.X >= this.Width - rect.Width * 2)
                slide = -10;
            else if (rect.X <= rect.Width / 2)
                slide = 10;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            g.DrawRectangle(red, rect);
            g.FillRectangle(fillBlue, rect);
        }

        private void StartServer()
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Server started on port 5000");

            Thread acceptThread = new Thread(() =>
            {
                while (running)
                {
                    TcpClient client = server.AcceptTcpClient();
                    lock (clients)
                    {
                        clients.Add(client);
                    }
                    Console.WriteLine("Client connected!");
                }
            });
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        private void PushFrame()
        {
            Bitmap bmp = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawRectangle(red, rect);
                g.FillRectangle(fillBlue, rect);
            }

            byte[] imgData;
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                imgData = ms.ToArray();
            }

            lock (clients)
            {
                foreach (var client in clients.ToArray())
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        BinaryWriter writer = new BinaryWriter(stream);
                        writer.Write(imgData.Length);
                        writer.Write(imgData);
                        writer.Flush();
                    }
                    catch
                    {
                        clients.Remove(client); // remove disconnected clients
                    }
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;
            server.Stop();
            base.OnFormClosing(e);
        }
    }
}
