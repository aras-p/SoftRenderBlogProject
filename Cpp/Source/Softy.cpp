#include "Softy.h"
#include <vector>
#include <math.h>
#include <time.h>
#include <algorithm>
#include "enkiTS/TaskScheduler_c.h"

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

typedef void PixelProgramSpan(Vector2 screenUV, Vector2 objUV, int cols, float screenUVdx, float objUVdx, Color* backbuffer);
typedef void UpdateFunc(RenderObject* obj);

struct RenderObject
{
    Vector2 position;
    Vector2 size;
    PixelProgramSpan* shader;
    UpdateFunc* tick;
};

typedef std::vector<RenderObject> RenderScene;
static RenderScene g_Scene;

static bool g_Checkerboard = true;
static int g_RenderOdd = 0;
static float g_Time;
static uint32_t g_TimeInt;
static float g_CosTime1000;
static float g_CosTime600;
static int g_ScreenWidth;
static int g_ScreenHeight;

static enkiTaskScheduler* g_TS;


static void DrawObject(int screenWidth, int screenHeight, int rowStartY, int rowEndY, Color* backbuffer, RenderObject* obj)
{
    int startX = clamp(int(obj->position.x * screenWidth), 0, screenWidth - 1);
    int endX = clamp(int((obj->position.x + obj->size.x) * screenWidth), 0, screenWidth - 1);
    int startY = clamp(int(obj->position.y * screenHeight), rowStartY, rowEndY);
    int endY = clamp(int((obj->position.y + obj->size.y) * screenHeight), rowStartY, rowEndY);

    float invWidth = 1.f / screenWidth;
    float invHeight = 1.f / screenHeight;
    Vector2 objPos = obj->position;
    Vector2 objInvSize = Vector2(1.f / obj->size.x, 1.f / obj->size.y);
    Vector2 objPosTimesInvSize = objPos * objInvSize;
    for (auto y = startY; y < endY; ++y)
    {
        if (g_Checkerboard && ((y & 1) != g_RenderOdd))
            continue;

        int yOffset = y * screenWidth;
        Vector2 screenUV = Vector2(startX * invWidth, y * invHeight);
        Vector2 objUV = screenUV * objInvSize - objPosTimesInvSize;
        objUV.y = clamp(objUV.y, 0.f, 1.f);
        obj->shader(screenUV, objUV, endX - startX, invWidth, invWidth*objInvSize.x, backbuffer + yOffset + startX);
    }
}

struct DrawObjectArgs
{
    int screenWidth, screenHeight;
    Color* backbuffer;
    RenderObject* obj;
};

static void DrawObjectJob(uint32_t start, uint32_t end, uint32_t threadnum, void* args_)
{
    DrawObjectArgs* args = (DrawObjectArgs*)args_;
    DrawObject(args->screenWidth, args->screenHeight, start, end, args->backbuffer, args->obj);
}


void DrawStuff(float time, int screenWidth, int screenHeight, Color* backbuffer)
{
    g_Time = time * 1000.0f;
    g_TimeInt = ((uint32_t)g_Time) * 17;
    g_CosTime1000 = cosf(g_Time / 1000.0f);
    g_CosTime600 = cosf(g_Time / 600.0f);

    g_ScreenWidth = screenWidth;
    g_ScreenHeight = screenHeight;

    for (auto& o : g_Scene)
        o.tick(&o);

    for (auto& o : g_Scene)
    {
        DrawObjectArgs args;
        args.screenWidth = screenWidth;
        args.screenHeight = screenHeight;
        args.backbuffer = backbuffer;
        args.obj = &o;
        enkiTaskSet * task = enkiCreateTaskSet(g_TS, DrawObjectJob);
        enkiAddTaskSetToPipeMinRange(g_TS, task, &args, screenHeight, 32);
        enkiWaitForTaskSet(g_TS, task);
        enkiDeleteTaskSet(task);
    }

    g_RenderOdd = 1 - g_RenderOdd;
}

static const Color* SampleTextureY(const Texture* texture, Vector2 objUV)
{
    int coordY = (int)(texture->Height1() * objUV.y);
    int index = coordY * texture->Width();
    return texture->Data() + index;
}

