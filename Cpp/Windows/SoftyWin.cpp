#include <stdint.h>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <tchar.h>
#include <time.h>

#include "../Source/Softy.h"
#define STB_IMAGE_IMPLEMENTATION
#include "../Source/stb_image.h"

static HINSTANCE g_HInstance;
static HWND g_Wnd;

ATOM                MyRegisterClass(HINSTANCE hInstance);
BOOL                InitInstance(HINSTANCE, int);
LRESULT CALLBACK    WndProc(HWND, UINT, WPARAM, LPARAM);
INT_PTR CALLBACK    About(HWND, UINT, WPARAM, LPARAM);

static const int g_BackbufferWidth = 1920;
static const int g_BackbufferHeight = 1080;
static Color* g_Backbuffer;
static HBITMAP g_BackbufferBitmap;

static void InitBackbufferBitmap()
{
    BITMAPINFO bmi;
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = g_BackbufferWidth;
    bmi.bmiHeader.biHeight = -g_BackbufferHeight;
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;
    bmi.bmiHeader.biSizeImage = g_BackbufferWidth * g_BackbufferHeight * 4;
    HDC hdc = CreateCompatibleDC(GetDC(0));
    g_BackbufferBitmap = CreateDIBSection(hdc, &bmi, DIB_RGB_COLORS, (void**)&g_Backbuffer, NULL, 0x0);
}

static Texture* LoadTexture(const char* path)
{
    int width, height;
    unsigned char* data = stbi_load(path, &width, &height, NULL, 4);
    Texture* tex = new Texture(width, height);
    for (int i = 0; i < width*height; ++i)
    {
        tex->Data()[i].ch.r = data[i * 4 + 2];
        tex->Data()[i].ch.g = data[i * 4 + 1];
        tex->Data()[i].ch.b = data[i * 4 + 0];
        tex->Data()[i].ch.a = data[i * 4 + 3];
    }
    stbi_image_free(data);
    return tex;
}

int APIENTRY wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE, _In_ LPWSTR, _In_ int nCmdShow)
{
    Texture* texScope = LoadTexture("../Cs/Unity/Assets/Data/Scope.png");
    Texture* texView = LoadTexture("../Cs/Unity/Assets/Data/View.png");

    InitBackbufferBitmap();

    InitializeStuff(texScope, texView);

    MyRegisterClass(hInstance);
    if (!InitInstance (hInstance, nCmdShow))
    {
        return FALSE;
    }

    // Main message loop
    MSG msg;
    msg.message = WM_NULL;
    while (msg.message != WM_QUIT)
    {
        bool gotMsg = (PeekMessage(&msg, NULL, 0U, 0U, PM_REMOVE) != 0);
        if (gotMsg)
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
        else
        {
            InvalidateRect(g_Wnd, NULL, FALSE);
            UpdateWindow(g_Wnd);
        }
    }

    delete texScope;
    delete texView;

    return (int) msg.wParam;
}


ATOM MyRegisterClass(HINSTANCE hInstance)
{
    WNDCLASSEXW wcex;
    memset(&wcex, 0, sizeof(wcex));
    wcex.cbSize = sizeof(WNDCLASSEX);
    wcex.style          = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc    = WndProc;
    wcex.cbClsExtra     = 0;
    wcex.cbWndExtra     = 0;
    wcex.hInstance      = hInstance;
    wcex.hCursor        = LoadCursor(nullptr, IDC_ARROW);
    wcex.hbrBackground  = (HBRUSH)(COLOR_WINDOW+1);
    wcex.lpszClassName  = L"SoftyClass";
    return RegisterClassExW(&wcex);
}

BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
    g_HInstance = hInstance;
    HWND hWnd = CreateWindowW(L"SoftyClass", L"Softy", WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, nullptr, nullptr, hInstance, nullptr);
    if (!hWnd)
        return FALSE;
    g_Wnd = hWnd;
    ShowWindow(hWnd, nCmdShow);
    UpdateWindow(hWnd);
    return TRUE;
}

static void DrawBitmap(HDC dc, int width, int height)
{
    HDC srcDC = CreateCompatibleDC(dc);
    SetStretchBltMode(dc, COLORONCOLOR);
    SelectObject(srcDC, g_BackbufferBitmap);
    StretchBlt(dc, 0, 0, width, height, srcDC, 0, 0, g_BackbufferWidth, g_BackbufferHeight, SRCCOPY);
    DeleteObject(srcDC);
}

static uint64_t s_Time;
static int s_Count;
static char s_Buffer[200];

LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message)
    {
    case WM_PAINT:
        {
            LARGE_INTEGER time1;
            QueryPerformanceCounter(&time1);
            float t = float(clock()) / CLOCKS_PER_SEC;
            DrawStuff(t, g_BackbufferWidth, g_BackbufferHeight, g_Backbuffer);
            LARGE_INTEGER time2;
            QueryPerformanceCounter(&time2);

            PAINTSTRUCT ps;
            RECT rect;
            HDC hdc = BeginPaint(hWnd, &ps);
            GetClientRect(hWnd, &rect);
            DrawBitmap(hdc, rect.right, rect.bottom);

            uint64_t dt = time2.QuadPart - time1.QuadPart;
            ++s_Count;
            s_Time += dt;
            if (s_Count > 200)
            {
                LARGE_INTEGER frequency;
                QueryPerformanceFrequency(&frequency);

                double s = double(s_Time) / double(frequency.QuadPart) / s_Count;
                sprintf_s(s_Buffer, sizeof(s_Buffer), "ms: %.2f FPS %.1f\n", s * 1000.0f, 1.f / s);
                s_Count = 0;
                s_Time = 0;
            }
            RECT textRect;
            textRect.left = 5;
            textRect.top = 5;
            textRect.right = 500;
            textRect.bottom = 30;
            SetTextColor(hdc, 0x00FFFFFF);
            SetBkMode(hdc, TRANSPARENT);
            DrawTextA(hdc, s_Buffer, (int)strlen(s_Buffer), &textRect, DT_NOCLIP | DT_LEFT | DT_TOP);
            EndPaint(hWnd, &ps);
        }
        break;
    case WM_DESTROY:
        PostQuitMessage(0);
        break;
    default:
        return DefWindowProc(hWnd, message, wParam, lParam);
    }
    return 0;
}
