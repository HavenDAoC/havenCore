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
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	/// <summary>
	/// Handles all user-based interaction for the '/serverproperties' command
	/// </summary>
	[CmdAttribute(
		// Enter '/serverproperties' to list all associated subcommands
		"&serverproperties",
		// Message: '/serverproperties' - Refreshes the live cache with any changes from the 'serverproperty' table. 
		"AdminCommands.ServerProp.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		ePrivLevel.Admin, // Required minimum privilege level to use the command
		// Message: Refreshes the live cache with any changes from the 'serverproperty' table.
		"AdminCommands.ServerProperties.Description",
		// Syntax: /serverproperties
		"AdminCommands.ServerProp.Syntax.ServerProp",
		// Message: Refreshes the server's properties.
		"AdminCommands.ServerProp.Usage.ServerProp")]
	public class ServerPropertiesCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			// Dated code for people still using XML setups instead of DBs
			if (GameServer.Instance.Configuration.DBType == DOL.Database.Connection.ConnectionType.DATABASE_XML)
			{
				// Message: XML is cached sorry, you cannot refresh server properties unless using MySQL!
				ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "AdminCommands.ServerProp.XMLOld.Sorry", null);
				return;
			}
			
			ServerProperties.Properties.Refresh();
			// Message: Atlas' server properties have been refreshed!
			ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "AdminCommands.ServerProp.Msg.PropsRefreshed", null);
		}
	}
}