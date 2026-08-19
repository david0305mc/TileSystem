
using UnityEngine;
using UnityEngine.U2D;

public class ResourceManager : SingletonMono<ResourceManager>
{
    [SerializeField] private SpriteAtlas spriteAtlas;

    public Sprite GetSpriteFromAtlas(string name)
    {
        return spriteAtlas.GetSprite(name);
    }
}
