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
        m_UIImage.texture = m_BackbufferTex;
        m_ViewTex = LoadTexture(m_ViewSource);
        m_ScopeTex = LoadTexture(m_ScopeSource);
        m_View = new PerformanceTest.View(m_Device, m_ViewTex);
        m_Scope = new PerformanceTest.Scope(m_Device, m_ScopeTex);
    }

    static Softy.Texture LoadTexture(Texture2D source)
    {
        Color32[] pixels = source.GetPixels32(0);
        byte[] bytes = new byte[source.width * source.height * 4];
        for (var i = 0; i < pixels.Length; ++i)
        {
            bytes[i * 4 + 0] = pixels[i].r;
            bytes[i * 4 + 1] = pixels[i].g;
            bytes[i * 4 + 2] = pixels[i].b;
            bytes[i * 4 + 3] = pixels[i].a;
        }
        return new Softy.Texture(bytes, source.width * 4);
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
        if (m_UpdateCounter == 10)
        {
            var s = (float)((double)m_Stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) / m_UpdateCounter;
            m_UIPerfText.text = string.Format("ms: {0:F2}, FPS: {1:F1}", s * 1000.0f, 1.0f / s);
            m_UpdateCounter = 0;
            m_Stopwatch.Reset();
        }
        m_BackbufferTex.LoadRawTextureData(m_Device.BackBuffer);
        m_BackbufferTex.Apply();
    }
}
