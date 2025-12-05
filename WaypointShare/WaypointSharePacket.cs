using ProtoBuf;

namespace WaypointShare
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class WaypointSharePacket
    {
        public string SenderPlayerUid { get; set; }
        public string SenderPlayerName { get; set; }
        public string RecipientPlayerUid { get; set; }
        public string WaypointTitle { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public int Color { get; set; }
        public string Icon { get; set; }
    }
}
