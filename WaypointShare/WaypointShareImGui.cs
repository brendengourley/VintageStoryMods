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
                // Load actual waypoints from player data
                ParseWaypointsFromCommand();
            }
            catch (Exception ex)
            {
                capi.Logger.Error($"Error loading waypoints: {ex.Message}");
            }
        }
        
        private void ParseWaypointsFromCommand()
        {
            // Try to load waypoints from the player's waypoint data
            try
            {
                LoadWaypointsFromPlayerData();
            }
            catch (Exception ex)
            {
                capi.Logger.Warning($"Could not load player waypoints: {ex.Message}");
            }
        }
        
        private void LoadWaypointsFromPlayerData()
        {
            // Access player waypoints through the world save data
            var playerData = capi.World.Player.Entity.WatchedAttributes;
            
            // Try to get waypoints from player attributes
            if (playerData.HasAttribute("waypoints"))
            {
                var waypointsAttribute = playerData.GetAttribute("waypoints") as TreeAttribute;
                if (waypointsAttribute != null)
                {
                    foreach (var kvp in waypointsAttribute)
                    {
                        if (kvp.Value is TreeAttribute waypointAttr)
                        {
                            try
                            {
                                var waypoint = new SimpleWaypoint
                                {
                                    Title = waypointAttr.GetString("title", "Untitled"),
                                    Position = new Vec3d(
                                        waypointAttr.GetDouble("x", 0),
                                        waypointAttr.GetDouble("y", 0),
                                        waypointAttr.GetDouble("z", 0)
                                    ),
                                    Color = waypointAttr.GetInt("color", 0xFF0000),
                                    Icon = waypointAttr.GetString("icon", "circle")
                                };
                                waypoints.Add(waypoint);
                            }
                            catch (Exception ex)
                            {
                                capi.Logger.Warning($"Error parsing waypoint: {ex.Message}");
                            }
                        }
                    }
                }
            }
            
            // If no waypoints found in attributes, try alternative approach
            if (waypoints.Count == 0)
            {
                LoadWaypointsFromCommand();
            }
        }
        
        private void LoadWaypointsFromCommand()
        {
            // Since we can't directly parse chat output, we'll attempt the command
            // but won't add fallback waypoints if it fails
            capi.SendChatMessage("/waypoint list");
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

            try
            {
                // Create packet with the actual waypoint data
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
                };\n\n                // Send packet to server
                capi.Network.GetChannel(WaypointShareMod.NetworkChannelId).SendPacket(packet);\n\n                capi.ShowChatMessage($\"Waypoint '{waypoint.Title}' sent to {recipient.PlayerName}\");\n                capi.Logger.Notification($\"Shared waypoint '{waypoint.Title}' at ({waypoint.Position.X:F1}, {waypoint.Position.Y:F1}, {waypoint.Position.Z:F1}) with {recipient.PlayerName}\");\n\n                isOpen = false;\n            }\n            catch (Exception ex)\n            {\n                capi.Logger.Error($\"Error sending waypoint: {ex.Message}\");\n                capi.ShowChatMessage($\"Failed to send waypoint to {recipient.PlayerName}\");\n            }\n        }
    }
}