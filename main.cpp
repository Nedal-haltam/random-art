#include <iostream>
#include <fstream>
#include <time.h>
#include <math.h>
#define WIDTH 256
#define HEIGHT 256

typedef struct Color
{
    int r;
    int g;
    int b;
} Color;

typedef struct Vector3
{
    float x;
    float y;
    float z;
} Vector3;

Color ToColor(Vector3 v, float min = -1, float max = 1)
{
    // min..max
    // 0..255
    float m = -min;
    return {(int)((v.x + (m))*(255.0f / (max - min))), (int)((v.y + (m))*(255.0f / (max - min))), (int)((v.z + (m))*(255.0f / (max - min)))};
}

Color GenGradient(float x, float y)
{
    Vector3 v = {x, x, x};
    return ToColor(v);
}

Color GetColorFromxy(float x, float y)
{
    if (x * y >= 0) return ToColor({x, y, 1});
    float t = fmodf(x, y);
    return ToColor({t, t, t});
}

bool GeneratePPM(std::string FilePath)
{
    std::ofstream image(FilePath);
    if (!image) {
        std::cerr << "Could not open the file for writing.\n";
        return false;
    }
    image << "P3\n" << WIDTH << " " << HEIGHT << "\n255\n";
    for (int y = 0; y < HEIGHT; ++y) {
        float Normalizedy = ((float)y / HEIGHT) * 2 - 1;
        for (int x = 0; x < WIDTH; ++x) {
            float Normalizedx = ((float)x / WIDTH) * 2 - 1;
            // Color c = GenGradient(Normalizedx, Normalizedy);
            Color c = GetColorFromxy(Normalizedx, Normalizedy);
            image << c.r << ' ' << c.g << ' ' << c.b << '\n';
        }
    }
    image.close();
    return true;
}

int main() 
{
    srand(time(0));
    if (!GeneratePPM("output.ppm")) return 1;
    std::cout << "PPM image generated: output.ppm\n";
    return 0;
}
