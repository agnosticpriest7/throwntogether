using UnityEngine;
namespace ThrownTogether
{
    public static class OrderBubbleLayout
    {
        // Coordinates use the HUD's existing 1280x720 reference canvas.
        public static Rect ForSeat(Vector2 point,float textScale)
        {
            float scale=Mathf.Clamp(textScale,1,1.4f),width=206*scale,height=100*scale;
            return new Rect(Mathf.Clamp(point.x+36,12,1268-width),Mathf.Clamp(point.y-22,76,612-height),width,height);
        }
        public static string State(OrderPhase phase)=>phase==OrderPhase.Waiting?"TO COOK":phase==OrderPhase.Delivering?"ON THE WAY":phase==OrderPhase.Eating?"ENJOYING":"SERVED";
    }
}
