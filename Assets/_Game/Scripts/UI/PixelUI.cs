using UnityEngine;

namespace DungeonCrawler.UI
{
    // little generated textures for ui/fx so we don't need extra art files for frames, glows and sparkles
    public static class PixelUI
    {
        public static Texture2D NewTexture(int w, int h, bool pixelated)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = pixelated ? FilterMode.Point : FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        // 9-sliced bevel box. raised (panel): light top/left, dark bottom/right. inset (slot): the other way round
        public static Sprite Bevel(Color fill, Color topLeft, Color bottomRight, Color outline, int bevel = 2)
        {
            const int size = 12;
            bool hasOutline = outline.a > 0f;
            int o = hasOutline ? 1 : 0;
            var tex = NewTexture(size, size, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int left = x, right = size - 1 - x, bottom = y, top = size - 1 - y;
                    int d = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
                    Color c = fill;
                    if (hasOutline && d == 0) c = outline;
                    else if (d < o + bevel)
                    {
                        bool nearTopLeft = d == top || d == left;
                        bool nearBottomRight = d == bottom || d == right;
                        if (nearTopLeft && !nearBottomRight) c = topLeft;
                        else if (nearBottomRight && !nearTopLeft) c = bottomRight;
                        // the two corners where light and shadow meet stay the fill colour
                    }
                    if (hasOutline && (x == 0 || x == size - 1) && (y == 0 || y == size - 1)) c = Color.clear; // rounded corner
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            int e = o + bevel + 1;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(e, e, e, e));
        }

        // soft round light, bright in the middle fading to nothing
        public static Sprite RadialGlow(int size = 64, float pixelsPerUnit = 32f)
        {
            var tex = NewTexture(size, size, false);
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // smoothstep so there's no hard edge
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        // 5x5 pixel twinkle star
        public static Sprite Star(float pixelsPerUnit = 48f)
        {
            string[] rows =
            {
                "..o..",
                "..O..",
                "oOXOo",
                "..O..",
                "..o..",
            };
            var tex = NewTexture(5, 5, true);
            for (int row = 0; row < 5; row++)
            {
                for (int x = 0; x < 5; x++)
                {
                    Color c = rows[row][x] switch
                    {
                        'X' => Color.white,
                        'O' => new Color(1f, 1f, 1f, 0.85f),
                        'o' => new Color(1f, 1f, 1f, 0.4f),
                        _ => Color.clear,
                    };
                    tex.SetPixel(x, 4 - row, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 5, 5), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        public static Sprite Solid(Color c)
        {
            var tex = NewTexture(1, 1, true);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