static Color SampleTextureX(const Texture* texture, const Color* textureY, Vector2 objUV)
{
    int coordX = (int)((texture->Width() - 1) * objUV.x);
    return textureY[coordX];
}


static void UpdateScope(RenderObject* obj)
{
    obj->size.x = 0.8f * float(g_ScreenHeight) / float(g_ScreenWidth);
    obj->size.y = 0.8f;
    obj->position.x = 0.5f - obj->size.x / 2 + g_CosTime1000 * 0.05f;
    obj->position.y = 0.5f - obj->size.y / 2 + g_CosTime600 * 0.05f;
}

static Texture* g_TextureScope;

static void PixelProgramScope(
    Vector2 screenUV, Vector2 objUV,
    int cols, float screenUVdx, float objUVdx, Color* backbuffer)
{
    const Color* texY = SampleTextureY(g_TextureScope, objUV);
    for (int x = 0; x < cols; ++x, screenUV.x += screenUVdx, objUV.x += objUVdx, backbuffer++)
    {
        objUV.x = clamp(objUV.x, 0.f, 1.f);
        Color result = SampleTextureX(g_TextureScope, texY, objUV);
        if (result.ch.a > 0)
            *backbuffer = result;
    }
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

static uint32_t IntHash(uint32_t a)
{
    a = (a + 0x7ed55d16) + (a << 12);
    a = (a ^ 0xc761c23c) ^ (a >> 19);
    a = (a + 0x165667b1) + (a << 5);
    a = (a + 0xd3a2646c) ^ (a << 9);
    a = (a + 0xfd7046c5) + (a << 3);
    a = (a ^ 0xb55a4f09) ^ (a >> 16);
    return a;
}

static Color Dither(Color col, uint32_t offset)
{
    uint32_t hash = IntHash(offset + g_TimeInt);
    uint32_t dither = 32;
    uint8_t v = (uint8_t)(hash & (dither - 1));
    if (col.ch.r < 255 - dither) col.ch.r += v;
    if (col.ch.g < 255 - dither) col.ch.g += v;
    if (col.ch.b < 255 - dither) col.ch.b += v;
    return col;
}

static void PixelProgramView(
    Vector2 screenUV, Vector2 objUV,
    int cols, float screenUVdx, float objUVdx, Color* backbuffer)
{
    const Color* textureY = SampleTextureY(g_TextureView, objUV);
    float darkXfac = -0.5f + g_CosTime1000 * 0.1f;
    float darkY = fabs(screenUV.y - 0.5f + g_CosTime600 * 0.1f);
    float darkY2 = darkY * darkY;
    float darkFac = 1.0f - 4.0f * darkY2;

    for (int x = 0; x < cols; ++x, screenUV.x += screenUVdx, objUV.x += objUVdx, backbuffer++)
    {
        objUV.x = clamp(objUV.x, 0.f, 1.f);
        Color result = Color(0, 0, 0, 255);

        float darkX = fabs(screenUV.x + darkXfac );
        float dark = darkFac - 4.f * darkX * darkX;
        if (dark > 0.0f)
        {
            result = SampleTextureX(g_TextureView, textureY, objUV);

            result.ch.b = (uint8_t)(result.ch.b * dark);
            result.ch.g = (uint8_t)(result.ch.g * dark);
            result.ch.r = (uint8_t)(result.ch.r * dark);

            result = Dither(result, (uint32_t)(size_t)backbuffer);
        }
        if (result.ch.a > 0)
        {
            *backbuffer = result;
        }
    }
}


void InitializeStuff(Texture* texScope, Texture* texView)
{
    g_TS = enkiNewTaskScheduler();
    enkiInitTaskScheduler(g_TS);

    g_TextureView = texView;
    RenderObject view = RenderObject{ Vector2(), Vector2(), &PixelProgramView, &UpdateView };
    g_Scene.push_back(view);

    g_TextureScope = texScope;
    RenderObject scope = RenderObject{ Vector2(), Vector2(), &PixelProgramScope, &UpdateScope };
    g_Scene.push_back(scope);
}

void ShutdownStuff()
{
    enkiDeleteTaskScheduler(g_TS);
}

