#include "Softy.h"
#include <vector>
#include <math.h>
#include <time.h>
#include <algorithm>

template<typename T> T clamp(T x, T min, T max)
{
    return x < min ? min : x > max ? max : x;
}

template<typename T> T lerp(T v1, T v2, float ratio)
{
    return v1 * ratio + v2 * (1.f - ratio);
}

struct Vector2
{
    float x, y;

    Vector2() : x(0), y(0) {}
    Vector2(float x_, float y_) : x(x_), y(y_) {}
};
inline Vector2 operator*(Vector2 a, float b) { return Vector2(a.x*b, a.y*b); }
inline Vector2 operator*(Vector2 a, Vector2 b) { return Vector2(a.x*b.x, a.y*b.y); }
inline Vector2 operator+(Vector2 a, Vector2 b) { return Vector2(a.x + b.x, a.y + b.y); }
inline Vector2 operator-(Vector2 a, Vector2 b) { return Vector2(a.x - b.x, a.y - b.y); }
inline Vector2 clamp(Vector2 a, float min, float max) { return Vector2(clamp(a.x, min, max), clamp(a.y, min, max)); }

struct RenderObject;

typedef Color PixelProgram(Vector2 screenUV, Vector2 objUV, RenderObject* obj);
typedef void UpdateFunc(RenderObject* obj);

struct RenderObject
{
    Vector2 position;
    Vector2 size;
    PixelProgram* shader;
    UpdateFunc* tick;
};

typedef std::vector<RenderObject> RenderScene;
RenderScene g_Scene;

bool g_Checkerboard = true;
int g_RenderOdd = 0;
float g_Time;
int g_ScreenWidth;
int g_ScreenHeight;

void DrawObject(int screenWidth, int screenHeight, Color* backbuffer, RenderObject* obj)
{
    int startX = clamp(int(obj->position.x * screenWidth), 0, screenWidth - 1);
    int endX = clamp(int((obj->position.x + obj->size.x) * screenWidth), 0, screenWidth - 1);
    int startY = clamp(int(obj->position.y * screenHeight), 0, screenHeight - 1);
    int endY = clamp(int((obj->position.y + obj->size.y) * screenHeight), 0, screenHeight - 1);

    float invWidth = 1.f / screenWidth;
    float invHeight = 1.f / screenHeight;
    Vector2 objInvSize = Vector2(1.f / obj->size.x, 1.f / obj->size.y);
    for (auto y = startY; y < endY; ++y)
    {
        for (auto x = startX; x < endX; ++x)
        {
            if ((g_Checkerboard && (((x + y) & 1) == g_RenderOdd)) || !g_Checkerboard)
            {
                Vector2 screenUV = Vector2(x * invWidth, y * invHeight);
                Vector2 objUV = (screenUV - obj->position) * objInvSize;
                objUV = clamp(objUV, 0.f, 1.f);

                Color result = obj->shader(screenUV, objUV, obj);
                if (result.a > 0)
                {
                    backbuffer[y * screenWidth + x] = result;
                }
            }
        }
    }
}

void DrawStuff(float time, int screenWidth, int screenHeight, Color* backbuffer)
{
    g_Time = time * 1000.0f;
    g_ScreenWidth = screenWidth;
    g_ScreenHeight = screenHeight;

    for (auto& o : g_Scene)
        o.tick(&o);

    for (auto& o : g_Scene)
        DrawObject(screenWidth, screenHeight, backbuffer, &o);

    g_RenderOdd = 1 - g_RenderOdd;
}

static Color SampleTexture(const Texture* texture, Vector2 objUV)
{
    int coordX = (int)lroundf((texture->Width() - 1) * objUV.x);
    int coordY = (int)lroundf((texture->Height() - 1) * objUV.y);
    int index = coordY * texture->Width() + coordX;
    return texture->Data()[index];
}


static void UpdateScope(RenderObject* obj)
{
    obj->size.x = 0.8f * float(g_ScreenHeight) / float(g_ScreenWidth);
    obj->size.y = 0.8f;
    obj->position.x = 0.5f - obj->size.x / 2 + cosf(g_Time / 1000.0f) * 0.05f;
    obj->position.y = 0.5f - obj->size.y / 2 + cosf(g_Time / 600.0f) * 0.05f;
}

static Texture* g_TextureScope;

static Color PixelProgramScope(Vector2 screenUV, Vector2 objUV, RenderObject* obj)
{
    return SampleTexture(g_TextureScope, objUV);
    //return Color(screenUV.x*255, screenUV.y*255, 0, 255);
}

static Texture* g_TextureView;
static float g_ViewLastUpdateTo;
static float g_ViewUpdateToLength;
static Vector2 g_ViewToPosition;

static void UpdateView(RenderObject* obj)
{
    if (g_Time > g_ViewLastUpdateTo + g_ViewUpdateToLength)
    {
        g_ViewToPosition.x = float(rand()) / float(RAND_MAX) - 1;
        g_ViewToPosition.y = float(rand()) / float(RAND_MAX) - 1;
        g_ViewUpdateToLength = float(rand()) / float(RAND_MAX) * (1500 - 400) + 400;
        g_ViewLastUpdateTo = g_Time;
    }

    obj->size.x = obj->size.y = 2;
    //float ratio = (g_Time - g_ViewLastUpdateTo) / 10000;
    obj->position.x = lerp(obj->position.x, g_ViewToPosition.x, 0.99f);
    obj->position.y = lerp(obj->position.y, g_ViewToPosition.y, 0.99f);
}

static uint8_t Dither(int strength, float salt)
{
    return (uint8_t)((int(3.1415 * salt * clock()) % (strength * 2 + 1)) - strength);
}

static Color PixelProgramView(Vector2 suv, Vector2 ouv, RenderObject* obj)
{
    Color result = Color(0, 0, 0, 255);

    float darkX = fabs(suv.x - 0.5f + cosf(g_Time / 1000.0f) * 0.1f);
    float darkY = fabs(suv.y - 0.5f + cosf(g_Time / 600.0f) * 0.1f);
    float dark = clamp(1.f - 4.f * (darkX * darkX + darkY * darkY), 0.f, 1.f);
    if (dark == 0)
    {
        return result;
    }

    result = SampleTexture(g_TextureView, ouv);

    result.b = (uint8_t)(result.b * dark);
    result.g = (uint8_t)(result.g * dark);
    result.r = (uint8_t)(result.r * dark);

    const int dither = 32;
    int ditherB = std::min(dither, std::min<int>(result.b, 255 - result.b));
    int ditherG = std::min(dither, std::min<int>(result.g, 255 - result.g));
    int ditherR = std::min(dither, std::min<int>(result.r, 255 - result.r));

    result.b += Dither(ditherB, suv.x);
    result.g += Dither(ditherG, suv.x);
    result.r += Dither(ditherR, suv.x);

    return result;

}


void InitializeStuff(Texture* texScope, Texture* texView)
{
    g_TextureView = texView;
    RenderObject view = RenderObject{ Vector2(), Vector2(), &PixelProgramView, &UpdateView };
    g_Scene.push_back(view);

    g_TextureScope = texScope;
    RenderObject scope = RenderObject{ Vector2(), Vector2(), &PixelProgramScope, &UpdateScope };
    g_Scene.push_back(scope);
}

void ShutdownStuff()
{
}

