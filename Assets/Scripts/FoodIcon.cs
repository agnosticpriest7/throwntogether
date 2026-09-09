using UnityEngine;
namespace ThrownTogether
{
    public static class FoodIcon
    {
        private static Texture2D disc;
        private static Texture2D Disc
        {
            get
            {
                if(disc!=null) return disc;
                disc=new Texture2D(32,32,TextureFormat.RGBA32,false) { name="Food icon disc",filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave };
                var pixels=new Color[1024];
                for(int y=0;y<32;y++) for(int x=0;x<32;x++) pixels[y*32+x]=new Color(1,1,1,Mathf.Clamp01(16-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16))));
                disc.SetPixels(pixels); disc.Apply(false,true); return disc;
            }
        }
        public static void Draw(Rect area,IngredientDefinition ingredient)
        {
            var previous=GUI.color;
            GUI.color=new Color(.77f,.84f,.87f); GUI.DrawTexture(new Rect(area.x,area.y+area.height*.39f,area.width,area.height*.56f),Disc);
            GUI.color=Color.white; GUI.DrawTexture(new Rect(area.x+area.width*.07f,area.y+area.height*.43f,area.width*.86f,area.height*.43f),Disc);
            GUI.color=ingredient==null ? Color.white:ingredient.ColorFor(ingredient.platingState);
            if(ingredient!=null && (ingredient.visualKind==IngredientVisualKind.Tomato || ingredient.visualKind==IngredientVisualKind.Lettuce))
            {
                for(int i=0;i<3;i++)
                {
                    GUI.color=new Color(.38f,.72f,.13f); GUI.DrawTexture(new Rect(area.x+area.width*(.02f+i*.23f),area.y+area.height*.28f,area.width*.49f,area.height*.46f),Disc);
                    var r=new Rect(area.x+area.width*(.08f+i*.23f),area.y+area.height*(.25f+(i%2)*.16f),area.width*.4f,area.height*.27f);
                    GUI.color=new Color(.93f,.13f,.06f); GUI.DrawTexture(r,Disc);
                    GUI.color=new Color(1,.55f,.3f); GUI.DrawTexture(new Rect(r.x+r.width*.2f,r.y+r.height*.2f,r.width*.6f,r.height*.6f),Disc);
                }
            }
            else if(ingredient!=null && ingredient.visualKind==IngredientVisualKind.Mushroom)
            {
                for(int i=0;i<3;i++)
                {
                    float x=area.x+area.width*(.1f+i*.25f), y=area.y+area.height*(.15f+(i%2)*.14f);
                    GUI.DrawTexture(new Rect(x+area.width*.085f,y+area.height*.15f,area.width*.11f,area.height*.32f),Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x,y,area.width*.31f,area.height*.29f),Disc);
                }
            }
            else for(int i=0;i<4;i++) GUI.DrawTexture(new Rect(area.x+area.width*(.14f+i*.18f),area.y+area.height*(.13f+(i%2)*.1f),area.width*.12f,area.height*.53f),Texture2D.whiteTexture);
            GUI.color=previous;
        }
    }
}
