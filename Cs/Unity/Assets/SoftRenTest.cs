using System.Diagnostics;
using UnityEngine;

public class SoftRenTest : MonoBehaviour
{
    public Texture2D m_ViewSource;
    public Texture2D m_ScopeSource;
    public TextAsset m_Settings;
    public UnityEngine.UI.Text m_UIPerfText;
    public UnityEngine.UI.RawImage m_UIImage;
    
    Texture2D m_BackbufferTex;
    byte[] m_BackbufferBytes;
    Softy.Device m_Device;
    Softy.Texture m_ViewTex;
    Softy.Texture m_ScopeTex;

    PerformanceTest.View m_View;
    PerformanceTest.Scope m_Scope;
    Stopwatch m_Stopwatch = new Stopwatch();
    int m_UpdateCounter;

    void Start ()
    {
        int width = 640, height = 480, threadCount = 64;
        bool checkerboard = true;
        Softy.Device.ReadSettings(m_Settings.text, ref width, ref height, ref threadCount, ref checkerboard);
        m_Device = new Softy.Device(width, height, threadCount)
        {
            Checkerboard = checkerboard
        };
        m_BackbufferTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        m_BackbufferBytes = new byte[width * height * 4];
        m_UIImage.texture = m_BackbufferTex;
        m_ViewTex = LoadTexture(m_ViewSource);
        m_ScopeTex = LoadTexture(m_ScopeSource);
        m_View = new PerformanceTest.View(m_Device, m_ViewTex);
        m_Scope = new PerformanceTest.Scope(m_Device, m_ScopeTex);
    }

    static Softy.Texture LoadTexture(Texture2D source)
    {
        Color32[] pixels = source.GetPixels32(0);
        Softy.Color[] mypixels = new Softy.Color[source.width * source.height];
        for (var i = 0; i < pixels.Length; ++i)
        {
            mypixels[i] = new Softy.Color(pixels[i].r, pixels[i].g, pixels[i].b, pixels[i].a);
        }
        return new Softy.Texture(mypixels, source.width);
    }

    void UpdateLoop()
    {
        m_Stopwatch.Start();
        m_View.Update();
        m_Scope.Update();

        m_View.Draw();
        m_Scope.Draw();

        m_Device.Render();
        m_Stopwatch.Stop();
        ++m_UpdateCounter;
    }

    void Update ()
    {
        UpdateLoop();
        if (m_UpdateCounter == 30)
        {
            var s = (float)((double)m_Stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) / m_UpdateCounter;
            m_UIPerfText.text = string.Format("ms: {0:F2}, FPS: {1:F1}", s * 1000.0f, 1.0f / s);
            m_UpdateCounter = 0;
            m_Stopwatch.Reset();
        }
        for (int i = 0; i < m_Device.BackBuffer.Length; ++i)
        {
            var c = m_Device.BackBuffer[i];
            m_BackbufferBytes[i * 4 + 0] = c.R;
            m_BackbufferBytes[i * 4 + 1] = c.G;
            m_BackbufferBytes[i * 4 + 2] = c.B;
            m_BackbufferBytes[i * 4 + 3] = c.A;
        }

        m_BackbufferTex.LoadRawTextureData(m_BackbufferBytes);
        m_BackbufferTex.Apply();
    }
}
