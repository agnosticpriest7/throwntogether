using UnityEngine;
namespace ThrownTogether
{
    public static class FoodIcon
    {
        private static Texture2D disc;
        private static readonly System.Collections.Generic.Dictionary<string,Texture2D> dishImages=new System.Collections.Generic.Dictionary<string,Texture2D>();
        public static Texture2D DishImage(RecipeDefinition recipe){if(!dishImages.TryGetValue(recipe.id,out var image)){image=Resources.Load<Texture2D>("DishIcons/"+recipe.id);dishImages[recipe.id]=image;}return image;}
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
        private static Color WithOpacity(Color color,float opacity) { color.a*=opacity; return color; }
        public static void Draw(Rect area,RecipeDefinition recipe,float opacity=1)
        {
            var image=DishImage(recipe);if(image!=null){var prior=GUI.color;GUI.color=new Color(1,1,1,opacity);GUI.DrawTexture(area,image,ScaleMode.ScaleToFit);GUI.color=prior;return;}
            int count=1+recipe.additionalIngredients.Length;
            if(count==1){Draw(area,recipe.ingredient,opacity,recipe.requiredState);return;}
            float width=area.width/(count==3?1.8f:1.5f);
            Draw(new Rect(area.x,area.y,width,area.height*.8f),recipe.ingredient,opacity,recipe.requiredState);
            for(int i=0;i<recipe.additionalIngredients.Length;i++)
            {var p=recipe.additionalIngredients[i];Draw(new Rect(area.x+area.width*.42f,area.y+i*area.height*.35f,width,area.height*.65f),p.ingredient,opacity,p.state);}
        }
        public static void Draw(Rect area,IngredientDefinition ingredient,float opacity=1,FoodState? state=null)
        {
            var previous=GUI.color;
            GUI.color=WithOpacity(new Color(.77f,.84f,.87f),opacity); GUI.DrawTexture(new Rect(area.x,area.y+area.height*.39f,area.width,area.height*.56f),Disc);
            GUI.color=WithOpacity(Color.white,opacity); GUI.DrawTexture(new Rect(area.x+area.width*.07f,area.y+area.height*.43f,area.width*.86f,area.height*.43f),Disc);
            GUI.color=WithOpacity(ingredient==null ? Color.white:ingredient.ColorFor(state??ingredient.platingState),opacity);
            if(ingredient!=null && ingredient.visualKind==IngredientVisualKind.Egg)
            {
                GUI.color=WithOpacity(Color.white,opacity);GUI.DrawTexture(new Rect(area.x+area.width*.1f,area.y+area.height*.2f,area.width*.8f,area.height*.5f),Disc);
                GUI.color=WithOpacity(new Color(1,.7f,.05f),opacity);GUI.DrawTexture(new Rect(area.x+area.width*.35f,area.y+area.height*.28f,area.width*.32f,area.height*.3f),Disc);
            }
            else if(ingredient!=null && ingredient.visualKind==IngredientVisualKind.Chicken)
            {
                GUI.DrawTexture(new Rect(area.x+area.width*.1f,area.y+area.height*.17f,area.width*.8f,area.height*.5f),Disc);
                GUI.color=WithOpacity(new Color(.28f,.14f,.05f),opacity);
                for(int i=0;i<3;i++)GUI.DrawTexture(new Rect(area.x+area.width*.26f,area.y+area.height*(.27f+i*.11f),area.width*.46f,area.height*.035f),Texture2D.whiteTexture);
            }
            else if(ingredient!=null && ingredient.visualKind==IngredientVisualKind.Potato && state==FoodState.Griddled)
            {for(int i=0;i<2;i++)GUI.DrawTexture(new Rect(area.x+area.width*(.12f+i*.35f),area.y+area.height*.2f,area.width*.42f,area.height*.45f),Disc);}
            else if(ingredient!=null && (ingredient.visualKind==IngredientVisualKind.Tomato || ingredient.visualKind==IngredientVisualKind.Lettuce))
            {
                for(int i=0;i<3;i++)
                {
                    if(ingredient.visualKind==IngredientVisualKind.Lettuce){GUI.color=WithOpacity(new Color(.38f,.72f,.13f),opacity); GUI.DrawTexture(new Rect(area.x+area.width*(.02f+i*.23f),area.y+area.height*.28f,area.width*.49f,area.height*.46f),Disc);continue;}
                    var r=new Rect(area.x+area.width*(.08f+i*.23f),area.y+area.height*(.25f+(i%2)*.16f),area.width*.4f,area.height*.27f);
                    GUI.color=WithOpacity(new Color(.93f,.13f,.06f),opacity); GUI.DrawTexture(r,Disc);
                    GUI.color=WithOpacity(new Color(1,.55f,.3f),opacity); GUI.DrawTexture(new Rect(r.x+r.width*.2f,r.y+r.height*.2f,r.width*.6f,r.height*.6f),Disc);
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
