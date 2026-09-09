using UnityEngine;
namespace ThrownTogether
{
    public static class FoodIcon
    {
        public static void Draw(Rect area,IngredientDefinition ingredient)
        {
            var previous=GUI.color;
            GUI.color=Color.white; GUI.DrawTexture(new Rect(area.x,area.y+area.height*.75f,area.width,area.height*.12f),Texture2D.whiteTexture);
            GUI.color=ingredient==null ? Color.white : ingredient.ColorFor(FoodState.Cooked);
            if(ingredient!=null && ingredient.visualKind==IngredientVisualKind.Mushroom)
            {
                GUI.DrawTexture(new Rect(area.x+area.width*.4f,area.y+area.height*.35f,area.width*.2f,area.height*.4f),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(area.x+area.width*.1f,area.y+area.height*.15f,area.width*.8f,area.height*.3f),Texture2D.whiteTexture);
            }
            else for(int i=0;i<4;i++) GUI.DrawTexture(new Rect(area.x+area.width*(.1f+i*.2f),area.y+area.height*(.1f+(i%2)*.1f),area.width*.12f,area.height*.6f),Texture2D.whiteTexture);
            GUI.color=previous;
        }
    }
}
