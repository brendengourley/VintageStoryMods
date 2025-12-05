using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace WaypointShare
{
    public class WaypointShareMod : ModSystem
    {
        private ICoreServerAPI serverApi;
        private ICoreClientAPI clientApi;

        public const string NetworkChannelId = "waypointshare";

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.Logger.Notification("Waypoint Share Mod loaded");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            serverApi = api;
            
            // Register network channel for server-side
            var serverChannel = serverApi.Network.RegisterChannel(NetworkChannelId)
                .RegisterMessageType<WaypointSharePacket>()
                .SetMessageHandler<WaypointSharePacket>(OnServerReceiveWaypoint);
            
            serverApi.Logger.Notification("Waypoint Share Mod: Server-side initialized");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;
            
            // Register network channel for client-side
            var clientChannel = clientApi.Network.RegisterChannel(NetworkChannelId)
                .RegisterMessageType<WaypointSharePacket>()
                .SetMessageHandler<WaypointSharePacket>(OnClientReceiveWaypoint);
            
            // TODO: Set up ImGui rendering integration
            // Following ImGui wiki approach with direct ImGui.NET usage
            // This will need proper integration with Vintage Story's rendering pipeline
            
            clientApi.Input.RegisterHotKey("waypointshare", "Open Waypoint Share Dialog", GlKeys.P, HotkeyType.GUIOrOtherControls, shiftPressed: true);
            clientApi.Input.SetHotKeyHandler("waypointshare", ToggleWaypointShareWindow);
            
            clientApi.Logger.Notification("Waypoint Share Mod: Client-side initialized");
        }

        public void SendWaypointPacket(WaypointSharePacket packet)
        {
            clientApi.Network.GetChannel(NetworkChannelId).SendPacket(packet);
        }

        private bool showWaypointShareWindow = false;
        private WaypointShareImGui waypointShareGui;
        
        private bool ToggleWaypointShareWindow(KeyCombination comb)
        {
            showWaypointShareWindow = !showWaypointShareWindow;
            return true;
        }
        
        private void DrawWaypointShareWindow()
        {
            if (showWaypointShareWindow)
            {
                if (waypointShareGui == null)
                    waypointShareGui = new WaypointShareImGui(clientApi, this);
                    
                showWaypointShareWindow = waypointShareGui.Draw();
            }
        }

        private void OnServerReceiveWaypoint(IServerPlayer fromPlayer, WaypointSharePacket packet)
        {
            // Server receives waypoint from sender and forwards to recipient
            var recipientPlayer = serverApi.World.PlayerByUid(packet.RecipientPlayerUid);
            
            if (recipientPlayer == null)
            {
                serverApi.Logger.Warning($"Waypoint Share: Recipient player {packet.RecipientPlayerUid} not found");
                return;
            }

            // Forward the packet to the recipient
            serverApi.Network.GetChannel(NetworkChannelId).SendPacket(packet, recipientPlayer as IServerPlayer);
            
            serverApi.Logger.Notification($"Waypoint shared from {fromPlayer.PlayerName} to {recipientPlayer.PlayerName}: {packet.WaypointTitle}");
        }

        private void OnClientReceiveWaypoint(WaypointSharePacket packet)
        {
            // Client receives waypoint from server
            var waypointManager = clientApi.ModLoader.GetModSystem<Vintagestory.GameContent.WorldMapManager>();
            
            if (waypointManager != null)
            {
                // Add the waypoint to the client's waypoint list
                var waypoint = new Waypoint
                {
                    Position = new Vintagestory.API.MathTools.Vec3d(packet.X, packet.Y, packet.Z),
                    Title = packet.WaypointTitle,
                    Text = $"Shared by {packet.SenderPlayerName}",
                    Color = packet.Color,
                    Icon = packet.Icon,
                    Pinned = false
                };

                waypointManager.WaypointMapLayer()?.AddWaypoint(waypoint);
                
                clientApi.ShowChatMessage($"Received waypoint '{packet.WaypointTitle}' from {packet.SenderPlayerName}");
            }
        }
    }
}
