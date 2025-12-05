using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using ImGuiNET;

namespace WaypointShare
{
    public class WaypointShareImGui
    {
        private ICoreClientAPI capi;
        private WaypointShareMod mod;
        private List<Vintagestory.API.Datastructures.Waypoint> waypoints;
        private List<IPlayer> onlinePlayers;
        private int selectedWaypointIndex = -1;
        private int selectedPlayerIndex = -1;
        private bool isOpen = true;

        public WaypointShareImGui(ICoreClientAPI capi, WaypointShareMod mod)
        {
            this.capi = capi;
            this.mod = mod;
            LoadWaypoints();
            LoadOnlinePlayers();
        }

        private void LoadWaypoints()
        {
            waypoints = new List<Vintagestory.API.Datastructures.Waypoint>();

            try
            {
                var worldMapManager = capi.ModLoader.GetModSystem<Vintagestory.GameContent.WorldMapManager>();
                if (worldMapManager != null)
                {
                    var waypointLayer = worldMapManager.WaypointMapLayer();
                    if (waypointLayer != null)
                    {
                        waypoints = waypointLayer.Waypoints.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Error($"Error loading waypoints: {ex.Message}");
            }
        }

        private void LoadOnlinePlayers()
        {
            onlinePlayers = new List<IPlayer>();

            foreach (var player in capi.World.AllOnlinePlayers)
            {
                // Don't include yourself in the list
                if (player.PlayerUID != capi.World.Player.PlayerUID)
                {
                    onlinePlayers.Add(player);
                }
            }
        }

        public bool Draw()
        {
            if (!isOpen) return false;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 400), ImGuiCond.FirstUseEver);
            
            if (ImGui.Begin("Share Waypoint", ref isOpen))
            {
                // Refresh data when window opens
                if (ImGui.Button("Refresh Lists"))
                {
                    LoadWaypoints();
                    LoadOnlinePlayers();
                }

                ImGui.Separator();

                // Split window into two columns
                if (ImGui.BeginTable("WaypointShareTable", 2, ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("Waypoints", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Players", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    // Waypoint selection
                    ImGui.Text("Select a waypoint to share:");
                    ImGui.Separator();

                    if (ImGui.BeginListBox("##waypoints", new System.Numerics.Vector2(-1, 250)))
                    {
                        for (int i = 0; i < waypoints.Count; i++)
                        {
                            var waypoint = waypoints[i];
                            string waypointText = $"{waypoint.Title} ({(int)waypoint.Position.X}, {(int)waypoint.Position.Y}, {(int)waypoint.Position.Z})";
                            
                            if (ImGui.Selectable(waypointText, selectedWaypointIndex == i))
                            {
                                selectedWaypointIndex = i;
                            }
                        }
                        ImGui.EndListBox();
                    }

                    ImGui.TableSetColumnIndex(1);

                    // Player selection
                    ImGui.Text("Select a player to send to:");
                    ImGui.Separator();

                    if (ImGui.BeginListBox("##players", new System.Numerics.Vector2(-1, 250)))
                    {
                        for (int i = 0; i < onlinePlayers.Count; i++)
                        {
                            var player = onlinePlayers[i];
                            
                            if (ImGui.Selectable(player.PlayerName, selectedPlayerIndex == i))
                            {
                                selectedPlayerIndex = i;
                            }
                        }
                        ImGui.EndListBox();
                    }

                    ImGui.EndTable();
                }

                ImGui.Separator();

                // Buttons
                bool canSend = selectedWaypointIndex >= 0 && selectedWaypointIndex < waypoints.Count &&
                              selectedPlayerIndex >= 0 && selectedPlayerIndex < onlinePlayers.Count;

                if (!canSend)
                    ImGui.BeginDisabled();

                if (ImGui.Button("Send Waypoint"))
                {
                    SendWaypoint();
                }

                if (!canSend)
                    ImGui.EndDisabled();

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    isOpen = false;
                }

                // Display selection info
                if (selectedWaypointIndex >= 0 && selectedWaypointIndex < waypoints.Count)
                {
                    var waypoint = waypoints[selectedWaypointIndex];
                    ImGui.Separator();
                    ImGui.Text($"Selected waypoint: {waypoint.Title}");
                    ImGui.Text($"Location: {(int)waypoint.Position.X}, {(int)waypoint.Position.Y}, {(int)waypoint.Position.Z}");
                }

                if (selectedPlayerIndex >= 0 && selectedPlayerIndex < onlinePlayers.Count)
                {
                    var player = onlinePlayers[selectedPlayerIndex];
                    ImGui.Text($"Sending to: {player.PlayerName}");
                }
            }
            ImGui.End();

            return isOpen;
        }

        private void SendWaypoint()
        {
            if (selectedWaypointIndex < 0 || selectedWaypointIndex >= waypoints.Count)
            {
                capi.ShowChatMessage("Please select a waypoint to share");
                return;
            }

            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= onlinePlayers.Count)
            {
                capi.ShowChatMessage("Please select a player to send the waypoint to");
                return;
            }

            var waypoint = waypoints[selectedWaypointIndex];
            var recipient = onlinePlayers[selectedPlayerIndex];

            // Create packet
            var packet = new WaypointSharePacket
            {
                SenderPlayerUid = capi.World.Player.PlayerUID,
                SenderPlayerName = capi.World.Player.PlayerName,
                RecipientPlayerUid = recipient.PlayerUID,
                WaypointTitle = waypoint.Title,
                X = waypoint.Position.X,
                Y = waypoint.Position.Y,
                Z = waypoint.Position.Z,
                Color = waypoint.Color,
                Icon = waypoint.Icon
            };

            // Send packet to server
            capi.Network.GetChannel(WaypointShareMod.NetworkChannelId).SendPacket(packet);

            capi.ShowChatMessage($"Waypoint '{waypoint.Title}' sent to {recipient.PlayerName}");

            isOpen = false;
        }
    }
}