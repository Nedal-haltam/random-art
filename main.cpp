#include <iostream>
#include <fstream>
#include <time.h>
#include <math.h>
#include <assert.h>

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

typedef struct Node
{

} Node;

Color eval(Node* f, float x, float y)
{
    assert(false && "TODO: eval\n");
    return {};
}

// bool GeneratePPM(std::string FilePath, Color (*f)(float, float))
bool GeneratePPM(std::string FilePath, Node *f)
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
            // Color c = f(Normalizedx, Normalizedy);
            Color c = eval(f, Normalizedx, Normalizedy);
            image << c.r << ' ' << c.g << ' ' << c.b << '\n';
        }
    }
    image.close();
    return true;
}

int main() 
{
    srand(time(0));
    std::string FilePath = "output.ppm";

    // if (!GeneratePPM(FilePath, GenGradient)) return 1;
    Node n = Node();
    if (!GeneratePPM(FilePath, &n)) return 1;

    std::cout << "INFO: PPM image generated: " << FilePath << "\n";
    return 0;
}
