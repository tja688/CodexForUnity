using UnityEngine;

namespace Tetris
{
    /// <summary>
    /// 方块单元组件
    /// 为单个方块提供视觉表现
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BlockUnit : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            // 如果没有Sprite，使用程序生成的白色方块
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreateBlockSprite();
            }
        }

        /// <summary>
        /// 设置方块颜色
        /// </summary>
        public void SetColor(Color color)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            spriteRenderer.color = color;
        }

        /// <summary>
        /// 创建一个简单的方块Sprite
        /// </summary>
        private static Sprite CreateBlockSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            // 填充白色，带有轻微的边框效果
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // 边框颜色稍暗
                    bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                    float brightness = isBorder ? 0.7f : 1f;

                    // 增加一些3D效果
                    if (x == 1 || y == size - 2)
                        brightness = 1.1f; // 高光
                    if (x == size - 2 || y == 1)
                        brightness = 0.8f; // 阴影

                    brightness = Mathf.Clamp01(brightness);
                    texture.SetPixel(x, y, new Color(brightness, brightness, brightness, 1));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                32f
            );
        }

        // 静态缓存的Sprite
        private static Sprite cachedBlockSprite;

        /// <summary>
        /// 获取或创建共享的方块Sprite
        /// </summary>
        public static Sprite GetSharedBlockSprite()
        {
            if (cachedBlockSprite == null)
            {
                int size = 32;
                Texture2D texture = new Texture2D(size, size);
                texture.filterMode = FilterMode.Point;

                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                        float brightness = isBorder ? 0.7f : 1f;

                        if (x == 1 || y == size - 2)
                            brightness = 1.1f;
                        if (x == size - 2 || y == 1)
                            brightness = 0.8f;

                        brightness = Mathf.Clamp01(brightness);
                        texture.SetPixel(x, y, new Color(brightness, brightness, brightness, 1));
                    }
                }

                texture.Apply();

                cachedBlockSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f),
                    32f
                );
            }

            return cachedBlockSprite;
        }
    }
}
