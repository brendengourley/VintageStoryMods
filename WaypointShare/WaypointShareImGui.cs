using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using ImGuiNET;

namespace WaypointShare
{
    public class SimpleWaypoint
    {
        public Vec3d Position { get; set; }
        public string Title { get; set; }
        public int Color { get; set; }
        public string Icon { get; set; } = "circle";
    }

    public class WaypointShareImGui
    {
        private ICoreClientAPI capi;
        private WaypointShareMod mod;
        private List<SimpleWaypoint> waypoints;
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
            waypoints = new List<SimpleWaypoint>();

            try
            {
                // Use the /waypoint list command to get player's actual waypoints
                // We'll capture the chat output to parse waypoint information
                ParseWaypointsFromCommand();
                
                // If no waypoints were loaded, add current position as fallback
                if (waypoints.Count == 0)
                {
                    var playerPos = capi.World.Player.Entity.Pos.XYZ;
                    waypoints.Add(new SimpleWaypoint 
                    { 
                        Position = playerPos, 
                        Title = "Current Position",
                        Color = 0xFF0000,
                        Icon = "circle"
                    });
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Error($"Error loading waypoints: {ex.Message}");
            }
        }
        
        private void ParseWaypointsFromCommand()
        {
            // Set up chat message listener to capture waypoint list output
            bool isListening = false;
            
            ClientChatLineDelegate chatHandler = (int groupId, string message, EnumChatType chattype, string data) =>
            {
                if (isListening && chattype == EnumChatType.CommandSuccess)
                {
                    // Parse waypoint line format: "0: Title at [x, y, z]"
                    ParseWaypointLine(message);
                }
            };
            
            capi.Event.ChatMessage += chatHandler;
            
            isListening = true;
            
            // Send the waypoint list command
            capi.SendChatMessage("/waypoint list details");
            
            // Stop listening after a short delay
            long listenerId = 0;
            listenerId = capi.Event.RegisterGameTickListener((dt) => {
                isListening = false;
                capi.Event.ChatMessage -= chatHandler; // Remove chat handler
                capi.Event.UnregisterGameTickListener(listenerId); // Remove this listener
            }, 1000); // Wait 1 second for response
        }
        
        private void ParseWaypointLine(string line)
        {
            try
            {
                // Expected format: "0: Waypoint Name at [100, 64, 200] #FF0000 circle"
                var parts = line.Split(':');
                if (parts.Length < 2) return;
                
                var info = parts[1].Trim();
                var atIndex = info.IndexOf(" at ");
                if (atIndex == -1) return;
                
                var title = info.Substring(0, atIndex).Trim();
                var remaining = info.Substring(atIndex + 4).Trim();
                
                // Parse coordinates [x, y, z]
                var coordStart = remaining.IndexOf('[');
                var coordEnd = remaining.IndexOf(']');
                if (coordStart == -1 || coordEnd == -1) return;
                
                var coordStr = remaining.Substring(coordStart + 1, coordEnd - coordStart - 1);
                var coords = coordStr.Split(',').Select(s => s.Trim()).ToArray();
                
                if (coords.Length != 3) return;
                
                if (double.TryParse(coords[0], out double x) && 
                    double.TryParse(coords[1], out double y) && 
                    double.TryParse(coords[2], out double z))
                {
                    // Parse color and icon if available
                    var afterCoords = remaining.Substring(coordEnd + 1).Trim();
                    var parts2 = afterCoords.Split(' ');
                    
                    int color = 0xFF0000; // Default red
                    string icon = "circle";
                    
                    if (parts2.Length >= 1 && parts2[0].StartsWith("#"))
                    {
                        try { color = Convert.ToInt32(parts2[0].Substring(1), 16); } catch { }
                    }
                    if (parts2.Length >= 2)
                    {
                        icon = parts2[1];
                    }
                    
                    waypoints.Add(new SimpleWaypoint
                    {
                        Position = new Vec3d(x, y, z),
                        Title = title,
                        Color = color,
                        Icon = icon
                    });
                }
            }
            catch (Exception ex)
            {
                capi.Logger.Warning($"Failed to parse waypoint line: {line} - {ex.Message}");
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