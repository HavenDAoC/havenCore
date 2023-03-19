/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Commands
{
	/// <summary>
	/// command handler for /gc command
	/// </summary>
	[Cmd(
		"&gc",
		new string[] { "&guildcommand" },
		ePrivLevel.Player,
		"Guild command (use /gc help for options)",
		"/gc <option>")]
	public class GuildCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public long GuildFormCost = Money.GetMoney(0, 0, 1, 0, 0); //Cost to form guild : live = 1g : (mith/plat/gold/silver/copper)
		
		/// <summary>
		/// Checks if a guildname has valid characters
		/// </summary>
		/// <param name="guildName"></param>
		/// <returns></returns>
		public static bool IsValidGuildName(string guildName)
		{
			if (!Regex.IsMatch(guildName, @"^[a-zA-Z àâäèéêëîïôœùûüÿçÀÂÄÈÉÊËÎÏÔŒÙÛÜŸÇ]+$") || guildName.Length < 0)

			{
				return false;
			}
			return true;
		}
		private static bool IsNearRegistrar(GamePlayer player)
		{
			foreach (GameNPC registrar in player.GetNPCsInRadius(500))
			{
				if (registrar is GuildRegistrar)
					return true;
			}
			return false;
		}
		private static bool GuildFormCheck(GamePlayer leader)
		{
			Group group = leader.Group;
			
			// Check to make sure the forming leader is in a group
			if (group == null)
			{
				// Message: You must be in a group to form a guild.
				ChatUtil.SendTypeMessage((int)eMsg.Error, leader, "Scripts.Player.Guild.FormNoGroup", null);
				return false;
			}
			
			// The group must consist of the full required amount to form a guild
			if (group.MemberCount < Properties.GUILD_NUM)
			{
				// Message: You need {0} members in your group to form a guild.
				ChatUtil.SendTypeMessage((int)eMsg.Error, leader, "Scripts.Player.Guild.FormNoMembers", Properties.GUILD_NUM);
				return false;
			}

			return true;
		}

		protected void CreateGuild(GamePlayer player, byte response)
		{
			if (player.Group == null)
			{
				// Message: There was an issue processing the guild formation request. Please try again.
				ChatUtil.SendTypeMessage((int)eMsg.Error, player, "Scripts.Player.Guild.ErrorForming", null);
				return;
			}

			#region Player Declines
			if (response != 0x01)
			{
				// Inform all players that leader has not agreed to form the guild
				// Remove all guild consider states to enable retry
				foreach (GamePlayer ply in player.Group.GetPlayersInTheGroup())
				{
					ply.TempProperties.removeProperty("Guild_Consider");
					// Message: {0} has declined to form the guild.
					ChatUtil.SendTypeMessage((int)eMsg.Important, ply, "Scripts.Player.Guild.DeclinesToForm", player.Name);
				}
				
				player.Group.Leader.TempProperties.removeProperty("Guild_Name");
				
				return;
			}
			#endregion Player Declines
			
			#region Player Accepts
			// Inform each player that the group leader is agreeing to form the guild.
			foreach (GamePlayer ply in player.Group.GetPlayersInTheGroup())
			{
				ply.TempProperties.removeProperty("Guild_Consider");
				// Message: {0} has agreed to form the guild!
				ChatUtil.SendTypeMessage((int)eMsg.Important, ply, "Scripts.Player.Guild.AgreesToForm", player.Name);
			}

			player.TempProperties.setProperty("Guild_Consider", true);
			var guildName = player.Group.Leader.TempProperties.getProperty<string>("Guild_Name");

			var memNum = player.Group.GetPlayersInTheGroup().Count(p => p.TempProperties.getProperty<bool>("Guild_Consider"));

			if (!GuildFormCheck(player) || memNum != player.Group.MemberCount) return;

			// Behaviors to follow if guilds require more than one player to form
			if (Properties.GUILD_NUM > 1)
			{
				Group group = player.Group;
				lock (group)
				{
					Guild newGuild = GuildMgr.CreateGuild(player.Realm, guildName, player);
					if (newGuild == null)
					{
						// Message: The guild {0} was unable to be created with {1} as its leader.
						ChatUtil.SendTypeMessage((int)eMsg.Error, player, "Scripts.Player.Guild.UnableToCreateLead", guildName, player.Name);
					}
					else
					{
						foreach (GamePlayer ply in group.GetPlayersInTheGroup())
						{
							if (ply != group.Leader)
							{
								newGuild.AddPlayer(ply);
							}
							else
							{
								newGuild.AddPlayer(ply, newGuild.GetRankByID(0));
							}
							ply.TempProperties.removeProperty("Guild_Consider");
						}
						player.Group.Leader.TempProperties.removeProperty("Guild_Name");
						player.Group.Leader.RemoveMoney(GuildFormCost);
						
						// Message: {0} is formed. {1} is the Guildmaster and must now adjust the settings for all members.
						ChatUtil.SendTypeMessage((int)eMsg.Important, player, "Scripts.Player.Guild.GuildCreated", guildName, player.Group.Leader.Name);
					}
				}
			}
			else
			{
				Guild newGuild = GuildMgr.CreateGuild(player.Realm, guildName, player);

				if (newGuild == null)
				{
					// Message: The guild {0} was unable to be created with {1} as its leader.
					ChatUtil.SendTypeMessage((int)eMsg.Error, player, "Scripts.Player.Guild.UnableToCreateLead", guildName, player.Name);
				}
				else
				{
					newGuild.AddPlayer(player, newGuild.GetRankByID(0));
					player.TempProperties.removeProperty("Guild_Name");
					player.RemoveMoney(10000);
					// Message: {0} is formed. {1} is the Guildmaster and must now adjust the settings for all members.
					ChatUtil.SendTypeMessage((int)eMsg.Important, player, "Scripts.Player.Guild.GuildCreated", guildName, player.Group.Leader.Name);
				}
			}
			#endregion Player Accepts
		}

		/// <summary>
		/// method to handle /gc commands from a client
		/// </summary>
		/// <param name="client"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "gc", 500))
				return;

			try
			{
				if (args.Length == 1)
				{
					DisplayHelp(client);
					return;
				}

				if (client.Player.IsIncapacitated)
				{
					return;
				}


				string message;

				// Use this to aid in debugging social window commands
				//string debugArgs = "";
				//foreach (string arg in args)
				//{
				//    debugArgs += arg + " ";
				//}
				//log.Debug(debugArgs);

				switch (args[1])
				{
					#region Create (Admin/GM command)
					// --------------------------------------------------------------------------------
					// CREATE (Admin/GM command)
					// '/gc create <guildName>'
					// Manually creates a new guild with the targeted player as leader, overruling the standard group requirement to form.
					// --------------------------------------------------------------------------------
					case "create":
						{
							// Players cannot perform this command
							if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
								return;

							if (args.Length < 3)
							{
								// Message: '/gc create <guildName>' - Creates a new guild with the targeted player as its leader.
								ChatUtil.SendTypeMessage((int)eMsg.CmdSyntax, client, "Scripts.Player.Guild.Help.GuildGMCreate", null);
								return;
							}

							// Use targeted player as leader for the guild
							GameLiving guildLeader = client.Player.TargetObject as GamePlayer;
							string guildname = String.Join(" ", args, 2, args.Length - 2);
							guildname = GameServer.Database.Escape(guildname);
							
							//Check to make sure a player is targeted
							if (guildLeader == null)
							{
								// Message: To create a guild, you must first select a player to make its leader.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.PlayerNotFound", null);
								return;
							}
							
							// Check to make sure the guild name isn't already taken
							if (!GuildMgr.DoesGuildExist(guildname))
							{
								// Check for invalid characters
								if (!IsValidGuildName(guildname))
								{
									// Message: Some of the characters entered for the guild name are invalid.
									ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.InvalidLetters", null);
									return;
								}
								
								// Create the guild using the target's realm
								Guild newGuild = GuildMgr.CreateGuild(guildLeader.Realm, guildname, client.Player);
								
								// If there's some problem with creating the guild, throw an error
								if (newGuild == null)
								{
									// Message: The guild {0} could not be created.
									ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.UnableToCreate", guildname);
								}
								else
								{
									// Add the targeted player and give them an appropriate guild rank
									newGuild.AddPlayer((GamePlayer)guildLeader);
									((GamePlayer)guildLeader).GuildRank = ((GamePlayer)guildLeader).Guild.GetRankByID(0);
									
									// Message: {0} is formed. {1} is the Guildmaster and must now adjust the settings for all members.
									ChatUtil.SendTypeMessage((int)eMsg.Important, client, "Scripts.Player.Guild.GuildCreated", guildname, ((GamePlayer)guildLeader).Name);
								}
								return;
							}
							
							// Message: The guild cannot be created because one with that name already exists.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.GuildExists", null);
							
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
					#endregion Create (Admin/GM command)
					#region Purge (Admin/GM command)
					// --------------------------------------------------------------------------------
					// PURGE (Admin/GM command)
					// '/gc purge <guildName>'
					// Deletes a guild completely from the database and disbands all members.
					// --------------------------------------------------------------------------------
					case "purge":
					{
						// Players cannot perform this command
						if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
							return;

						if (args.Length < 3)
						{
							// Message: '/gc purge <guildName>' - Disbands and deletes a guild completely.
							ChatUtil.SendTypeMessage((int)eMsg.CmdSyntax, client, "Scripts.Player.Guild.Help.GuildGMPurge", null);
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMPurge"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
						}
						string guildname = String.Join(" ", args, 2, args.Length - 2);
						if (!GuildMgr.DoesGuildExist(guildname))
						{
							// Message: No guild exists with that name.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.GuildNotExist", null);
							return;
						}

						// Delete the guild
						GuildMgr.DeleteGuild(guildname);
							
						// Message: {0} has been deleted from the database and all guild members disbanded.
						ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "Scripts.Player.Guild.Purged", guildname);
					}
						break;
					#endregion Purge (Admin/GM command)
					#region Rename (Admin/GM command)
					// --------------------------------------------------------------------------------
					// RENAME (Admin/GM command)
					// '/gc rename <oldName> to <newName>'
					// Forces a manual rename of the guild to a new value.
					// --------------------------------------------------------------------------------
					case "rename":
						{
							// Players cannot perform this command
							if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
								return;

							// Must follow syntax of '/gc rename <oldName> to <newName>'
							if (args.Length < 5)
							{
								// Message: '/gc rename <oldName> to <newName' - Chances a guild's existing name to the specified new name. Use double quotes for multi-word guild names.
								ChatUtil.SendTypeMessage((int)eMsg.CmdSyntax, client, "Scripts.Player.Guild.Help.GuildGMRename", null);
								return;
							}
							
							// Parse the command
							int i;
							for (i = 2; i < args.Length; i++)
							{
								if (args[i] == "to")
									break;
							}

							// Parse the command some more
							// Put guild names in " " if you need to specify longer names
							string oldGuildName = String.Join(" ", args, 2, i - 2);
							string newGuildName = String.Join(" ", args, i + 1, args.Length - i - 1);
							
							// If the guild name doesn't exist, then throw an error
							if (!GuildMgr.DoesGuildExist(oldGuildName))
							{
								// Message: No guild exists with that name.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.GuildNotExist", null);
								return;
							}
							
							// Get the existing guild by name and add the new one
							Guild myGuild = GuildMgr.GetGuildByName(oldGuildName);
							myGuild.Name = newGuildName;
							GuildMgr.AddGuild(myGuild);
							
							// Update the guild name and inform everyone that the change has happened.
							foreach (GamePlayer ply in myGuild.GetListOfOnlineMembers())
							{
								ply.GuildName = newGuildName;
								// Message: Your guild's name has been changed to '{0}'.
								ChatUtil.SendTypeMessage((int)eMsg.Important, ply, "Scripts.Player.Guild.GuildNameRenamed", newGuildName);
							}
							
							// Delete the old guild & update accordingly
							GuildMgr.DeleteGuild(oldGuildName);
							client.Player.Guild.UpdateGuildWindow();
							
							// Message: You have changed the guild's name to '{0}'.
							ChatUtil.SendTypeMessage((int)eMsg.Important, client, "Scripts.Player.Guild.YouChangedGuildName", GuildMgr.GetGuildByName(newGuildName).Name);
						}
						break;
					#endregion Rename (Admin/GM command)
					#region AddPlayer (Admin/GM command)
					// --------------------------------------------------------------------------------
					// ADDPLAYER (Admin/GM command)
					// '/gc addplayer <playerName> to <guildName>'
					// Forces a player to become a member of the guild, without prompting for acceptance.
					// --------------------------------------------------------------------------------
					case "addplayer":
						{
							// Players cannot perform this command
							if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
								return;

							// Make sure the command is long enough
							if (args.Length < 5)
							{
								// Message: '/gc addplayer <playerName> to <guildName>' - Adds a player automatically to the specified guild without an invitation.
								ChatUtil.SendTypeMessage((int)eMsg.CmdSyntax, client, "Scripts.Player.Guild.Help.GuildGMAddPlayer", null);
								return;
							}

							// Parse the command
							int i;
							for (i = 2; i < args.Length; i++)
							{
								if (args[i] == "to")
									break;
							}

							// Parse the command some more
							string playerName = String.Join(" ", args, 2, i - 2);
							string guildName = String.Join(" ", args, i + 1, args.Length - i - 1);
							
							// If the guild name doesn't exist, then throw an error
							if (!GuildMgr.DoesGuildExist(guildName))
							{
								// Message: No guild exists with that name.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.GuildNotExist", null);
								return;
							}
							
							// Get the player by client
							var addedPlayer = WorldMgr.GetClientByPlayerName(playerName, true, false).Player;

							// Add the player to the guild & update accordingly
							GuildMgr.GetGuildByName(guildName).AddPlayer(addedPlayer);
							client.Player.Guild.UpdateGuildWindow();
							
							// Message: You have added {0} to the guild {1}.
							ChatUtil.SendTypeMessage((int)eMsg.Important, client, "Scripts.Player.Guild.AddedToGuild", addedPlayer.Name, GuildMgr.GetGuildByName(guildName).Name);
							// Message: Eclipse staff have added you to the guild {0}.
							ChatUtil.SendTypeMessage((int)eMsg.Important, addedPlayer, "Scripts.Player.Guild.YouHaveBeenAdded", GuildMgr.GetGuildByName(guildName).Name);
						}
						break;
					#endregion AddPlayer (Admin/GM command)
					#region RemovePlayer (Admin/GM command)
					// --------------------------------------------------------------------------------
					// REMOVEPLAYER (Admin/GM command)
					// '/gc removeplayer <player> from <guildName>'
					// Removes the identified player immediately from the guild.
					// --------------------------------------------------------------------------------
					case "removeplayer":
						{
							// Players cannot perform this command
							if (client.Account.PrivLevel == (uint)ePrivLevel.Player)
								return;

							// Make sure the command is long enough
							if (args.Length < 5)
							{
								// Message: '/gc removeplayer <player> from <guildName>' - Removes the identified player immediately from the guild.
								ChatUtil.SendTypeMessage((int)eMsg.CmdSyntax, client, "Scripts.Player.Guild.Help.GuildGMRemovePlayer", null);
								return;
							}

							// Parse the command
							int i;
							for (i = 2; i < args.Length; i++)
							{
								if (args[i] == "from")
									break;
							}

							// Parse the command some more
							string playerName = String.Join(" ", args, 2, i - 2);
							string guildName = String.Join(" ", args, i + 1, args.Length - i - 1);

							// If the guild name doesn't exist, then throw an error
							if (!GuildMgr.DoesGuildExist(guildName))
							{
								// Message: No guild exists with that name.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.GuildNotExist", null);
								return;
							}
							
							// Get the player by client
							var removedPlayer = WorldMgr.GetClientByPlayerName(playerName, true, false).Player;

							// Remove the player from the guild & update accordingly
							GuildMgr.GetGuildByName(guildName).RemovePlayer("Eclipse staff", removedPlayer);
							client.Player.Guild.UpdateGuildWindow();

							// Message: You have removed {0} from the guild {1}.
							ChatUtil.SendTypeMessage((int)eMsg.Important, client, "Scripts.Player.Guild.RemovedFromGuild", removedPlayer.Name, GuildMgr.GetGuildByName(guildName).Name);
							// Message: Eclipse staff have removed you from the guild {0}.
							ChatUtil.SendTypeMessage((int)eMsg.Important, removedPlayer, "Scripts.Player.Guild.YouHaveBeenRemoved", GuildMgr.GetGuildByName(guildName).Name);
						}
						break;
					#endregion RemovePlayer (Admin/GM command)
					#region Invite
					// --------------------------------------------------------------------------------
					// INVITE
					// '/gc invite <playerName>'
					// Invites the targeted or specified player to join your guild.
					// --------------------------------------------------------------------------------
					case "invite":
						{
							// Player must be a member of an existing guild to invite players
							if (client.Player.Guild == null)
							{
								// Message: You must be a member of a guild to use any guild commands.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
								return;
							}

							// Inviting player must have sufficient rank privileges in the guild to invite new members
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Invite))
							{
								// Message: You do not have high sufficient privileges in your guild to use that command.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPrivileges", null);
								return;
							}

							// If possible, players can invite by only targeting the desired character
							GamePlayer guildInvitee = client.Player.TargetObject as GamePlayer;
							
							// If the player specifies a character by name as part of the command, use that instead
							if (args.Length > 2)
							{
								// Grab the player's name from the command
								var playerName = args[2];
								
								// Get the player's client (whole name specified and the player must be online)
								GameClient playerClient = WorldMgr.GetClientByPlayerName(playerName, true, true);
								
								// Now grab the active player
								if (playerClient != null)
									guildInvitee = playerClient.Player;
							}
							
							// If the player doesn't exist, isn't online, or is a member of another realm (ignore realm requirement if inviter is an Admin/GM)
							if (guildInvitee == null || (guildInvitee.Realm != client.Player.Realm && client.Account.PrivLevel == (int)ePrivLevel.Player))
							{
								// Message: You must select or specify an active player in your realm.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.InviteNoSelected", null);
								return;
							}
							
							// Make sure inviter isn't trying to invite self
							if (guildInvitee == client.Player)
							{
								// Message: You can't invite yourself.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.InviteNoSelf", null);
								return;
							}

							// If the invitee is already a member of a guild
							if (guildInvitee.Guild != null)
							{
								// If they're in your guild, mention that
								if (guildInvitee.Guild == client.Player.Guild)
								{
									// Message: {0} is already a member of you guild.
									ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.AlreadyInYourGuild", guildInvitee.Name);
									return;
								}

								// Message: {0} is already a member of a guild.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.AlreadyInGuild", guildInvitee.Name);
								return;
							}
							
							// Make sure the invitee is not dead
							if (!guildInvitee.IsAlive)
							{
								// Message: You cannot invite a dead player to your guild.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.InviteDead", null);
								return;
							}
							
							// Make sure the invitee is an entity that can be grouped with according to server rules
							if (!GameServer.ServerRules.IsAllowedToGroup(client.Player, guildInvitee, true))
							{
								// Message: You cannot invite this character.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.InviteNotThis", null);
								return;
							}
							
							// Make sure they can join a guild according to server rules
							if (!GameServer.ServerRules.IsAllowedToJoinGuild(guildInvitee, client.Player.Guild))
							{
								// Message: You cannot invite this character.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.InviteNotThis", null);
								return;
							}
							
							// Finally send out the guild invite
							// Message: {0} has invited you to join their guild.
							guildInvitee.Out.SendGuildInviteCommand(client.Player, LanguageMgr.GetTranslation(guildInvitee.Client.Account.Language, "Scripts.Player.Guild.InviteReceived", client.Player.Name));
							
							// Message: You have invited {0} to join your guild.
							ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InviteSent", guildInvitee.Name);
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
					#endregion Invite
					#region Remove
					// --------------------------------------------------------------------------------
					// REMOVE
					// '/gc remove <playerName>'
					// Removes a specific player character from the guild.
					// --------------------------------------------------------------------------------
					case "remove":
						{
							// Player must be a member of an existing guild to remove players
							if (client.Player.Guild == null)
							{
								// Message: You must be a member of a guild to use any guild commands.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
								return;
							}

							// Inviting player must have sufficient rank privileges in the guild to invite new members
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Remove))
							{
								// Message: You do not have high sufficient privileges in your guild to use that command.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPrivileges", null);
								return;
							}

							// Make sure the command has a player name specified
							if (args.Length < 3)
							{
								// Message: '/gc remove <playerName>' - Removes a specific player character from the guild.
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.Help.GuildRemove", null);
								return;
							}

							// Vars for command target
							object target = null;
							var playerName = args[2];
							
							// Use the player's current target if no name is specified
							if (playerName == "")
								target = client.Player.TargetObject as GamePlayer;
							// Otherwise use the player's name in the command
							else
							{
								var myClient = WorldMgr.GetClientByPlayerName(playerName, true, true);
								
								// If they're offline, then check the DB for entries
								if (myClient == null)
								{
									// Patch 1.84: look for offline players
									target = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(playerName));
								}
								else
									target = myClient.Player;
							}
							
							// Check to make sure player even exists
							if (target == null)
							{
								// Message: No player was found with that name.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPlayerFound", null);
								return;
							}

							// Set vars to change things to
							var guildID = "";
							var guildRank = 9;
							var charName = "";
							var player = target as GamePlayer;
							var character = target as DOLCharacters;
							
							// Associate vars with target values
							if (target is GamePlayer)
							{
								charName = player.Name;
								guildID = player.GuildID;
								if (player.GuildRank != null)
									guildRank = player.GuildRank.RankLevel;
							}
							else
							{
								charName = character.Name;
								guildID = character.GuildID;
								guildRank = (byte)character.GuildRank;
							}
							
							// Make sure the target is a member of the player's guild before removal
							if (guildID != client.Player.GuildID)
							{
								// Message: That player is not in your guild.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotInYourGuild", null);
								return;
							}

							foreach (GamePlayer plyon in client.Player.Guild.GetListOfOnlineMembers())
							{
								// Don't send the message to the person removing since they're already going to get a message
								if (plyon != client.Player)
									// Message: {0} has removed {1} from the guild.
									ChatUtil.SendTypeMessage((int)eMsg.Guild, plyon, "Scripts.Player.Guild.AccountRemoved", client.Player.Name, charName);
								else
									// Message: You have removed {0} from the guild.
									ChatUtil.SendTypeMessage((int)eMsg.Guild, plyon, "Scripts.Player.Guild.YouHaveRemovedPlayer", charName);
							}
							
							// Remove player from guild and inform them of change
							if (target is GamePlayer)
								client.Player.Guild.RemovePlayer(client.Player.Name, player);
							// Update values
							else
							{
								character.GuildID = "";
								character.GuildRank = (ushort)guildRank;
								GameServer.Database.SaveObject(character);
							}

							// Update accordingly
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
					#endregion Remove
					#region RemoveAccount
					// --------------------------------------------------------------------------------
					// REMOVE ACCOUNT (Patch 1.84)
					// '/gc removeaccount <playerName>'
					// Removes all characters associated with a specific account from the guild.
					// --------------------------------------------------------------------------------
					case "removeaccount":
						{
							// Player must be a member of an existing guild to remove players
							if (client.Player.Guild == null)
							{
								// Message: You must be a member of a guild to use any guild commands.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
								return;
							}

							// Player must have sufficient rank privileges in the guild to remove members
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Invite))
							{
								// Message: You do not have high sufficient privileges in your guild to use that command.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPrivileges", null);
								return;
							}

							// Make sure they specify the player name
							if (args.Length < 3)
							{
								// Message: '/gc removeaccount <playerName>' - Removes all characters associated with a specific account from the guild.
								ChatUtil.SendTypeMessage((int)eMsg.CmdSyntax, client, "Scripts.Player.Guild.Help.GuildRemAccount", null);
								return;
							}

							// Parse the player name
							string playerName = String.Join(" ", args, 2, args.Length - 2);
							
							// Find all characters for that account, online or offline, in the guild
							// Patch 1.84: look for offline players
							var chars = DOLDB<DOLCharacters>.SelectObjects(DB.Column("AccountName").IsEqualTo(playerName).And(DB.Column("GuildID").IsEqualTo(client.Player.GuildID)));
							
							// Stuff to do if we find any characters
							if (chars.Count > 0)
							{
								
								GameClient myClient = WorldMgr.GetClientByAccountName(playerName, false);
								string plys = "";
								bool isOnline = (myClient != null);
								
								// Actions to perform for each character found in the database
								foreach (DOLCharacters ch in chars)
								{
									plys += (plys != "" ? "," : "") + ch.Name;
									
									// Alert the player if they're online
									if (isOnline && ch.Name == myClient.Player.Name)
										client.Player.Guild.RemovePlayer(client.Player.Name, myClient.Player);
									else
									{
										// Change the database values for all other characters
										ch.GuildID = "";
										ch.GuildRank = 9;
										GameServer.Database.SaveObject(ch);
									}
								}

								// Notify each player in the guild
								foreach (GamePlayer plyon in client.Player.Guild.GetListOfOnlineMembers())
								{
									// Don't send the message to the person removing since they're already going to get a message
									if (plyon != client.Player)
										// Message: {0} has removed {1} from the guild.
										ChatUtil.SendTypeMessage((int)eMsg.Guild, plyon, "Scripts.Player.Guild.AccountRemoved", client.Player.Name, myClient.Player.Name);
									else
										// Message: You have removed {0} from the guild.
										ChatUtil.SendTypeMessage((int)eMsg.Guild, plyon, "Scripts.Player.Guild.YouHaveRemovedPlayer", myClient.Player.Name);
								}
							}
							else
								// Message: There are no players associated with this account in your guild.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPlayersInAcc", null);

							client.Player.Guild.UpdateGuildWindow();
						}
						break;
					#endregion RemoveAccount
					#region Info
					// --------------------------------------------------------------------------------
					// INFO
					// '/gc info'
					// Displays all information regarding your guild, such as realm points, bounty points, guild level, web page, and more.
					// --------------------------------------------------------------------------------
					case "info":
					{
						bool typed = args.Length != 3;

						// Player must be a member of an existing guild to view guild info
						if (client.Player.Guild == null)
						{
							if (!(args.Length == 3 && args[2] == "1"))
							{
								// Message: You must be a member of a guild to use any guild commands.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
							}
							return;
						}
							
						// Player must have sufficient rank privileges in the guild to view its information
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.View))
						{
							// Message: You do not have high sufficient privileges in your guild to use that command.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPrivileges", null);
							return;
						}

						// Show guild information
						if (typed)
						{
							/*
							 * Guild Info for Clan Cotswold:
							 * Realm Points: xxx Bouty Points: xxx Merit Points: xxx
							 * Guild Level: xx
							 * Dues: 0% Bank: 0 copper pieces
							 * Current Merit Bonus: None
							 * Banner available for purchase
							 * Webpage: xxx
							 * Contact Email:
							 * Message: motd
							 * Officer Message: xxx
							 * Alliance Message: xxx
							 * Claimed Keep: xxx
							 */
								
							// Message: Guild Info for {0}:
							ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoGuild", client.Player.Guild.Name);
							// Message: Realm Points: {0} | Bounty Points: {1} | Merit Points: {2}
							ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoRPBPMP", client.Player.Guild.RealmPoints, client.Player.Guild.BountyPoints, client.Player.Guild.MeritPoints);
							// Message: Guild Level: {0}
							ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoGuildLevel", client.Player.Guild.GuildLevel);

							// Parse out the copper into types of coin for easier comprehension by players
								
							// First figure out plat and remove from total
							var plat = Math.Round(client.Player.Guild.GetGuildBank() / 1000 / 100 / 100, 0);
							if (plat < 1)
								plat = 0;
							var platString = plat.ToString("0");
								
							// Now figure out gold and do the same
							var gold = Math.Round(client.Player.Guild.GetGuildBank() - (plat * 1000 * 100 * 100) / 100 / 100, 0);
							if (gold < 1)
								gold = 0;
							var goldString = gold.ToString("0");
								
							// Now it's silver's turn
							var silver = Math.Round(client.Player.Guild.GetGuildBank() - (plat * 1000 * 100 * 100) - (gold * 100 * 100) / 100, 0);
							if (silver < 1)
								silver = 0;
							var silverString = silver.ToString("0");
								
							// Finally copper
							var copper = Math.Round(client.Player.Guild.GetGuildBank() - (plat * 1000 * 100 * 100) - (gold * 100 * 100) - (silver * 100));
							if (copper < 1)
								copper = 0;
							var copperString = copper.ToString("0");

							// Message: Dues: {0} | Guild Bank: {1} platinum, {2} gold, {3} silver, {4} copper
							ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoGDuesBank", client.Player.Guild.GetGuildDuesPercent().ToString() + "%", platString, goldString, silverString, copperString);

							// Message: Guild Bonus: {0}
							ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoGBonusType", Guild.BonusTypeToName(client.Player.Guild.BonusType));

							// Guild banner stuff
							// Eclipse doesn't use this, so it's being commented out
							// if (client.Player.Guild.GuildBanner)
							// {
							// Message: Guild Banner Status: {0}
							// 	ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoGBannerStatus", client.Player.Guild.GuildBannerStatus(client.Player));
							// }
							// else if (client.Player.Guild.GuildLevel >= 7)
							// {
							// 	TimeSpan lostTime = DateTime.Now.Subtract(client.Player.Guild.GuildBannerLostTime);

							// 	if (lostTime.TotalMinutes < Properties.GUILD_BANNER_LOST_TIME)
							// 	{
							// Message: Guild Banner Status: Your banner has been lost to the enemy
							// 		ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoGBannerLostEnemy", null);
							// 	}
							// 	else
							// 	{
							// Message: Guild Banner Status: Banner available for purchase
							// 		ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoGBannerAvailable", null);
							// 	}
							// }

							// Message: Website: {0}
							ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoWebpage", client.Player.Guild.Webpage);
							// Message: Contact Email: {0}
							ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoCEmail", client.Player.Guild.Email);

							// Make sure there is a message and the player has access to the guild channel (for some reason)
							if (!Util.IsEmpty(client.Player.Guild.Motd) && client.Player.GuildRank.GcHear)
							{
								// Message: Guild Message: {0}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoMotd", client.Player.Guild.Motd);
							}

							// Make sure there is a message and the player has access to the guild officer channel
							if (!Util.IsEmpty(client.Player.Guild.Omotd) && client.Player.GuildRank.OcHear)
							{
								// Message: Officer Message: {0}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoOMotd", client.Player.Guild.Omotd);
							}

							// Make sure there is a message and the player has access to the alliance chat channel
							if (client.Player.Guild.alliance != null && client.Player.GuildRank.AcHear && !Util.IsEmpty(client.Player.Guild.alliance.Dballiance.Motd))
							{
								// Message: Alliance Message: {0}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.InfoaMotd", client.Player.Guild.alliance.Dballiance.Motd);
							}
								
							// If the guild has a claimed keep, then mention it
							if (client.Player.Guild.ClaimedKeeps.Count > 0)
							{
								foreach (AbstractGameKeep keep in client.Player.Guild.ClaimedKeeps)
								{
									// Message: Keeps Claimed: Your guild has currently claimed {0}.
									ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.Keep", keep.Name);
								}
							}

							// Show guild house number if they've got one
							if (client.Player.Guild.GuildOwnsHouse)
							{
								// Message: Guild House #: {0}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildHouse", client.Player.Guild.GuildHouseNumber);
							}
							else
							{
								// Message: Guild House #: {0}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildHouse", "None");
							}
						}
						else
						{
							switch (args[2])
							{
								case "1": // show guild info
								{
									if (client.Player.Guild == null)
										return;

									int housenum;
									if (client.Player.Guild.GuildOwnsHouse)
									{
										housenum = client.Player.Guild.GuildHouseNumber;
									}
									else
										housenum = 0;

									string mes = "I";
									mes += ',' + client.Player.Guild.GuildLevel.ToString(); // Guild Level
									mes += ',' + client.Player.Guild.GetGuildBank().ToString(); // Guild Bank money
									mes += ',' + client.Player.Guild.GetGuildDuesPercent().ToString(); // Guild Dues enable/disable
									mes += ',' + client.Player.Guild.BountyPoints.ToString(); // Guild Bounty
									mes += ',' + client.Player.Guild.RealmPoints.ToString(); // Guild Experience
									mes += ',' + client.Player.Guild.MeritPoints.ToString(); // Guild Merit Points
									mes += ',' + housenum.ToString(); // Guild houseLot ?
									mes += ',' + (client.Player.Guild.MemberOnlineCount + 1).ToString(); // online Guild member ?
									// mes += ',' + client.Player.Guild.GuildBannerStatus(client.Player); //"Banner available for purchase", "Missing banner buying permissions"
									mes += ",\"" + client.Player.Guild.Motd + '\"'; // Guild Motd
									mes += ",\"" + client.Player.Guild.Omotd + '\"'; // Guild oMotd
											
									client.Out.SendMessage(mes, eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
									break;
								}
								case "2": //enable/disable social windows
								{
									// "P,ShowGuildWindow,ShowAllianceWindow,?,ShowLFGuildWindow(only with guild),0,0" // news and friend windows always showed
									client.Out.SendMessage("P," + (client.Player.Guild == null ? "0" : "1") + (client.Player.Guild.AllianceId != string.Empty ? "0" : "1") + ",0,0,0,0", eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
									break;
								}
								default:
									break;
							}
						}

						SendSocialWindowData(client, 1, 1, 2);
						break;
					}
					#endregion Info
					#region BuyBanner (Disabled)
					// --------------------------------------------------------------------------------
					// BUYBANNER
					// '/gc buybanner'
					// Spend points to buy a guild banner.
					// --------------------------------------------------------------------------------
					/* Disabled because it is not used for Eclipse
					case "buybanner":
						{
							if (client.Player.Guild.GuildLevel < 7)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.GuildLevelReq"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							long bannerPrice = (client.Player.Guild.GuildLevel * 100);

							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.GuildBanner)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerAlready"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							TimeSpan lostTime = DateTime.Now.Subtract(client.Player.Guild.GuildBannerLostTime);

							if (lostTime.TotalMinutes < Properties.GUILD_BANNER_LOST_TIME)
							{
								int hoursLeft = (int)((Properties.GUILD_BANNER_LOST_TIME - lostTime.TotalMinutes + 30) / 60);
								if (hoursLeft < 2)
								{
									int minutesLeft = (int)(Properties.GUILD_BANNER_LOST_TIME - lostTime.TotalMinutes + 1);
									client.Out.SendMessage("Your guild banner was lost to the enemy. You must wait " + minutesLeft + " minutes before you can purchase another one.", eChatType.CT_Guild, eChatLoc.CL_ChatWindow);
								}
								else
								{
									client.Out.SendMessage("Your guild banner was lost to the enemy. You must wait " + hoursLeft + " hours before you can purchase another one.", eChatType.CT_Guild, eChatLoc.CL_ChatWindow);
								}
								return;
							}


							client.Player.Guild.UpdateGuildWindow();

							if (client.Player.Guild.BountyPoints > bannerPrice || client.Account.PrivLevel > (int)ePrivLevel.Player)
							{
								client.Out.SendCustomDialog("Are you sure you buy a guild banner for " + bannerPrice + " guild bounty points? ", ConfirmBannerBuy);
								client.Player.TempProperties.setProperty(GUILD_BANNER_PRICE, bannerPrice);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNotAfford"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							break;
						}
					*/
					#endregion BuyBanner (Disabled)
					#region Summon (Disabled)
					// --------------------------------------------------------------------------------
					// SUMMON
					// '/gc summon'
					// Summons the guild banner if you have one
					// --------------------------------------------------------------------------------
					/* Disabled because it is not used for Eclipse
					case "summon":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.GuildBanner)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNone"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Group == null && client.Account.PrivLevel == (int)ePrivLevel.Player)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNoGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							foreach (GamePlayer guildPlayer in client.Player.Guild.GetListOfOnlineMembers())
							{
								if (guildPlayer.GuildBanner != null)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerGuildSummoned"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
									return;
								}
							}

							if (client.Player.Group != null)
							{
								foreach (GamePlayer groupPlayer in client.Player.Group.GetPlayersInTheGroup())
								{
									if (groupPlayer.GuildBanner != null)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerGroupSummoned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
										return;
									}
								}
							}

							if (client.Player.CurrentRegion.IsRvR)
							{
								GuildBanner banner = new GuildBanner(client.Player);
								banner.Start();
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerSummoned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								client.Player.Guild.SendMessageToGuildMembers(string.Format("{0} has summoned the guild banner!", client.Player.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								client.Player.Guild.UpdateGuildWindow();
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNotRvR"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							}
							break;
						}
					*/
					#endregion Summon (Disabled)
					#region Buff
					// --------------------------------------------------------------------------------
					// BUFF
					// '/gc buff < crafting | rps | bps | xp >'
					// Activates a guild-wide buff for the desired 5% bonus type. Each activation lasts 24 hours and costs 1000 Merit Points.
					// --------------------------------------------------------------------------------
					case "buff":
						{
							// Player must be a member of an existing guild to view guild info
							if (client.Player.Guild == null)
							{
								// Message: You must be a member of a guild to use any guild commands.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
								return;
							}

							// Player must have sufficient rank privileges in the guild to view its information
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Buff))
							{
								// Message: You do not have high sufficient privileges in your guild to use that command.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPrivileges", null);
								return;
							}

							// Guild must have merit points available to purchase the buff
							if (client.Player.Guild.MeritPoints < 1000)
							{
								// Message: Your guild does not have the sufficient merit points to activate this buff.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.MeritPointReq", null);
								return;
							}

							// Present the player with a dialog to activate the buff
							if (client.Player.Guild.BonusType == Guild.eBonusType.None && args.Length > 2)
							{
								// Set some vars to make things centralized
								var meritCost = "1000";
								var buffType = " ";
								var dialog = "Are you sure you want to activate a guild " + buffType + " buff for" + meritCost + " merit points?";

								// Set the default prop type for buffs
								var buffProp = Guild.eBonusType.None;

								// Switch between buff types
								switch (args[2])
								{
									case "rps":
									{
										buffType = "Realm Point";
										buffProp = Guild.eBonusType.RealmPoints;

										// Only activate if the server property is set above 0
										if (Properties.GUILD_BUFF_RP > 0)
										{
											client.Out.SendCustomDialog(dialog, ConfirmBuffBuy);
											
											// Apply the bonus to everyone online
											// TODO: Apply it to offline members as well
											foreach (GamePlayer guildMember in client.Player.Guild.GetListOfOnlineMembers())
											{
												// Message: {0} has activated the {1} guild bonus.
												ChatUtil.SendTypeMessage((int)eMsg.Error, guildMember, "Scripts.Player.Guild.BuffActivatedBy", client.Player.Name, buffType);
												guildMember.TempProperties.setProperty(GUILD_BUFF_TYPE, buffProp);
												guildMember.Guild.UpdateGuildWindow();
											}
										}
										else
										{
											// Message: This buff type is not available.
											ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.BuffTypeNotAvail", null);
										}
										return;
									}
										break;
									case "bps":
									{
										buffType = "Bounty Point";
										buffProp = Guild.eBonusType.BountyPoints;

										// Only activate if the server property is set above 0
										if (Properties.GUILD_BUFF_BP > 0)
										{
											client.Out.SendCustomDialog(dialog, ConfirmBuffBuy);
											
											// Apply the bonus to everyone online
											// TODO: Apply it to offline members as well
											foreach (GamePlayer guildMember in client.Player.Guild.GetListOfOnlineMembers())
											{
												// Message: {0} has activated the {1} guild bonus.
												ChatUtil.SendTypeMessage((int)eMsg.Error, guildMember, "Scripts.Player.Guild.BuffActivatedBy", client.Player.Name, buffType);
												guildMember.TempProperties.setProperty(GUILD_BUFF_TYPE, buffProp);
												guildMember.Guild.UpdateGuildWindow();
											}
										}
										else
										{
											// Message: This buff type is not available.
											ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.BuffTypeNotAvail", null);
										}
									}
										break;
									case "xp":
									{
										buffType = "Experience";
										buffProp = Guild.eBonusType.Experience;

										// Only activate if the server property is set above 0
										if (Properties.GUILD_BUFF_XP > 0)
										{
											client.Out.SendCustomDialog(dialog, ConfirmBuffBuy);
											
											// Apply the bonus to everyone online
											// TODO: Apply it to offline members as well
											foreach (GamePlayer guildMember in client.Player.Guild.GetListOfOnlineMembers())
											{
												// Message: {0} has activated the {1} guild bonus.
												ChatUtil.SendTypeMessage((int)eMsg.Error, guildMember, "Scripts.Player.Guild.BuffActivatedBy", client.Player.Name, buffType);
												guildMember.TempProperties.setProperty(GUILD_BUFF_TYPE, buffProp);
												guildMember.Guild.UpdateGuildWindow();
											}
										}
										else
										{
											// Message: This buff type is not available.
											ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.BuffTypeNotAvail", null);
										}
									}
										break;
									case "crafting":
									{
										buffType = "Crafting";
										buffProp = Guild.eBonusType.CraftingHaste;

										// Only activate if the server property is set above 0
										if (Properties.GUILD_BUFF_CRAFTING > 0)
										{
											client.Out.SendCustomDialog(dialog, ConfirmBuffBuy);
											
											// Apply the bonus to everyone online
											// TODO: Apply it to offline members as well
											foreach (GamePlayer guildMember in client.Player.Guild.GetListOfOnlineMembers())
											{
												// Message: {0} has activated the {1} guild bonus.
												ChatUtil.SendTypeMessage((int)eMsg.Error, guildMember, "Scripts.Player.Guild.BuffActivatedBy", client.Player.Name, buffType);
												guildMember.TempProperties.setProperty(GUILD_BUFF_TYPE, buffProp);
												guildMember.Guild.UpdateGuildWindow();
											}
										}
										else
										{
											// Message: This buff type is not available.
											ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.BuffTypeNotAvail", null);
										}
									}
										break;
									case "":
									{
										// Message: '/gc buff < bps | crafting | rps | xp >' - Activates a guild-wide buff for the desired 5% bonus type. Each activation lasts 24 hours and costs 1000 Merit Points.
										ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.Help.GuildBuff", null);
									}
										break;
								}
							}
							else
							{
								if (client.Player.Guild.BonusType == Guild.eBonusType.None)
								{
									// Message: '/gc buff < bps | crafting | rps | xp >' - Activates a guild-wide buff for the desired 5% bonus type. Each activation lasts 24 hours and costs 1000 Merit Points.
									ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.Help.GuildBuff", null);
								}
								else
								{
									var buffType = " ";

									// If a buff is already active, then throw a message saying as much
									switch (client.Player.Guild.BonusType)
									{
										case Guild.eBonusType.Experience:
										{
											buffType = "n experience";

											// Message: Your guild already has a{0} buff active.
											ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffActive", buffType);
										}
											break;
										case Guild.eBonusType.RealmPoints:
										{
											buffType = " realm point";

											// Message: Your guild already has a{0} buff active.
											ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffActive", buffType);
										}
											break;
										case Guild.eBonusType.BountyPoints:
										{
											buffType = " bounty point";

											// Message: Your guild already has a{0} buff active.
											ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffActive", buffType);
										}
											break;
										case Guild.eBonusType.CraftingHaste:
										{
											buffType = " crafting";

											// Message: Your guild already has a{0} buff active.
											ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffActive", buffType);
										}
											break;
									}
									//client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.ActiveBuff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								}
							}

							// If no buff is active, show which buffs the player may activate
							if (client.Player.Guild.BonusType == Guild.eBonusType.None)
								// Message: Available guild buffs:
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffTypesAvail", null);

							// Bounty point buff
							if (ServerProperties.Properties.GUILD_BUFF_BP > 0 && client.Player.Guild.BonusType == Guild.eBonusType.None)
								// Message: {0}: {1}%
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffPercents", Guild.BonusTypeToName(Guild.eBonusType.BountyPoints), ServerProperties.Properties.GUILD_BUFF_BP);

							// Crafting buff
							if (ServerProperties.Properties.GUILD_BUFF_CRAFTING > 0 && client.Player.Guild.BonusType == Guild.eBonusType.None)
								// Message: {0}: {1}%
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffPercents", Guild.BonusTypeToName(Guild.eBonusType.CraftingHaste), ServerProperties.Properties.GUILD_BUFF_CRAFTING);

							// Realm point buff
							if (ServerProperties.Properties.GUILD_BUFF_RP > 0 && client.Player.Guild.BonusType == Guild.eBonusType.None)
								// Message: {0}: {1}%
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffPercents", Guild.BonusTypeToName(Guild.eBonusType.RealmPoints), ServerProperties.Properties.GUILD_BUFF_RP);

							// XP buff
							if (ServerProperties.Properties.GUILD_BUFF_XP > 0 && client.Player.Guild.BonusType == Guild.eBonusType.None)
								// Message: {0}: {1}%
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.GuildBuffPercents", Guild.BonusTypeToName(Guild.eBonusType.Experience), ServerProperties.Properties.GUILD_BUFF_XP);

							//if (ServerProperties.Properties.GUILD_BUFF_ARTIFACT_XP > 0)
							//	client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.ArtifactXP), ServerProperties.Properties.GUILD_BUFF_ARTIFACT_XP), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

							//if (ServerProperties.Properties.GUILD_BUFF_MASTERLEVEL_XP > 0)
							//    client.Out.SendMessage(string.Format("{0}: {1}%", Guild.BonusTypeToName(Guild.eBonusType.MasterLevelXP), ServerProperties.Properties.GUILD_BUFF_MASTERLEVEL_XP), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

							return;
						}
					#endregion Buff
					#region Unsummon (Disabled)
					// --------------------------------------------------------------------------------
					// SUMMON
					// '/gc unsummon'
					// Removes the guild banner if you have one active
					// --------------------------------------------------------------------------------
					/* Disabled because it is not used for Eclipse
					case "unsummon":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.GuildBanner)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNone"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Group == null && client.Account.PrivLevel == (int)ePrivLevel.Player)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerNoGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.InCombat)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InCombat"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							foreach (GamePlayer player in client.Player.Guild.GetListOfOnlineMembers())
							{
								if (client.Player.Name == player.Name && player.GuildBanner != null && player.GuildBanner.BannerItem.Status == GuildBannerItem.eStatus.Active)
								{
									client.Player.GuildBanner.Stop();
									client.Player.GuildBanner = null;
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.BannerUnsummoned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
									client.Player.Guild.SendMessageToGuildMembers(string.Format("{0} has put away the guild banner!", client.Player.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
									client.Player.Guild.UpdateGuildWindow();
									break;
								}

								client.Out.SendMessage("You aren't carrying a banner!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							break;
						} 
					*/
					#endregion Unsummon (Disabled)
					#region Ranks
					// --------------------------------------------------------------------------------
					// RANKS
					// '/gc ranks'
					// Displays all rank settings and information for the guild.
					// --------------------------------------------------------------------------------
					case "ranks":
						{
							// Obviously need to be a member of a guild to use this command
							if (client.Player.Guild == null)
							{
								// Message: You must be a member of a guild to use any guild commands.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
								return;
							}
							
							client.Player.Guild.UpdateGuildWindow();
							
							// Make sure they've got sufficient privileges
							if (!client.Player.GuildRank.GcHear)
							{
								// Message: You do not have sufficient privileges in your guild to use that command.
								ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPrivileges", null);
								return;
							}
							
							List<DBRank> rankList = client.Player.Guild.Ranks.ToList();
							
							// List the following info for each rank
							foreach (DBRank rank in rankList.OrderBy(rank => rank.RankLevel))
							{
								// Message: Rank: {0} | Name: {1}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.RanksRankName", rank.RankLevel.ToString(), rank.Title);
								// Message: Alliance Channel Hear: {0} | Alliance Channel Speak: {1}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.RankAlliancePriv", (rank.AcHear ? "Yes" : "No"), (rank.AcSpeak ? "Yes" : "No"));
								// Message: Officer Channel Hear: {0} | Officer Channel Speak: {1}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.RankOfficerPriv", (rank.OcHear ? "Yes" : "No"), (rank.OcSpeak ? "Yes" : "No"));
								// Message: Guild Channel Hear: {0} | Guild Channel Speak: {1}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.RankGuildPriv", (rank.GcHear ? "Yes" : "No"), (rank.GcSpeak ? "Yes" : "No"));
								// Message: Wear Emblems: {0} | Promote Members: {1}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.RankEmbPromPriv", (rank.Emblem ? "Yes" : "No"), (rank.Promote ? "Yes" : "No"));
								// Message: Remove Members: {0} | View Ranks: {1}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.RankRemoveViewPriv", (rank.Remove ? "Yes" : "No"), (rank.View ? "Yes" : "No"));
								// Message: Set Dues: {0} | Withdraw from Guild Bank: {1}
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.RankDuesWithPriv", (rank.Dues ? "Yes" : "No"), (rank.Withdraw ? "Yes" : "No"));
							}

							client.Player.Guild.UpdateGuildWindow();
							break;
						}
					#endregion Ranks
					#region Webpage
					// --------------------------------------------------------------------------------
					// WEBPAGE
					// '/gc webpage <webURL>'
					// Displays a website address, typically one associated with a guild website. This
					// is shown on the Herald, as well as to members that use the '/gc info' command.
					// --------------------------------------------------------------------------------
					case "webpage":
					{
						// Obviously need to be a member of a guild to use this command
						if (client.Player.Guild == null)
						{
							// Message: You must be a member of a guild to use any guild commands.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
							return;
						}
							
						client.Player.Guild.UpdateGuildWindow();
							
						// You need to be Guildmaster to use this command
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							// Message: You do not have sufficient privileges in your guild to use that command.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPrivileges", null);
						}
							
						message = String.Join(" ", args, 2, args.Length - 2);
						client.Player.Guild.Webpage = message;
							
						// Message: You have set the guild webpage to {0}.
						ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.WebpageSet", client.Player.Guild.Webpage);
						break;
					}
					#endregion Webpage
					#region Email
					// --------------------------------------------------------------------------------
					// EMAIL
					// '/gc email <emailAddress>'
					// Sets a contact email address guild members can use to contact the guild if needed.
					// Format the value as 'address@email.com'. This value is displayed on the Herald,
					// as well as when guild members use the '/gc info' command.
					// --------------------------------------------------------------------------------
					case "email":
					{
						// Obviously need to be a member of a guild to use this command
						if (client.Player.Guild == null)
						{
							// Message: You must be a member of a guild to use any guild commands.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
							return;
						}
							
						client.Player.Guild.UpdateGuildWindow();
							
						// You need to be Guildmaster to use this command
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							// Message: You do not have sufficient privileges in your guild to use that command.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NoPrivileges", null);
						}
							
						message = String.Join(" ", args, 2, args.Length - 2);
						client.Player.Guild.Email = message;
							
						// Message: You have set the guild email to {0}.
						ChatUtil.SendTypeMessage((int)eMsg.Guild, client, "Scripts.Player.Guild.EmailSet", client.Player.Guild.Email);
							
						client.Player.Guild.UpdateGuildWindow();
						break;
					}
					#endregion Email
					#region List
					// --------------------------------------------------------------------------------
					// LIST
					// '/gc list'
					// Displays a list of all guilds in your realm with members online.
					// --------------------------------------------------------------------------------
					case "list":
					{
						// Changing this to list online only, not sure if this is live like or not but list can be huge
						// and spam client.  - Tolakram
						List<Guild> guildList = GuildMgr.GetAllGuilds();
						foreach (Guild guild in guildList)
						{
							if (guild.MemberOnlineCount > 0)
							{
								string mesg = guild.Name + " " + guild.MemberOnlineCount + " members ";
								ChatUtil.SendTypeMessage((int)eMsg.Guild, client, mesg, null);
							}
						}
						client.Player.Guild.UpdateGuildWindow();
					}
						break;
					#endregion List
					#region Edit
					// --------------------------------------------------------------------------------
					// EDIT
					// '/gc edit'
					// Lists all available guild parameters that may be altered.
					// --------------------------------------------------------------------------------
					case "edit":
					{
						// Obviously need to be a member of a guild to use this command
						if (client.Player.Guild == null)
						{
							// Message: You must be a member of a guild to use any guild commands.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "Scripts.Player.Guild.NotMember", null);
							return;
						}
							
						client.Player.Guild.UpdateGuildWindow();
						GCEditCommand(client, args);
					}
						client.Player.Guild.UpdateGuildWindow();
						break;
					#endregion Edit
						#region Form
						// --------------------------------------------------------------------------------
						// FORM
						// --------------------------------------------------------------------------------
					case "form":
						{
							Group group = client.Player.Group;
							if (args.Length < 3)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildForm"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#region Near Registrar
							if (!IsNearRegistrar(client.Player))
							{
								client.Out.SendMessage("You must be near a guild registrar to use this command!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region No group Check
							if (group == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.FormNoGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Groupleader Check
							if (group != null && client.Player != client.Player.Group.Leader)
							{
								client.Out.SendMessage("Only the group leader can create a guild", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Enough members to form Check
							if (group.MemberCount < Properties.GUILD_NUM)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.FormNoMembers" + Properties.GUILD_NUM), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Player already in guild check and Cross Realm Check

							foreach (GamePlayer ply in group.GetPlayersInTheGroup())
							{
								if (ply.Guild != null)
								{
									client.Player.Group.SendMessageToGroupMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AlreadyInGuildName", ply.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
								if (ply.Realm != client.Player.Realm && ServerProperties.Properties.ALLOW_CROSS_REALM_GUILDS == false)
								{
									client.Out.SendMessage("All group members must be of the same realm in order to create a guild.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
							}
							#endregion
							#region Guild Length Naming Checks
							//Check length of guild name.
							string guildname = String.Join(" ", args, 2, args.Length - 2);
							if (guildname.Length > 30)
							{
								client.Out.SendMessage("Sorry, your guild name is too long.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Valid Characters Check
							if (!IsValidGuildName(guildname))
							{
								// Mannen doesn't know the live server message, so someone needs to enter it . ;-)
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.InvalidLetters"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Guild Exist Checks
							if (GuildMgr.DoesGuildExist(guildname))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.GuildExists"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#endregion
							#region Enoguh money to form Check
							if (client.Player.Group.Leader.GetCurrentMoney() < GuildFormCost)
							{
								client.Out.SendMessage("It cost 1 gold piece to create a guild", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							#endregion


							client.Player.Group.Leader.TempProperties.setProperty("Guild_Name", guildname);
							if (GuildFormCheck(client.Player))
							{
								client.Player.Group.Leader.TempProperties.setProperty("Guild_Consider", true);
								foreach (GamePlayer p in group.GetPlayersInTheGroup().Where(p => p != @group.Leader))
								{
									p.Out.SendCustomDialog(string.Format("Do you wish to create the guild {0} with {1} as Guild Leader", guildname, client.Player.Name), new CustomDialogResponse(CreateGuild));
								}
							}
						}
						break;
					#endregion
						#region Quit
						// --------------------------------------------------------------------------------
						// QUIT
						// --------------------------------------------------------------------------------
					case "quit":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							client.Out.SendGuildLeaveCommand(client.Player, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.ConfirmLeave", client.Player.Guild.Name));
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Promote
						// --------------------------------------------------------------------------------
						// PROMOTE
						// /gc promote [name] <rank#>' to promote player to a superior rank
						// --------------------------------------------------------------------------------
					case "promote":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Promote))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							if (args.Length < 3)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildPromote"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							object obj = null;
							string playerName = string.Empty;
							bool useDB = false;

							if (args.Length >= 4)
							{
								playerName = args[2];
							}

							if (playerName == string.Empty)
							{
								obj = client.Player.TargetObject as GamePlayer;
							}
							else
							{
								GameClient onlineClient = WorldMgr.GetClientByPlayerName(playerName, true, false);
								if (onlineClient == null)
								{
									// Patch 1.84: look for offline players
									obj = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(playerName));
									useDB = true;
								}
								else
								{
									obj = onlineClient.Player;
								}
							}

							if (obj == null)
							{
								if (useDB)
								{
									client.Out.SendMessage("No player with that name can be found!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								}
								else if (playerName == string.Empty)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								}
								else
								{
									client.Out.SendMessage("You need to target a player or provide a player name!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildPromote"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								}
								return;
							}
							//First Check Routines, GuildIDControl search for player or character.
							string guildId = "";
							string plyName = "";
							ushort currentTargetGuildRank = 9;
							GamePlayer ply = obj as GamePlayer;
							DOLCharacters ch = obj as DOLCharacters;

							if (ply != null)
							{
								plyName = ply.Name;
								guildId = ply.GuildID;
								currentTargetGuildRank = ply.GuildRank.RankLevel;
							}
							else if (ch != null)
							{
								plyName = ch.Name;
								guildId = ch.GuildID;
								currentTargetGuildRank = ch.GuildRank;
							}
							else
							{
								client.Out.SendMessage("Error during promotion, player not found!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							if (guildId != client.Player.GuildID)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotInYourGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							//Second Check, Autorisation Checks, a player can promote another to it's own RealmRank or above only if: newrank(rank to be applied) >= commandUserGuildRank(usercommandRealmRank)

							ushort commandUserGuildRank = client.Player.GuildRank.RankLevel;
							ushort newrank;
							try
							{
								newrank = Convert.ToUInt16(args[3]);

								if (newrank > 9)
								{
									client.Out.SendMessage("Error changing to new rank! Realm Rank have to be set to 0-9.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
							}
							catch
							{
								client.Out.SendMessage("Error changing to new rank!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildPromote"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								return;
							}
							//if (commandUserGuildRank != 0 && (newrank < commandUserGuildRank || newrank < 0)) // Do we have to authorize Self Retrograde for GuildMaster?
							if ((newrank < commandUserGuildRank) || (newrank < 0))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromoteHigherThanPlayer"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (newrank > currentTargetGuildRank && commandUserGuildRank != 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromoteHaveToUseDemote"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (obj is GamePlayer)
							{
								ply.GuildRank = client.Player.Guild.GetRankByID(newrank);
								ply.SaveIntoDatabase();
								currentTargetGuildRank = ply.GuildRank.RankLevel;
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromotedSelf", plyName, newrank.ToString(), currentTargetGuildRank.ToString()), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
								client.Player.Guild.SendMessageToGuildMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromotedOther", client.Player.Name, plyName, newrank.ToString(), currentTargetGuildRank.ToString()), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							}
							else
							{
								ch.GuildRank = newrank;
								GameServer.Database.SaveObject(ch);
								GameServer.Database.FillObjectRelations(ch);
								currentTargetGuildRank = ch.GuildRank;
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromotedSelf", plyName, newrank.ToString(), currentTargetGuildRank.ToString()), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
								client.Player.Guild.SendMessageToGuildMembers(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PromotedOther", client.Player.Name, plyName, newrank.ToString(), currentTargetGuildRank.ToString()), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
					#endregion
						#region Demote
						// --------------------------------------------------------------------------------
						// DEMOTE
						// --------------------------------------------------------------------------------
						case "demote":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.NotMember"), eChatType.CT_System,
									eChatLoc.CL_SystemWindow);
								return;
							}

							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Demote))
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System,
									eChatLoc.CL_SystemWindow);
								return;
							}

							if (args.Length < 3)
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.Help.GuildDemote"), eChatType.CT_System,
									eChatLoc.CL_SystemWindow);
								return;
							}


							object obj = null;
							string playername = string.Empty;
							bool useDB = false;
							
							if (args.Length >= 4)
							{
								playername = args[2];
							}

							if (playername == string.Empty)
							{
								obj = client.Player.TargetObject as GamePlayer;
							}
							else
							{
								GameClient myclient = WorldMgr.GetClientByPlayerName(playername, true, false);
								if (myclient == null)
								{
									// Patch 1.84: look for offline players
									obj = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(playername));
									useDB = true;
								}
								else
								{
									obj = myclient.Player;
								}
							}
							if (obj == null)
							{
								if (useDB)
								{
									client.Out.SendMessage("No player with that name can be found!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								}
								else if (playername == string.Empty)
								{
									client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.NoPlayerSelected"), eChatType.CT_System,
									eChatLoc.CL_SystemWindow);
								}
								else
								{
									client.Out.SendMessage("You need to target a player or provide a player name!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDemote"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								}
								return;
							}

							string guildId = "";
							ushort guildRank = 1;
							string plyName = "";
							GamePlayer ply = obj as GamePlayer;
							DOLCharacters ch = obj as DOLCharacters;
							if (obj is GamePlayer)
							{
								plyName = ply.Name;
								guildId = ply.GuildID;
								if (ply.GuildRank != null)
									guildRank = ply.GuildRank.RankLevel;
							}
							else
							{
								plyName = ch.Name;
								guildId = ch.GuildID;
								guildRank = ch.GuildRank;
							}

							if (guildId != client.Player.GuildID)
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.NotInYourGuild"), eChatType.CT_System,
									eChatLoc.CL_SystemWindow);
								return;
							}

							try
							{
								ushort newrank = Convert.ToUInt16(args[3]);
								if (newrank < guildRank || newrank > 10)
								{
									client.Out.SendMessage(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedHigherThanPlayer"), eChatType.CT_System,
										eChatLoc.CL_SystemWindow);
									return;
								}

								if (obj is GamePlayer)
								{
									ply.GuildRank = client.Player.Guild.GetRankByID(newrank);
									ply.SaveIntoDatabase();
									guildRank = ply.GuildRank.RankLevel;
									client.Out.SendMessage(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedSelf", plyName, newrank.ToString(),
											guildRank.ToString()), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
									client.Player.Guild.SendMessageToGuildMembers(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedOther", client.Player.Name, plyName,
											newrank.ToString(), guildRank.ToString()), eChatType.CT_Important,
										eChatLoc.CL_SystemWindow);
								}
								else
								{
									ch.GuildRank = newrank;
									GameServer.Database.SaveObject(ch);
									guildRank = ch.GuildRank;
									client.Out.SendMessage(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedSelf", plyName, newrank.ToString(),
											guildRank.ToString()), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
									client.Player.Guild.SendMessageToGuildMembers(
										LanguageMgr.GetTranslation(client.Account.Language,
											"Scripts.Player.Guild.DemotedOther", client.Player.Name, plyName,
											newrank.ToString(), guildRank.ToString()), eChatType.CT_Important,
										eChatLoc.CL_SystemWindow);
								}
							}
							catch
							{
								client.Out.SendMessage(
									LanguageMgr.GetTranslation(client.Account.Language,
										"Scripts.Player.Guild.InvalidRank"), eChatType.CT_System,
									eChatLoc.CL_SystemWindow);
							}

							client.Player.Guild.UpdateGuildWindow();
				}
						break;
						#endregion
						#region Who
						// --------------------------------------------------------------------------------
						// WHO
						// --------------------------------------------------------------------------------
					case "who":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							int ind = 0;
							int startInd = 0;

							#region Social Window
							if (args.Length == 6 && args[2] == "window")
							{
								int sortTemp;
								byte showTemp;
								int page;

								//Lets get the variables that were sent over
								if (Int32.TryParse(args[3], out sortTemp) && Int32.TryParse(args[4], out page) && Byte.TryParse(args[5], out showTemp) && sortTemp >= -7 && sortTemp <= 7)
								{
									SendSocialWindowData(client, sortTemp, page, showTemp);
								}
								return;
							}
							#endregion

							#region Alliance Who
							else if (args.Length == 3)
							{
								if (args[2] == "alliance" || args[2] == "a")
								{
									foreach (Guild guild in client.Player.Guild.alliance.Guilds)
									{
										lock (guild.GetListOfOnlineMembers())
										{
											foreach (GamePlayer ply in guild.GetListOfOnlineMembers())
											{
												if (ply.Client.IsPlaying && !ply.IsAnonymous)
												{
													ind++;
													string zoneName = (ply.CurrentZone == null ? "(null)" : ply.CurrentZone.Description);
													string mesg = ind + ") " + ply.Name + " <guild=" + guild.Name + "> the Level " + ply.Level + " " + ply.CharacterClass.Name + " in " + zoneName;
													client.Out.SendMessage(mesg, eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
												}
											}
										}
									}
									return;
								}
								else
								{
									int.TryParse(args[2], out startInd);
								}
							}
							#endregion

							#region Who
							IList<GamePlayer> onlineGuildMembers = client.Player.Guild.GetListOfOnlineMembers();

							foreach (GamePlayer ply in onlineGuildMembers)
							{
								if (ply.Client.IsPlaying && !ply.IsAnonymous)
								{
									if (startInd + ind > startInd + WhoCommandHandler.MAX_LIST_SIZE)
										break;
									ind++;
									string zoneName = (ply.CurrentZone == null ? "(null)" : ply.CurrentZone.Description);
									string mesg;
									if (ply.GuildRank.Title != null)
										mesg = ind.ToString() + ") " + ply.Name + " <" + ply.GuildRank.Title + "> the Level " + ply.Level.ToString() + " " + ply.CharacterClass.Name + " in " + zoneName;
									else
										mesg = ind.ToString() + ") " + ply.Name + " <" + ply.GuildRank.RankLevel.ToString() + "> the Level " + ply.Level.ToString() + " " + ply.CharacterClass.Name + " in " + zoneName;
									if (ServerProperties.Properties.ALLOW_CHANGE_LANGUAGE)
										mesg += " <" + ply.Client.Account.Language + ">";
									if (ind >= startInd)
										client.Out.SendMessage(mesg, eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								}
							}
							if (ind > WhoCommandHandler.MAX_LIST_SIZE && ind < onlineGuildMembers.Count)
								client.Out.SendMessage(string.Format(WhoCommandHandler.MESSAGE_LIST_TRUNCATED, onlineGuildMembers.Count), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							else client.Out.SendMessage("total member online:        " + ind.ToString(), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);

							break;
							#endregion
						}
						#endregion
						#region Leader
						// --------------------------------------------------------------------------------
						// LEADER
						// --------------------------------------------------------------------------------
					case "leader":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer newLeader = client.Player.TargetObject as GamePlayer;
							if (args.Length > 2)
							{
								GameClient temp = WorldMgr.GetClientByPlayerName(args[2], true, false);
								if (temp != null && GameServer.ServerRules.IsAllowedToGroup(client.Player, temp.Player, true))
									newLeader = temp.Player;
							}
							if (newLeader == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (newLeader.Guild != client.Player.Guild)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotInYourGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							newLeader.GuildRank = newLeader.Guild.GetRankByID(0);
							newLeader.SaveIntoDatabase();
							newLeader.Out.SendMessage(LanguageMgr.GetTranslation(newLeader.Client, "Scripts.Player.Guild.MadeLeader", newLeader.Guild.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							foreach (GamePlayer ply in client.Player.Guild.GetListOfOnlineMembers())
							{
								ply.Out.SendMessage(LanguageMgr.GetTranslation(ply.Client, "Scripts.Player.Guild.MadeLeaderOther", newLeader.Name, newLeader.Guild.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Emblem
						// --------------------------------------------------------------------------------
						// EMBLEM
						// --------------------------------------------------------------------------------
					case "emblem":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.Emblem != 0)
							{
								if (client.Player.TargetObject is EmblemNPC == false)
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.EmblemAlready"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
								client.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.EmblemRedo"), new CustomDialogResponse(EmblemChange));
								return;
							}
							if (client.Player.TargetObject is EmblemNPC == false)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.EmblemNPCNotSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							client.Out.SendEmblemDialogue();

							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Autoremove
					case "autoremove":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Remove))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							if (args.Length == 4 && args[3].ToLower() == "account")
							{
								//#warning how can player name  !=  account if args[3] = account ?
								string playername = args[3];
								string accountId = "";

								GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], false, true);
								if (targetClient != null)
								{
									OnCommand(client, new string[] { "gc", "remove", args[3] });
									accountId = targetClient.Account.Name;
								}
								else
								{
									DOLCharacters c = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(playername));

									if (c == null)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PlayerNotFound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
										return;
									}

									accountId = c.AccountName;
								}
								List<DOLCharacters> chars = new List<DOLCharacters>();
								chars.AddRange(DOLDB<DOLCharacters>.SelectObjects(DB.Column("AccountName").IsEqualTo(accountId)));
								//chars.AddRange((Character[])DOLDB<CharacterArchive>.SelectObjects("AccountID = '" + accountId + "'"));

								foreach (DOLCharacters ply in chars)
								{
									ply.GuildID = "";
									ply.GuildRank = 0;
								}
								GameServer.Database.SaveObject(chars);
								break;
							}
							else if (args.Length == 3)
							{
								GameClient targetClient = WorldMgr.GetClientByPlayerName(args[2], false, true);
								if (targetClient != null)
								{
									OnCommand(client, new string[] { "gc", "remove", args[2] });
									return;
								}
								else
								{
									var c = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(args[2]));
									if (c == null)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.PlayerNotFound"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
										return;
									}
									if (c.GuildID != client.Player.GuildID)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotInYourGuild"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
										return;
									}
									else
									{
										c.GuildID = "";
										c.GuildRank = 0;
										GameServer.Database.SaveObject(c);
									}
								}
								break;
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAutoRemoveAcc"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAutoRemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region MOTD
						// --------------------------------------------------------------------------------
						// MOTD
						// --------------------------------------------------------------------------------
					case "motd":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							message = String.Join(" ", args, 2, args.Length - 2);
							client.Player.Guild.Motd = message;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.MotdSet"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region AMOTD
						// --------------------------------------------------------------------------------
						// AMOTD
						// --------------------------------------------------------------------------------
					case "amotd":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.AllianceId == string.Empty)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							message = String.Join(" ", args, 2, args.Length - 2);
							client.Player.Guild.alliance.Dballiance.Motd = message;
							GameServer.Database.SaveObject(client.Player.Guild.alliance.Dballiance);
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AMotdSet"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region OMOTD
						// --------------------------------------------------------------------------------
						// OMOTD
						// --------------------------------------------------------------------------------
					case "omotd":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							message = String.Join(" ", args, 2, args.Length - 2);
							client.Player.Guild.Omotd = message;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.OMotdSet"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Alliance
						// --------------------------------------------------------------------------------
						// ALLIANCE
						// --------------------------------------------------------------------------------
					case "alliance":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							Alliance alliance = null;
							if (client.Player.Guild.AllianceId != null && client.Player.Guild.AllianceId != string.Empty)
							{
								alliance = client.Player.Guild.alliance;
							}
							else
							{
								DisplayMessage(client, "Your guild is not a member of an alliance!");
								return;
							}

							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceInfo", alliance.Dballiance.AllianceName));
							DBGuild leader = alliance.Dballiance.DBguildleader;
							if (leader != null)
								DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceLeader", leader.GuildName));
							else
								DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNoLeader"));

							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMembers"));
							int i = 0;
							foreach (DBGuild guild in alliance.Dballiance.DBguilds)
								if (guild != null)
									DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMember", i++, guild.GuildName));
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Alliance Invite
						// --------------------------------------------------------------------------------
						// AINVITE
						// --------------------------------------------------------------------------------
					case "ainvite":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer obj = client.Player.TargetObject as GamePlayer;
							if (obj == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (obj.GuildRank.RankLevel != 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNoGMSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (obj.Guild.alliance != null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceAlreadyOther"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (ServerProperties.Properties.ALLIANCE_MAX == 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceDisabled"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (ServerProperties.Properties.ALLIANCE_MAX != -1)
							{
								if (client.Player.Guild.alliance != null)
								{
									if (client.Player.Guild.alliance.Guilds.Count + 1 > ServerProperties.Properties.ALLIANCE_MAX)
									{
										client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMax"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
										return;
									}
								}
							}
							obj.TempProperties.setProperty("allianceinvite", client.Player); //finish that
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceInvite"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							obj.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceInvited", client.Player.Guild.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						case "aleader":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer obj = client.Player.TargetObject as GamePlayer;
							if (obj == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (obj.GuildRank.RankLevel != 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNoGMSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (obj.Guild.alliance != client.Player.Guild.alliance)
							{
								client.Out.SendMessage("You're not in the same alliance", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.alliance.Dballiance.LeaderGuildID != client.Player.Guild.GuildID)
							{
								client.Out.SendMessage("You're not the leader of the alliance", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
								return;
							}
							
							client.Player.Guild.alliance.Dballiance.AllianceName = obj.Guild.Name;
							client.Player.Guild.alliance.Dballiance.LeaderGuildID = obj.Guild.GuildID;
							GameServer.Database.SaveObject(client.Player.Guild.alliance.Dballiance);
							client.Player.Guild.alliance.SendMessageToAllianceMembers(obj.Guild.Name + " is the new leader of the alliance", PacketHandler.eChatType.CT_Alliance, PacketHandler.eChatLoc.CL_SystemWindow);
							
							// client.Player.Guild.alliance.PromoteGuild(obj.Guild);

							break;
						}
						
						#endregion
						#region Alliance Invite Accept
						// --------------------------------------------------------------------------------
						// AINVITE
						// --------------------------------------------------------------------------------
					case "aaccept":
						{
							AllianceInvite(client.Player, 0x01);
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Alliance Invite Cancel
						// --------------------------------------------------------------------------------
						// ACANCEL
						// --------------------------------------------------------------------------------
					case "acancel":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer obj = client.Player.TargetObject as GamePlayer;
							if (obj == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer inviter = client.Player.TempProperties.getProperty<object>("allianceinvite", null) as GamePlayer;
							if (inviter == client.Player)
								obj.TempProperties.removeProperty("allianceinvite");
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceAnsCancel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							obj.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceAnsCancel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							return;
						}
						#endregion
						#region Alliance Invite Decline
						// --------------------------------------------------------------------------------
						// ADECLINE
						// --------------------------------------------------------------------------------
					case "adecline":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							GamePlayer inviter = client.Player.TempProperties.getProperty<object>("allianceinvite", null) as GamePlayer;
							client.Player.TempProperties.removeProperty("allianceinvite");
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceDeclined"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							inviter.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceDeclinedOther"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							return;
						}
						#endregion
						#region Alliance Remove
						// --------------------------------------------------------------------------------
						// AREMOVE
						// --------------------------------------------------------------------------------
					case "aremove":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.alliance == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.GuildID != client.Player.Guild.alliance.Dballiance.DBguildleader.GuildID)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNotLeader"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (args.Length > 3)
							{
								if (args[2] == "alliance")
								{
									try
									{
										int index = Convert.ToInt32(args[3]);
										Guild myguild = (Guild)client.Player.Guild.alliance.Guilds[index];
										if (myguild != null)
											client.Player.Guild.alliance.RemoveGuild(myguild);
									}
									catch
									{
										client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceIndexNotVal"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
									}

								}
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildARemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildARemoveAlli"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								return;
							}
							else
							{
								GamePlayer obj = client.Player.TargetObject as GamePlayer;
								if (obj == null)
								{
									client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPlayerSelected"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
								if (obj.Guild == null)
								{
									client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMemNotSel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
								if (obj.Guild.alliance != client.Player.Guild.alliance)
								{
									client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceMemNotSel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
									return;
								}
								client.Player.Guild.alliance.RemoveGuild(obj.Guild);
							}
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Alliance Leave
						// --------------------------------------------------------------------------------
						// ALEAVE
						// --------------------------------------------------------------------------------
					case "aleave":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Alli))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.alliance == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.AllianceNotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							client.Player.Guild.alliance.RemoveGuild(client.Player.Guild);
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Claim
						// --------------------------------------------------------------------------------
						//ClAIM
						// --------------------------------------------------------------------------------
					case "claim":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(client.Player.CurrentRegionID, client.Player, WorldMgr.VISIBILITY_DISTANCE);
							if (keep == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.ClaimNotNear"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (keep.CheckForClaim(client.Player))
							{
								keep.Claim(client.Player);
							}
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Release
						// --------------------------------------------------------------------------------
						//RELEASE
						// --------------------------------------------------------------------------------
					case "release":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.ClaimedKeeps.Count == 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoKeep"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Release))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.ClaimedKeeps.Count == 1)
							{
								if (client.Player.Guild.ClaimedKeeps[0].CheckForRelease(client.Player))
								{
									client.Player.Guild.ClaimedKeeps[0].Release();
								}
							}
							else
							{
								foreach (AbstractArea area in client.Player.CurrentAreas)
								{
									if (area is KeepArea && ((KeepArea)area).Keep.Guild == client.Player.Guild)
									{
										if (((KeepArea)area).Keep.CheckForRelease(client.Player))
										{
											((KeepArea)area).Keep.Release();
										}
									}
								}
							}
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Upgrade
						// --------------------------------------------------------------------------------
						//UPGRADE
						// --------------------------------------------------------------------------------
					case "upgrade":
						{
							client.Out.SendMessage("Keep upgrading is currently disabled!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return;
							/* un-comment this to work on allowing keep upgrading
                            if (client.Player.Guild == null)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 0)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoKeep"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (!client.Player.Guild.GotAccess(client.Player, Guild.eGuildRank.Upgrade))
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (args.Length != 3)
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.KeepNoLevel"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            byte targetlevel = 0;
                            try
                            {
                                targetlevel = Convert.ToByte(args[2]);
                                if (targetlevel > 10 || targetlevel < 1)
                                    return;
                            }
                            catch
                            {
                                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.UpgradeScndArg"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
                                return;
                            }
                            if (client.Player.Guild.ClaimedKeeps.Count == 1)
                            {
                                foreach (AbstractGameKeep keep in client.Player.Guild.ClaimedKeeps)
                                    keep.StartChangeLevel(targetlevel);
                            }
                            else
                            {
                                foreach (AbstractArea area in client.Player.CurrentAreas)
                                {
                                    if (area is KeepArea && ((KeepArea)area).Keep.Guild == client.Player.Guild)
                                        ((KeepArea)area).Keep.StartChangeLevel(targetlevel);
                                }
                            }
                            client.Player.Guild.UpdateGuildWindow();
                            return;
							 */
						}
						#endregion
						#region Type
						//TYPE
						// --------------------------------------------------------------------------------
					case "type":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (client.Player.Guild.ClaimedKeeps.Count == 0)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoKeep"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Upgrade))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							int type = 0;
							try
							{
								type = Convert.ToInt32(args[2]);
								if (type != 1 || type != 2 || type != 4)
									return;
							}
							catch
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.UpgradeScndArg"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								return;
							}
							//client.Player.Guild.ClaimedKeep.Release();
							client.Player.Guild.UpdateGuildWindow();
							return;
						}
						#endregion
						#region Noteself
					case "noteself":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							string note = String.Join(" ", args, 2, args.Length - 2);
							client.Player.GuildNote = note;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoteSet", note), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Note
						case "note":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
							{
								client.Out.SendMessage("Use '/gc noteself <note>' to set your own note", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							if (args[2] is null)
							{
								client.Out.SendMessage("You need to specify a target guild member.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							bool noteSet = false;
							foreach (var guildMember in GuildMgr.GetAllGuildMembers(client.Player.GuildID))
							{
								if (guildMember.Value.Name.ToLower() != args[2].ToLower()) continue;
								string note = String.Join(" ", args, 3, args.Length - 3);
								guildMember.Value.Note = note;
								noteSet = true;
								break;
							}
							
							if(!noteSet)
								client.Out.SendMessage("No guild member with that name found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							else
								client.Out.SendMessage($"Note set correctly for {args[2]}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							
							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Dues
					case "dues":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Dues))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}
							if (args[2] == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDues"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							long amount = long.Parse(args[2]);
							if (amount == 0)
							{
								client.Player.Guild.SetGuildDues(false);
								client.Player.Guild.SetGuildDuesPercent(0);
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DuesOff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							}
							else if (amount > 0 && amount <= 25)
							{
								client.Player.Guild.SetGuildDues(true);
								if (ServerProperties.Properties.NEW_GUILD_DUES)
								{
									client.Player.Guild.SetGuildDuesPercent(amount);
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DuesOn", amount), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								}
								else
								{
									client.Player.Guild.SetGuildDuesPercent(2);
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DuesOn", 2), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								}
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDues"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Deposit
					case "deposit":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}

							double amount = double.Parse(args[2]);
							if (amount < 0 || amount > 1000000001)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DepositInvalid"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							else if (client.Player.GetCurrentMoney() < amount)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.DepositTooMuch"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Player.Guild.SetGuildBank(client.Player, amount);
							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Withdraw
					case "withdraw":
						{
							if (client.Player.Guild == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Withdraw))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
								return;
							}

							double amount = double.Parse(args[2]);
							if (amount < 0 || amount > 1000000001)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.WithdrawInvalid"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							}
							else if ((client.Player.Guild.GetGuildBank() - amount) < 0)
							{
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Player.Client, "Scripts.Player.Guild.WithdrawTooMuch"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
								return;
							}
							else
							{
								client.Player.Guild.WithdrawGuildBank(client.Player, amount);

							}
							client.Player.Guild.UpdateGuildWindow();
						}
						break;
						#endregion
						#region Logins
					case "logins":
						{
							client.Player.ShowGuildLogins = !client.Player.ShowGuildLogins;

							if (client.Player.ShowGuildLogins)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.LoginsOn"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.LoginsOff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
							}
							client.Player.Guild.UpdateGuildWindow();
							break;
						}
						#endregion
						#region Default
					default:
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.UnknownCommand", args[1]), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							DisplayHelp(client);
						}
						break;
						#endregion
				}
			}
			catch (Exception e)
			{
				if (ServerProperties.Properties.ENABLE_DEBUG)
				{
					log.Debug("Error in /gc script, " + args[1] + " command: " + e.ToString());
				}

				DisplayHelp(client);
			}
		}

		private const string GUILD_BANNER_PRICE = "GUILD_BANNER_PRICE";

		protected void ConfirmBannerBuy(GamePlayer player, byte response)
		{
			if (response != 0x01)
				return;

			long bannerPrice = player.TempProperties.getProperty<long>(GUILD_BANNER_PRICE, 0);
			player.TempProperties.removeProperty(GUILD_BANNER_PRICE);

			if (bannerPrice == 0 || player.Guild.GuildBanner)
				return;

			if (player.Guild.BountyPoints >= bannerPrice || player.Client.Account.PrivLevel > (int)ePrivLevel.Player)
			{
				player.Guild.RemoveBountyPoints(bannerPrice);
				player.Guild.GuildBanner = true;
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.BannerBought", bannerPrice), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			}
			else
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.BannerNotAfford"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

		}


		private const string GUILD_BUFF_TYPE = "GUILD_BUFF_TYPE";

		protected void ConfirmBuffBuy(GamePlayer player, byte response)
		{
			if (response != 0x01)
				return;

			Guild.eBonusType buffType = player.TempProperties.getProperty<Guild.eBonusType>(GUILD_BUFF_TYPE, Guild.eBonusType.None);
			player.TempProperties.removeProperty(GUILD_BUFF_TYPE);

			if (buffType == Guild.eBonusType.None || player.Guild.MeritPoints < 1000 || player.Guild.BonusType != Guild.eBonusType.None)
				return;

			player.Guild.BonusType = buffType;
			player.Guild.RemoveMeritPoints(1000);
			player.Guild.BonusStartTime = DateTime.Now;

			string buffName = Guild.BonusTypeToName(buffType);

			foreach (GamePlayer ply in player.Guild.GetListOfOnlineMembers())
			{
				ply.Out.SendMessage(LanguageMgr.GetTranslation(ply.Client, "Scripts.Player.Guild.BuffActivated", player.Name), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
				ply.Out.SendMessage(string.Format("Your guild now has a bonus to {0} for 24 hours!", buffName), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			}

			player.Guild.UpdateGuildWindow();

		}


		/// <summary>
		/// method to handle the aliance invite
		/// </summary>
		/// <param name="player"></param>
		/// <param name="reponse"></param>
		protected void AllianceInvite(GamePlayer player, byte reponse)
		{
			if (reponse != 0x01)
				return; //declined

			GamePlayer inviter = player.TempProperties.getProperty<object>("allianceinvite", null) as GamePlayer;

			if (player.Guild == null)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.NotMember"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (inviter == null || inviter.Guild == null)
			{
				return;
			}

			if (!player.Guild.HasRank(player, Guild.eRank.Alli))
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			player.TempProperties.removeProperty("allianceinvite");

			if (inviter.Guild.alliance == null)
			{
				//create alliance
				Alliance alli = new Alliance();
				DBAlliance dballi = new DBAlliance();
				dballi.AllianceName = inviter.Guild.Name;
				dballi.LeaderGuildID = inviter.GuildID;
				dballi.DBguildleader = null;
				dballi.Motd = "";
				alli.Dballiance = dballi;
				alli.Guilds.Add(inviter.Guild);
				inviter.Guild.alliance = alli;
				inviter.Guild.AllianceId = inviter.Guild.alliance.Dballiance.ObjectId;
			}
			inviter.Guild.alliance.AddGuild(player.Guild);
			inviter.Guild.alliance.SaveIntoDatabase();
			player.Guild.UpdateGuildWindow();
			inviter.Guild.UpdateGuildWindow();
		}

		/// <summary>
		/// method to handle the emblem change
		/// </summary>
		/// <param name="player"></param>
		/// <param name="reponse"></param>
		public static void EmblemChange(GamePlayer player, byte reponse)
		{
			if (reponse != 0x01)
				return;
			if (player.TargetObject is EmblemNPC == false)
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.EmblemNeedNPC"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			if (player.GetCurrentMoney() < GuildMgr.COST_RE_EMBLEM) //200 gold to re-emblem
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Player.Guild.EmblemNeedGold"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			player.Out.SendEmblemDialogue();
			player.Guild.UpdateGuildWindow();
		}

		public void DisplayHelp(GameClient client)
		{
			if (client.Account.PrivLevel > 1)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMCommands"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMCreate"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMPurge"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMRename"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMAddPlayer"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildGMRemovePlayer"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			}
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildUsage"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildForm"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildInfo"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildRanks"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildCancel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDecline"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildClaim"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildQuit"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildMotd"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAMotd"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildOMotd"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildPromote"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDemote"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildRemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildRemAccount"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEmblem"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEdit"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildLeader"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAccept"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildInvite"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildWho"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildList"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAlli"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAAccept"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildACancel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildADecline"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildAInvite"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildARemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildARemoveAlli"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildALeader"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildNoteSelf"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDues"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildDeposit"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildWithdraw"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildWebpage"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEmail"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildBuff"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildBuyBanner"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildBannerSummon"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// method to handle commands for /gc edit
		/// </summary>
		/// <param name="client"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public int GCEditCommand(GameClient client, string[] args)
		{
			if (args.Length < 4)
			{
				DisplayEditHelp(client);
				return 0;
			}

			bool reponse = true;
			if (args.Length > 4)
			{
				if (args[4].StartsWith("y"))
					reponse = true;
				else if (args[4].StartsWith("n"))
					reponse = false;
				else if (args[3] != "title" && args[3] != "ranklevel")
				{
					DisplayEditHelp(client);
					return 1;
				}
			}
			byte number;
			try
			{
				number = Convert.ToByte(args[2]);
				if (number > 9 || number < 0)
					return 0;
			}
			catch
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.ThirdArgNotNum"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return 0;
			}

			switch (args[3])
			{
				case "title":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						string message = String.Join(" ", args, 4, args.Length - 4);
						client.Player.Guild.GetRankByID(number).Title = message;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankTitleSet", number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
						client.Player.Guild.UpdateGuildWindow();
					}
					break;
				case "ranklevel":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						if (args.Length >= 5)
						{
							byte lvl = Convert.ToByte(args[4]);
							client.Player.Guild.GetRankByID(number).RankLevel = lvl;
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankLevelSet", lvl.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
						}
						else
						{
							DisplayEditHelp(client);
						}
					}
					break;

				case "emblem":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Emblem = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankEmblemSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "gchear":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).GcHear = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankGCHearSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "gcspeak":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}

						client.Player.Guild.GetRankByID(number).GcSpeak = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankGCSpeakSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "ochear":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).OcHear = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankOCHearSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "ocspeak":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).OcSpeak = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankOCSpeakSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "achear":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).AcHear = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankACHearSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "acspeak":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).AcSpeak = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankACSpeakSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "invite":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Invite = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankInviteSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "promote":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Promote = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankPromoteSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "remove":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Remove = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankRemoveSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "alli":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Alli = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankAlliSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "view":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.View))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).View = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankViewSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "buff":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Buff = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankBuffSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "claim":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Claim))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Claim = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankClaimSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "upgrade":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Upgrade))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Upgrade = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankUpgradeSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "release":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Release))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Release = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankReleaseSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "dues":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Dues))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Release = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankDuesSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				case "withdraw":
					{
						if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Withdraw))
						{
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.NoPrivilages"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
							return 1;
						}
						client.Player.Guild.GetRankByID(number).Release = reponse;
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.RankWithdrawSet", (reponse ? "enabled" : "disabled"), number.ToString()), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
					}
					break;
				default:
					{
						DisplayEditHelp(client);
						return 0;
					}
			} //switch
			DBRank rank = client.Player.Guild.GetRankByID(number);
			if (rank != null)
				GameServer.Database.SaveObject(rank);
			return 1;
		}

		/// <summary>
		/// Send social window data to the client
		/// </summary>
		/// <param name="client"></param>
		/// <param name="sort"></param>
		/// <param name="page"></param>
		/// <param name="offline">0 = false, 1 = true, 2 to try and recall last setting used by player</param>
		private void SendSocialWindowData(GameClient client, int sort, int page, byte offline)
		{
			Dictionary<string, GuildMgr.GuildMemberDisplay> allGuildMembers = GuildMgr.GetAllGuildMembers(client.Player.GuildID);

			if (allGuildMembers == null || allGuildMembers.Count == 0)
			{
				return;
			}

			bool showOffline = false;

			if (offline < 2)
			{
				showOffline = (offline == 0 ? false : true);
			}
			else
			{
				// try to recall last setting
				showOffline = client.Player.TempProperties.getProperty<bool>("SOCIALSHOWOFFLINE", false);
			}

			client.Player.TempProperties.setProperty("SOCIALSHOWOFFLINE", showOffline);

			//The type of sorting we will be sending
			GuildMgr.GuildMemberDisplay.eSocialWindowSort sortOrder = (GuildMgr.GuildMemberDisplay.eSocialWindowSort)sort;

			//Let's sort the sorted list - we don't need to sort if sort = name
			SortedList<string, GuildMgr.GuildMemberDisplay> sortedWindowList = null;

			GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Name;

			#region Determine Sort
			switch (sortOrder)
			{
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.ClassAsc:
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.ClassDesc:
					sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.ClassID;
					break;
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.GroupAsc:
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.GroupDesc:
					sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Group;
					break;
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.LevelAsc:
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.LevelDesc:
					sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Level;
					break;
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.NoteAsc:
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.NoteDesc:
					sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Note;
					break;
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.RankAsc:
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.RankDesc:
					sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.Rank;
					break;
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.ZoneOrOnlineAsc:
				case GuildMgr.GuildMemberDisplay.eSocialWindowSort.ZoneOrOnlineDesc:
					sortColumn = GuildMgr.GuildMemberDisplay.eSocialWindowSortColumn.ZoneOrOnline;
					break;
			}
			#endregion

			if (showOffline == false) // show only a sorted list of online players
			{
				IList<GamePlayer> onlineGuildPlayers = client.Player.Guild.GetListOfOnlineMembers();
				sortedWindowList = new SortedList<string, GuildMgr.GuildMemberDisplay>(onlineGuildPlayers.Count);

				foreach (GamePlayer player in onlineGuildPlayers)
				{
					if (allGuildMembers.ContainsKey(player.InternalID))
					{
						GuildMgr.GuildMemberDisplay memberDisplay = allGuildMembers[player.InternalID];
						memberDisplay.UpdateMember(player);
						string key = memberDisplay[sortColumn];

						if (sortedWindowList.ContainsKey(key))
							key += sortedWindowList.Count.ToString();

						sortedWindowList.Add(key, memberDisplay);
					}
				}
			}
			else // sort and display entire list
			{
				sortedWindowList = new SortedList<string, GuildMgr.GuildMemberDisplay>();
				int keyIncrement = 0;

				foreach (GuildMgr.GuildMemberDisplay memberDisplay in allGuildMembers.Values)
				{
					GamePlayer p = client.Player.Guild.GetOnlineMemberByID(memberDisplay.InternalID);
					if (p != null)
					{
						//Update to make sure we have the most up to date info
						memberDisplay.UpdateMember(p);
					}
					else
					{
						//Make sure that since they are offline they get the offline flag!
						memberDisplay.GroupSize = "0";
					}
					//Add based on the new index
					string key = memberDisplay[sortColumn];

					if (sortedWindowList.ContainsKey(key))
					{
						key += keyIncrement++;
					}

					try
					{
						sortedWindowList.Add(key, memberDisplay);
					}
					catch
					{
						if (log.IsErrorEnabled)
							log.Error(string.Format("Sorted List duplicate entry - Key: {0} Member: {1}. Replacing - Member: {2}.  Sorted count: {3}.  Guild ID: {4}", key, memberDisplay.Name, sortedWindowList[key].Name, sortedWindowList.Count, client.Player.GuildID));
					}
				}
			}

			//Finally lets send the list we made

			IList<GuildMgr.GuildMemberDisplay> finalList = sortedWindowList.Values;

			int i = 0;
			string[] buffer = new string[10];
			for (i = 0; i < 10 && finalList.Count > i + (page - 1) * 10; i++)
			{
				GuildMgr.GuildMemberDisplay memberDisplay;

				if ((int)sortOrder > 0)
				{
					//They want it normal
					memberDisplay = finalList[i + (page - 1) * 10];
				}
				else
				{
					//They want it in reverse
					memberDisplay = finalList[(finalList.Count - 1) - (i + (page - 1) * 10)];
				}

				buffer[i] = memberDisplay.ToString((i + 1) + (page - 1) * 10, finalList.Count);
			}

			client.Out.SendMessage("TE," + page.ToString() + "," + finalList.Count + "," + i.ToString(), eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);

			foreach (string member in buffer)
				client.Player.Out.SendMessage(member, eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);

		}

		public void DisplayEditHelp(GameClient client)
		{
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildUsage"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditTitle"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditRankLevel"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditEmblem"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditGCHear"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditGCSpeak"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditOCHear"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditOCSpeak"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditACHear"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditACSpeak"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditInvite"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditPromote"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditRemove"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditView"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditAlli"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditClaim"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditUpgrade"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditRelease"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditDues"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage("/gc edit <ranknum> buff <y/n>", eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Guild.Help.GuildEditWithdraw"), eChatType.CT_Guild, eChatLoc.CL_SystemWindow);
		}
	}
}
