#pragma once

#include <stdint.h>

struct Color
{
    union
    {
        uint32_t rgba;
        struct
        {
            uint8_t r, g, b, a;
        } ch;
    };
    Color() {}
    Color(uint8_t rr, uint8_t gg, uint8_t bb, uint8_t aa)
    {
        ch.r = rr;
        ch.g = gg;
        ch.b = bb;
        ch.a = aa;
    }
    explicit Color(uint32_t rgba_)
    {
        rgba = rgba_;
    }

    void Scale(uint32_t scale) // 0..255
    {
        scale += 1;
        uint32_t u = rgba;
        uint32_t lsb = (((u & 0x00ff00ff) * scale) >> 8) & 0x00ff00ff;
        uint32_t msb = (((u & 0xff00ff00) >> 8) * scale) & 0xff00ff00;
        rgba = lsb | msb | 0xff000000;
    }
};

class Texture
{
public:
    Texture(int width, int height) : m_Width(width), m_Height(height), m_Width1(width - 1), m_Height1(height - 1), m_Data(nullptr) { m_Data = new Color[width*height]; }
    ~Texture() { delete[] m_Data; }

    int Width() const { return m_Width; }
    int Height() const { return m_Height; }
    float Width1() const { return m_Width1; }
    float Height1() const { return m_Height1; }
    const Color* Data() const { return m_Data; }
    Color* Data() { return m_Data; }
private:
    int m_Width;
    int m_Height;
    float m_Width1;
    float m_Height1;
    Color* m_Data;
};


void InitializeStuff(Texture* texScope, Texture* texView);
void ShutdownStuff();
void DrawStuff(float time, int screenWidth, int screenHeight, Color* backbuffer);
