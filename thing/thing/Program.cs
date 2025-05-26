using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

public class WavyScreen : Form
{
    public WavyScreen()
    {
        base.FormBorderStyle = FormBorderStyle.None;
        base.WindowState = FormWindowState.Maximized;
        base.TopMost = true;
        this.DoubleBuffered = true;
        
        // Set timer interval to 10ms (0.01 seconds)
        this.timer = new Timer();
        this.timer.Interval = 10;
        this.timer.Tick += Timer_Tick;
        this.timer.Start();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        this.phase += 0.2;
        base.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        
        // Capture screen once
        if (screenBitmap == null || screenBitmap.Width != base.Width || screenBitmap.Height != base.Height)
        {
            screenBitmap?.Dispose();
            screenBitmap = new Bitmap(base.Width, base.Height);
        }
        
        using (var g = Graphics.FromImage(screenBitmap))
        {
            g.CopyFromScreen(0, 0, 0, 0, screenBitmap.Size);
        }

        BitmapData sourceData = screenBitmap.LockBits(
            new Rectangle(0, 0, screenBitmap.Width, screenBitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        
        BitmapData destData = null;
        try
        {
            if (wavyBitmap == null || wavyBitmap.Width != base.Width || wavyBitmap.Height != base.Height)
            {
                wavyBitmap?.Dispose();
                wavyBitmap = new Bitmap(base.Width, base.Height);
            }
            
            destData = wavyBitmap.LockBits(
                new Rectangle(0, 0, wavyBitmap.Width, wavyBitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int bytesPerPixel = 4;
            int heightInPixels = sourceData.Height;
            int widthInBytes = sourceData.Width * bytesPerPixel;

            byte[] sourcePixels = new byte[sourceData.Stride * sourceData.Height];
            Marshal.Copy(sourceData.Scan0, sourcePixels, 0, sourcePixels.Length);
            
            byte[] destPixels = new byte[destData.Stride * destData.Height];

            Parallel.For(0, heightInPixels, y =>
            {
                int currentLine = y * sourceData.Stride;
                
                for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                {
                    int pixelX = x / bytesPerPixel;
                    int pixelY = y;
                    
                    // Vertical wave (moving)
                    int waveOffsetY = (int)(amplitude * Math.Cos(frequency * pixelX + phase));
                    int sourceY = pixelY + waveOffsetY;
                    
                    // Horizontal wave (static)
                    int waveOffsetX = (int)(amplitude * 0.7 * Math.Sin(frequency * pixelY * 0.8));
                    int sourceX = pixelX + waveOffsetX;
                    
                    // Clamp coordinates to edges if out of bounds
                    sourceX = Math.Clamp(sourceX, 0, sourceData.Width - 1);
                    sourceY = Math.Clamp(sourceY, 0, sourceData.Height - 1);

                    int sourcePos = sourceY * sourceData.Stride + (sourceX * bytesPerPixel);
                    int destPos = currentLine + x;
                    
                    // Copy BGRA (32bppArgb format)
                    destPixels[destPos] = sourcePixels[sourcePos];     // B
                    destPixels[destPos + 1] = sourcePixels[sourcePos + 1]; // G
                    destPixels[destPos + 2] = sourcePixels[sourcePos + 2]; // R
                    destPixels[destPos + 3] = sourcePixels[sourcePos + 3]; // A
                }
            });

            Marshal.Copy(destPixels, 0, destData.Scan0, destPixels.Length);
        }
        finally
        {
            screenBitmap.UnlockBits(sourceData);
            if (destData != null)
                wavyBitmap.UnlockBits(destData);
        }

        e.Graphics.DrawImage(wavyBitmap, 0, 0);
    }

    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new WavyScreen());
    }

    private Timer timer;
    private Bitmap screenBitmap;
    private Bitmap wavyBitmap;
    private int amplitude = 20;
    private double frequency = 0.05;
    private double phase = 0.0;
}