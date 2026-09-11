using UnityEngine;
namespace ThrownTogether
{
    public static class OrderBubbleLayout
    {
        // Coordinates use the HUD's existing 1280x720 reference canvas.
        public static Rect ForSeat(Vector2 point,float textScale)
        {
            float scale=Mathf.Clamp(textScale,1,1.4f),width=82.5f*scale,height=82.5f*scale;
            return new Rect(Mathf.Clamp(point.x+36,12,1268-width),Mathf.Clamp(point.y-22,76,612-height),width,height);
        }
        public static bool Visible(CustomerOrder ticket)=>ticket!=null && ticket.Active && ticket.recipe!=null && (ticket.Phase==OrderPhase.Waiting || ticket.Phase==OrderPhase.Delivering);
    }
}
