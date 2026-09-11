using System;
using UnityEngine;

namespace ThrownTogether
{
    [Serializable]
    public sealed class ChefAppearanceData
    {
        public int build=1, bodyColor, clothing=1, clothingColor, eyes, mouth, hair, headwear=1, glasses, hairColor;
        public ChefAppearanceData Copy() => (ChefAppearanceData)MemberwiseClone();
        public void Normalize()
        {
            build=Mathf.Clamp(build,0,2); bodyColor=Mathf.Clamp(bodyColor,0,ChefWardrobe.Colors.Length-1);
            clothing=Mathf.Clamp(clothing,0,ChefWardrobe.Clothes.Length-1); clothingColor=Mathf.Clamp(clothingColor,0,ChefWardrobe.ClothColors.Length-1);
            eyes=Mathf.Clamp(eyes,0,ChefWardrobe.Eyes.Length-1); mouth=Mathf.Clamp(mouth,0,ChefWardrobe.Mouths.Length-1);
            hair=Mathf.Clamp(hair,0,ChefWardrobe.Hair.Length-1); hairColor=Mathf.Clamp(hairColor,0,ChefWardrobe.HairColors.Length-1); headwear=Mathf.Clamp(headwear,0,3); glasses=Mathf.Clamp(glasses,0,1);
        }
        public static ChefAppearanceData Example(int index)
        {
            switch(index)
            {
                case 1: return new ChefAppearanceData {bodyColor=1,clothing=2,clothingColor=1,eyes=2,mouth=1,headwear=2,glasses=1};
                case 2: return new ChefAppearanceData {bodyColor=2,clothing=2,clothingColor=2,eyes=3,mouth=2,headwear=3};
                case 3: return new ChefAppearanceData {bodyColor=3,clothing=2,clothingColor=3,eyes=1,mouth=3,headwear=0,hair=1};
                default: return new ChefAppearanceData();
            }
        }
    }

    // Choices survive kitchen loads and P2 leaving/rejoining, but reset for a new application session.
    public static class ChefWardrobe
    {
        private static ChefAppearanceData[] players={ChefAppearanceData.Example(0),ChefAppearanceData.Example(1)};
        public static readonly string[] Builds={"Slim","Standard","Fuller"};
        public static readonly string[] Clothes={"None","Waist apron","Bib apron","Buttoned jacket","Striped tee"};
        public static readonly string[] Colors={"Teal","Peach","Lavender","Sky blue","Cocoa","Sand","Rose","Mint","Golden","Slate"};
        public static readonly string[] ClothColors={"Cream","Navy","Pale yellow","Rust orange","Sea green","Plum","Coral","Charcoal","White","Cobalt"};
        public static readonly string[] Eyes={"Round dots","Vertical ovals","Sleepy","Happy curves","Wink","Bright eyes"};
        public static readonly string[] Mouths={"Small smile","Crooked smile","Open grin","Single tooth","Little O","Wide smile"};
        public static readonly string[] Hair={"None","Swept tuft","Curls","Side part","Twin buns"};
        public static readonly string[] HairColors={"Espresso","Black","Chestnut","Honey","Silver","Auburn"};
        public static readonly string[] Hats={"None","Short-brim cap","Folded beanie","Tied headband"};
        public static ChefAppearanceData ForPlayer(int index) => players[Mathf.Clamp(index,0,1)];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => players=new[]{ChefAppearanceData.Example(0),ChefAppearanceData.Example(1)};
    }
}
