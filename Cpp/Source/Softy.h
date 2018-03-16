#pragma once

#include <stdint.h>

struct Color
{
    uint8_t r, g, b, a;
    Color() {}
    Color(uint8_t rr, uint8_t gg, uint8_t bb, uint8_t aa) : r(rr), g(gg), b(bb), a(aa) {}
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
