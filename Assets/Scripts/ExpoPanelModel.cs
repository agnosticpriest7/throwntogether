using System;
using System.Collections.Generic;
using System.Linq;
namespace ThrownTogether
{
    // Display slots only: never assign dishes, reserve capacity, or change tickets.
    public sealed class ExpoPanelModel
    {
        public const int WaitingVisible=5;
        public KitchenTicket[] Slots {get;private set;}=Array.Empty<KitchenTicket>();
        public KitchenTicket[] Waiting {get;private set;}=Array.Empty<KitchenTicket>();
        public int WaitingTop {get;private set;}
        IReadOnlyList<KitchenTicket> history=Array.Empty<KitchenTicket>();
        public int Number(KitchenTicket ticket)
        {for(int i=0;i<history.Count;i++)if(history[i]==ticket)return i+1;return 0;}
        public void Refresh(IReadOnlyList<KitchenTicket> tickets,int capacity,KitchenTicket focus)
        {
            history=tickets;
            if(Slots.Length!=capacity){var slots=new KitchenTicket[capacity];Array.Copy(Slots,slots,Math.Min(capacity,Slots.Length));Slots=slots;}
            bool Active(KitchenTicket t)=>t!=null && tickets.Contains(t) && (t.State==KitchenTicketState.Active || t.State==KitchenTicketState.Ready);
            for(int i=0;i<Slots.Length;i++)if(!Active(Slots[i]))Slots[i]=null;
            foreach(var t in tickets.Where(Active).OrderBy(t=>t.FireSequence))
                if(!Slots.Contains(t)){int empty=Array.IndexOf(Slots,null);if(empty>=0)Slots[empty]=t;}
            Waiting=tickets.Where(t=>t.State==KitchenTicketState.Waiting).ToArray();
            int selected=Array.IndexOf(Waiting,focus);
            if(selected>=0){if(selected<WaitingTop)WaitingTop=selected;if(selected>=WaitingTop+WaitingVisible)WaitingTop=selected-WaitingVisible+1;}
            WaitingTop=Math.Clamp(WaitingTop,0,Math.Max(0,Waiting.Length-WaitingVisible));
        }
    }
}
