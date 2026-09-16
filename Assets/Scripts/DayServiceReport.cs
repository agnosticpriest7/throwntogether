namespace ThrownTogether
{
    // Presentation only: observations explain the completed day without changing settlement.
    public static class DayServiceReport
    {
        public static string Variety(RestaurantDay day)
        {
            if(day.Menu.Length<4)return "Variety: choose 4+ dishes and serve 4 different dishes to qualify.";
            if(day.DistinctDishesServed<4)return "Variety: "+day.DistinctDishesServed+"/4 different dishes served. Selecting them alone is not enough.";
            return "Variety earned: 5% of dish sales, rounded down; maximum $15.";
        }
        public static string Advice(RestaurantDay day)
        {
            string outside=day.LostOutside==0?"":day.QueueWithDirtyTablesSeconds>=1
                ?"Dirty tables and queues overlapped: clear faster or consider a busser."
                :"Outside losses: check table turnover and host access; consider more seats.";
            string seated=day.LostUnserved==0?"":"Unserved guests: check cooking capacity, delivery help or a smaller menu.";
            if(outside.Length>0 && seated.Length>0)return outside+"\n"+seated;
            if(outside.Length>0)return outside;
            if(seated.Length>0)return seated;
            if(day.Served==0)return day.CustomersArrived==0?"No customers arrived; there is not enough service data yet.":"Only closing-time turnaways were recorded. No patience losses to diagnose.";
            return "No patience losses today. Keep this layout, or test a broader menu before expanding.";
        }
    }
}
