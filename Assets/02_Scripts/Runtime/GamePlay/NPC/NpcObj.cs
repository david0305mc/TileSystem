using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

public class NpcObj : MonoBehaviour
{

    [SerializeField] private Transform _hudAnchor;
    public Transform HudAnchor => _hudAnchor;
    public NpcHud NpcHud { get; set; }
    [SerializeField] protected SkeletonAnimation _skeletonAnimation;

    [Header("Appearance")]
    [SerializeField, SpineSkin]
    private string _baseSkin = "skin-base";

    [SerializeField, SpineSkin]
    private string[] _hairSkins =
    {
        "hair/blue",
        "hair/brown",
        "hair/long-blue-with-scarf",
        "hair/pink",
        "hair/short-red",
    };

    [SerializeField, SpineSkin]
    private string[] _eyeSkins =
    {
        "eyes/eyes-blue",
        "eyes/green",
        "eyes/violet",
        "eyes/yellow",
    };

    [SerializeField, SpineSkin]
    private string[] _noseSkins =
    {
        "nose/long",
        "nose/short",
    };

    [SerializeField, SpineSkin]
    private string[] _clothesSkins =
    {
        "clothes/dress-blue",
        "clothes/dress-green",
        "clothes/hoodie-blue-and-scarf",
        "clothes/hoodie-orange",
    };

    [SerializeField, SpineSkin]
    private string[] _legSkins =
    {
        "legs/boots-pink",
        "legs/boots-red",
        "legs/pants-green",
        "legs/pants-jeans",
    };

    [SerializeField, SpineSkin]
    private string[] _accessorySkins =
    {
        "accessories/backpack",
        "accessories/bag",
        "accessories/cape-blue",
        "accessories/cape-red",
        "accessories/hat-pointy-blue-yellow",
        "accessories/hat-red-yellow",
        "accessories/scarf",
    };

    [SerializeField, Range(0f, 1f)]
    private float _accessoryChance = 0.5f;

    protected void RandomizeAppearance()
    {
        if (_skeletonAnimation == null)
        {
            return;
        }

        _skeletonAnimation.Initialize(false);

        Skeleton skeleton = _skeletonAnimation.Skeleton;

        if (skeleton == null)
        {
            return;
        }

        SkeletonData skeletonData = skeleton.Data;
        var combinedSkin = new Skin("npc-random");

        AddSkin(combinedSkin, skeletonData, _baseSkin);
        AddRandomSkin(combinedSkin, skeletonData, _hairSkins);
        AddRandomSkin(combinedSkin, skeletonData, _eyeSkins);
        AddRandomSkin(combinedSkin, skeletonData, _noseSkins);
        AddRandomSkin(combinedSkin, skeletonData, _clothesSkins);
        AddRandomSkin(combinedSkin, skeletonData, _legSkins);

        if (Random.value < _accessoryChance)
        {
            AddRandomSkin(combinedSkin, skeletonData, _accessorySkins);
        }

        skeleton.SetSkin(combinedSkin);
        skeleton.SetSlotsToSetupPose();
    }

    private static void AddRandomSkin(
        Skin combinedSkin,
        SkeletonData skeletonData,
        IReadOnlyList<string> skinNames)
    {
        if (skinNames == null || skinNames.Count == 0)
        {
            return;
        }

        AddSkin(
            combinedSkin,
            skeletonData,
            skinNames[Random.Range(0, skinNames.Count)]);
    }

    private static void AddSkin(
        Skin combinedSkin,
        SkeletonData skeletonData,
        string skinName)
    {
        if (string.IsNullOrEmpty(skinName))
        {
            return;
        }

        Skin skin = skeletonData.FindSkin(skinName);

        if (skin != null)
        {
            combinedSkin.AddSkin(skin);
        }
    }
    protected void SetFlip(bool isLeft)
    {
        _skeletonAnimation.Skeleton.ScaleX = isLeft ? -1f : 1f;
    }

    
}
