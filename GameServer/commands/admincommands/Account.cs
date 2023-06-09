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

/* <--- SendMessage Standardization --->
*  All messages now use translation IDs to both
*  centralize their location and standardize the method
*  of message calls used throughout this project. All messages affected
*  are in English. Other languages are not yet supported.
* 
*  To  find a message at its source location, either use
*  the message body contained in the comment above the return
*  (e.g., // Message: "This is a message.") or the
*  translation ID (e.g., "AdminCommands.Account.Description").
* 
*  To perform message changes, take note of your server settings.
*  If the `serverproperty` table setting `use_dblanguage`
*  is set to `True`, you must make your changes from the
*  `languagesystem` DB table.
* 
*  If the `serverproperty` table setting
*  `update_existing_db_system_sentences_from_files` is set to `True`,
*  perform changes to messages from this file at "GameServer >
*  language > EN > OtherSentences.txt" and "Commands > AdminCommands.txt".
*
*  OPTIONAL: After changing a message, paste the new content
*  into the comment above the affected message return(s). This is
*  done for ease of reference. */

using System.Text.RegularExpressions;
using DOL.GS.PacketHandler.Client.v168;

namespace DOL.GS.Commands
{
	// See the comments above 'using' about SendMessage translation IDs
	[CmdAttribute(
		// Enter '/account' to list all associated subcommands
		"&account",
		// Message: '/account' - Creates new, manages existing, and controls character assignment for Atlas DAoC accounts.
		"AdminCommands.Account.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.Admin,
		// Message: Creates new, manages existing, and controls character assignment for HavenDAoC accounts. We recommend editing the database directly where possible to perform many of these same functions. Otherwise, use the following syntax:
		"AdminCommands.Account.Description",
		// Syntax: /account command
		"AdminCommands.Account.Syntax.Comm",
		// Message: Provides syntax information regarding all '/account' subcommands in a dialog.
		"AdminCommands.Account.Usage.Comm",
		// Syntax: /account accountname <characterName>
		"AdminCommands.Account.Syntax.AccountName",
		// Message: Identifies the account associated with the character. This may be used on offline characters.
		"AdminCommands.Account.Usage.AccountName",
		// Syntax: /account changepassword <accountName> <newPassword>
		"AdminCommands.Account.Syntax.ChangePassword",
		// Message: Changes the password associated with an existing account. If a player requests a password reset, verify ownership of the account.
		"AdminCommands.Account.Usage.ChangePassword",
		// Syntax: /account create <accountName> <password>
		"AdminCommands.Account.Syntax.Create",
		// Message: Creates a new account with the specified login credentials.
        "AdminCommands.Account.Usage.Create",
		// Syntax: /account delete <accountName>
		"AdminCommands.Account.Syntax.Delete",
		// Message: Deletes the specified account, along with any associated characters.
		"AdminCommands.Account.Usage.Delete",
		// Syntax: /account deletecharacter <characterName>
		"AdminCommands.Account.Syntax.DeleteChar",
		// Message: Deletes the matching character from its associated account.
		"AdminCommands.Account.Usage.DeleteChar",
		// Syntax: /account movecharacter <characterName> <destAccount>
		"AdminCommands.Account.Syntax.MoveChar",
		// Message: Moves the specified character to the first available slot of the same realm on the destination account.
		"AdminCommands.Account.Usage.MoveChar",
		// Syntax: /account status <accountName> <status>
		"AdminCommands.Account.Syntax.Status",
		// Message: Sets an account's status (between '0' and '255'), which is used to define custom behaviors.
		"AdminCommands.Account.Usage.Status",
		// Syntax: /account unban <accountName>
		"AdminCommands.Account.Syntax.Unban",
		// Message: Removes an account's ban state, if one is active. This command cannot remove IP-only bans ('/ban ip').
		"AdminCommands.Account.Usage.Unban"
	)]
	
	public class AccountCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				// Lists all '/account' type commands' syntax (see section above)
				DisplaySyntax(client);
				return;
			}

			// Switch case for all possible subcommands
			switch (args[1].ToLower())
			{
				#region Create
				// --------------------------------------------------------------------------------
				// CREATE
				// '/account create <accountName> <password>'
				// Creates a new account with the specified login credentials.
				// --------------------------------------------------------------------------------
                case "create":
                    {
	                    if (args.Length < 4)
                        {
                            // Lists the '/account create' command's full syntax
	                        // Syntax: <----- '/{0}{1}' Subcommand {2}----->
	                        // Message: Use the following syntax for this command:
	                        // Syntax: /account create <accountName> <password>
	                        // Message: Creates a new account with the specified login credentials.
	                        DisplayHeadCmd(client, "account", "create", "", 3, false, "AdminCommands.Account.Syntax.Create", "AdminCommands.Account.Usage.Create");
	                        return;
                        }

	                    // Convert all letters in '<accountName>' to lowercase
                        var accountname = args[2].ToLower();
                        var password = args[3];
                        
                        // Only letters and numbers allowed in account name
                        if (Regex.IsMatch(accountname, @"^[a-z0-9]*$"))
                        {
                            // Account name & password must each be 4 or more characters in length
	                        if (accountname.Length < 4 || password.Length < 4)
	                        {
		                        // Message: A new account could not be created! Either the account name or password is too short. These must both be a minimum of 4 characters in length.
		                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.InvalidNamePass", null);
	                            return;
	                        }
	                        
	                        // Check for existing accounts, throw error if one exists with same name
	                        Account account = GetAccount(accountname);
	                        if (account != null)
	                        {
		                        // Message: An account already exists with the name '{0}'! Try again with a different value.
		                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.AlreadyRegistered", accountname);
	                            return;
	                        }
	                        
	                        account = new Account
	                        {
	                            Name = accountname,
	                            Password = LoginRequestHandler.CryptPassword(password),
	                            PrivLevel = (uint)ePrivLevel.Player,
	                            Realm = (int)eRealm.None,
	                            CreationDate = DateTime.Now,
	                            Language = ServerProperties.Properties.SERV_LANGUAGE
	                        };
	                        
	                        GameServer.Database.AddObject(account);
	                        
	                        // todo Make Audit work
	                        // AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountCreate, "", "acct="+args[2]);
	                        
	                        // Message: You have successfully created the account '{0}'! Remember to write down the login credentials for future use.
	                        ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Account.Msg.AccountCreated", account.Name);
                        }
                        else
                        {
	                        // Message: A new account could not be created! Special characters (e.g., !@#.?) were detected in the account name. Only numbers and lower-case letters are allowed.
	                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.SpecialChars", null);
                        }
                    }
                    break;
                #endregion Create
				#region Change Password
				// --------------------------------------------------------------------------------
				// CHANGE PASSWORD
				// '/account changepassword <accountName> <newPassword>'
				// Changes the password associated with an existing account. If a player requests a password reset, verify ownership of the account.
				// --------------------------------------------------------------------------------
				case "changepassword":
					{
						// Lists this command's full syntax
						if (args.Length < 4)
						{
							// Syntax: <----- '/{0}{1}' Subcommand {2}----->
							// Message: If you are unable to perform database queries to accomplish this action, use the following syntax:
							// Message: /account changepassword <accountName> <newPassword>
							// Message: Changes the password associated with an existing account. If a player requests a password reset, verify ownership of the account.
							DisplayHeadCmd(client, "account", "changepassword", "", 3, true, "AdminCommands.Account.Syntax.ChangePassword", "AdminCommands.Account.Usage.ChangePassword");
							return;
						}

						string accountname = args[2];
						string newpass = args[3];

						Account acc = GetAccount(accountname);
						
						// If no account currently exists with the specified account name
						if (acc == null)
						{
							// Message: No account exists with the name '{0}'. Please make sure you entered the full account name correctly.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.AccountNotFound", accountname);
							return;
						}
						
						// If account password is shorter than 4 chars
						if (newpass.Length < 4)
						{
							// Message: A new password could not be set! The expected value was too short. Enter a password at least 4 characters long for security purposes.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.PasswordChars", accountname);
							return;
						}
						
						// Kicks anyone on the account off (in case of account theft)
						KickAccount(acc);
						acc.Password = LoginRequestHandler.CryptPassword(newpass);
						GameServer.Database.SaveObject(acc);
						
						// Message: You have successfully changed the password to account '{0}'. Share this with the account owner, if applicable.
						ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Account.Msg.PasswordChanged", accountname);
						
						// todo Make Audit work
						// AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountPasswordChange, "acct="+args[2], "");
					}
					break;
				#endregion Change Password
				#region Delete
				// --------------------------------------------------------------------------------
				// DELETE
				// '/account delete <accountName>'
				// Deletes the specified account, along with any associated characters.
				// --------------------------------------------------------------------------------
				case "delete":
					{
						// Lists the command's full syntax
						if (args.Length < 3)
						{
							// Message: <----- '/{0}{1}' Subcommand {2}----->
							// Message: If you are unable to perform database queries to accomplish this action, use the following syntax:
							// Message: /account delete <accountName>
							// Message: Deletes the specified account, along with any associated characters.
							DisplayHeadCmd(client, "account", "delete", "", 3, true, "AdminCommands.Account.Syntax.Delete", "AdminCommands.Account.Usage.Delete");
							return;
						}

						string accountname = args[2];
                        Account acc = GetAccount(accountname);

                        // If no account matches
						if (acc == null)
						{
							// Message: No account exists with the name '{0}'.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AllCommands.Command.Err.NoAccountExists", accountname);
							return;
						}

						// Removes the player from the server, closes the DAoC client, and deletes the account
						KickAccount(acc);
						GameServer.Database.DeleteObject(acc);
						
						// todo Make Audit work
						// AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountDelete, args[2], (""));
						
						// Message: You have successfully deleted the account '{0}'!
						ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Account.Msg.AccountDeleted", acc.Name);
						return;
					}
				#endregion Delete
				#region Delete Character
				// --------------------------------------------------------------------------------
				// DELETE CHARACTER
				// '/account deletecharacter <characterName>'
				// Deletes the matching character from its associated account.
				// --------------------------------------------------------------------------------
                case "deletecharacter":
                    {
	                    // Lists the command's full syntax
                        if (args.Length < 3)
                        {
	                        // Message: <----- '/{0}{1}' Subcommand {2}----->
	                        // Message: If you are unable to perform database queries to accomplish this action, use the following syntax:
	                        // Message: /account deletecharacter <characterName>
	                        // Message: Deletes the matching character from its associated account.
	                        DisplayHeadCmd(client, "account", "deletecharacter", "", 3, true, "AdminCommands.Account.Syntax.DeleteChar", "AdminCommands.Account.Usage.DeleteChar");
                            return;
                        }

                        string charname = args[2];
                        DOLCharacters cha = GetCharacter(charname);

                        // If no character exists that matches the exact name entered
                        if (cha == null)
                        {
	                        // Message: No character exists with the name '{0}'.
	                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AllCommands.Command.Err.NoExistPlayer", charname);
                            return;
                        }

                        // If the character is logged in, remove them from the game
                        KickCharacter(cha);
                        GameServer.Database.DeleteObject(cha);

						// todo Make Audit work
						// AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterDelete, args[2], (""));
						
						// Message: You have successfully deleted the character '{0}'!
						ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Account.Msg.CharacterDeleted", cha.Name);
                        return;
                    }
                #endregion Delete Character
				#region Move Character
				// --------------------------------------------------------------------------------
				// MOVE CHARACTER
				// '/account movecharacter <characterName> <newAccount>'
				// Moves the specified character to the first available slot of the same realm on the destination account.
				// --------------------------------------------------------------------------------
                case "movecharacter":
                    {
	                    // Lists the command's full syntax
                        if (args.Length < 4)
                        {
	                        // Message: <----- '/{0}{1}' Subcommand {2}----->
	                        // Message: Use the following syntax for this command:
	                        // Message: /account movecharacter <characterName> <destAccount>
	                        // Message: Moves the specified character to the first available slot of the same realm on the destination account.
	                        DisplayHeadCmd(client, "account", "movecharacter", "", 3, false, "AdminCommands.Account.Syntax.MoveChar", "AdminCommands.Account.Usage.MoveChar");
                            return;
                        }

                        string charname = args[2];
                        string accountname = args[3];

                        // Check 'dolcharacters' table for exact name match
                        DOLCharacters cha = GetCharacter(charname);
                        
                        if (cha == null)
                        {
	                        // Message: No character exists with the name '{0}'.
	                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AllCommands.Command.Err.NoExistPlayer", null);
                            return;
                        }
                        
                        // Check 'account' table for exact match
                        Account acc = GetAccount(accountname);
                        
                        // If the destination account does not exist
                        if (acc == null)
                        {
	                        // Message: No account exists with the name '{0}'.
	                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AllCommands.Command.Err.NoAccountExists", accountname);
                            return;
                        }

                        int firstAccountSlot;
                        switch ((eRealm)cha.Realm)
                        {
                            case eRealm.Albion:
                                firstAccountSlot = 100;
                                break;
                            case eRealm.Midgard:
                                firstAccountSlot = 200;
                                break;
                            case eRealm.Hibernia:
                                firstAccountSlot = 300;
                                break;
                            default:
                            {
	                            // If the character does not belong to any of these realms (i.e., somehow got assigned Neutral (0))
	                            // Message: {0} is currently assigned a realm ID of {1}. That is not an accepted value and the character move failed! Use the Atlas Web Admin tool or the '/player realm' command on the desired character to resolve this issue.
	                            ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.CharNotFromValidRealm", cha.Name, cha.Realm);
                                return;
	                        }
                        }

                        // Moves a character to the first available slot for that realm
                        int freeslot;
                        for (freeslot = firstAccountSlot; freeslot < firstAccountSlot + 8; freeslot++)
                        {
                            bool found = false;
                            foreach (DOLCharacters ch in acc.Characters)
                            {
                                if (ch.Realm == cha.Realm && ch.AccountSlot == freeslot)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                                break;

                        }

                        // If no free slots exist for a realm on the destination account
                        // todo Fix this, currently hides character and places it on same slot as another character, gives DB errors
                        if (freeslot == 0)
                        {
	                        // Message: The destination account of '{0}' has no available character slots for that realm! The character transfer has failed.
	                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.NoFreeSlots", accountname);
                            return;
                        }

                        GameClient playingclient = WorldMgr.GetClientByPlayerName(cha.Name, true, false);
                        
                        // Kick the player from the server to complete the character move
                        if (playingclient != null)
                        {
                            playingclient.Out.SendPlayerQuit(true);
                            playingclient.Disconnect();
                        }

                        cha.AccountName = acc.Name;
                        cha.AccountSlot = freeslot;

                        // Saves the character record with the destination account
                        GameServer.Database.SaveObject(cha);
                        
                        // todo Make Audit work
                        // AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterMove, "char="+args[2], ("acct="+args[3]));
                        
                        // Message: You have successfully transferred ownership of {0} to account '{1}'!
                        ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Account.Msg.CharacterMovedToAccount", cha.Name, acc.Name);
                        return;
                    }
                #endregion Move Character
				#region Status
				// --------------------------------------------------------------------------------
				// STATUS
				// '/account status <accountName> <status>'
				// Sets an account's status (between '0' and '256'), which is used to define custom behaviors.
				// --------------------------------------------------------------------------------
				case "status":
					{
						// Lists the command's full syntax
						if (args.Length < 3)
						{
							// Message: <----- '/{0}{1}' Subcommand {2}----->
							// Message: Use the following syntax for this command:
							// Message: /account status <accountName> <status>
							// Message: Sets an account's status (between '0' and '256'), which is used to define custom behaviors.
							DisplayHeadCmd(client, "account", "status", "", 3, false, "AdminCommands.Account.Syntax.Status", "AdminCommands.Account.Usage.Status");
							return;
						}

						string accountname = args[2];
						Account acc = GetAccount(accountname);

						if (acc == null)
						{
							// Message: No account exists with the name '{0}'.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AllCommands.Command.Err.NoExistPlayer", accountname);
							return;
						}

						int status;
                        // Exception: The value you entered was not expected. Please provide a number between '0' and '255'.
                        try
                        {
	                        status = Convert.ToInt32(args[3]);
                        }
                        catch (Exception)
                        {
	                        // Message: 
	                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.StatusValueReq", null);
	                        
	                        return;
                        }
                        
                        
						if (status >= 0 && status < 256 )
						{
							// Message triggers here to catch previous status from DB before it is replaced
							// Message: You have changed the status for account '{0}' from {1} to {2}!
							ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Account.Msg.SetStatus", acc.Name, acc.Status, status);
							
							// Change DB entry for Status
							acc.Status=status;
							GameServer.Database.SaveObject(acc);
							
							// todo Make Audit work
							// AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountStatusUpdated, "", (args[3]));
						}
						// If not 0 - 255
						else
						{
							// Message: The value you entered was not expected. Please provide a number between '0' and '255'.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.StatusValueReq", null);
						}
						
						return;
					}
				#endregion Status
				#region Unban
				// --------------------------------------------------------------------------------
				// UNBAN
				// '/account unban <accountName>'
				// Removes an account's ban state, if one is active. This command cannot remove
				// IP-only bans ('/ban ip').
				// --------------------------------------------------------------------------------
				case "unban":
					{
						// Lists the command's full syntax
						if (args.Length < 3)
						{
							// Message: <----- '/{0}{1}' Subcommand {2}----->
							// Message: If you are unable to perform database queries to accomplish this action, use the following syntax:
							// Message: /account unban <accountName>
							// Message: Removes an account's ban state, if one is active. This command cannot remove IP-only bans ('/ban ip').
							DisplayHeadCmd(client, "account", "unban", "", 3, false, "AdminCommands.Account.Syntax.Unban", "AdminCommands.Account.Usage.Unban");
							return;
						}

						string accountname = args[2];
						Account acc = GetAccount(accountname);
						
						if (acc == null)
						{
							// Message: No account exists with the name '{0}'.
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AllCommands.Command.Err.NoAccountExists", accountname);
							return;
						}

						var banacc = DOLDB<DBBannedAccount>.SelectObjects(DB.Column("Type").IsEqualTo("A").Or(DB.Column("Type").IsEqualTo("B")).And(DB.Column("Account").IsEqualTo(accountname)));
						
						// If no ban record exists for the specified account
						if (banacc.Count == 0)
						{
							// Message: The account '{0}' is not currently banned!
							ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.Account.Err.AccountBanNotFound", accountname);
							return;
						}
						
						// Removes the ban record from the database
						try
						{
							GameServer.Database.DeleteObject(banacc);
						}
						catch (Exception)
						{
							DisplaySyntax(client); return;
						}
						
						// todo Make Audit work
						// AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountUnbanned, accountname, (""));
						
						// Message: You have successfully removed the ban from the account '{0}'!
						ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Account.Msg.AccountUnbanned", accountname);
						return;
					}
				#endregion Unban
				#region Account Name
				// --------------------------------------------------------------------------------
				// ACCOUNT NAME
				// '/account accountname <characterName>'
				// Identifies the account associated with the character. This may be used on offline
				// characters.
				// --------------------------------------------------------------------------------
                case "accountname":
                    {
                        if (args.Length < 3)
                        {
	                        // Lists the command's full syntax
	                        // Message: <----- '/{0}{1}' Subcommand {2}----->
	                        // Message: If you are unable to perform database queries to accomplish this action, use the following syntax:
	                        // Message: /account accountname <characterName>
	                        // Message: Identifies the account associated with the character. This may be used on offline characters.
	                        DisplayHeadCmd(client, "account", "accountname", "", 3, true, "AdminCommands.Account.Syntax.AccountName", "AdminCommands.Account.Usage.AccountName");
                            return;
                        }

                        var charName = args[2];
                        DOLCharacters Char = GetCharacter(charName);
                        
                        if (Char == null)
                        {
	                        // Message: No character exists with the name '{0}'.
	                        ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AllCommands.Command.Err.NoExistPlayer", charName);
                            return;
                        }
                        
                        string accName = GetAccountName(Char.Name);
                        
                        // Message: {0} is associated with the account '{1}'.
                        ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.Account.Msg.AccNameForChar", Char.Name, accName);
                        return;
                    }
                #endregion Account Name
				#region Command
				// --------------------------------------------------------------------------------
				// COMMAND
				// '/account command'
				// Provides syntax information regarding all '/account' subcommands in a dialog.
				// --------------------------------------------------------------------------------
				case "command":
				{
					ChatUtil.SendWindowMessage((int)eWindow.Text, client, "Using the '/account' Command", 
						" ",
						" ",
						// Message: Creates new, manages existing, and controls character assignment for HavenDAoC accounts. We recommend editing the database directly where possible to perform many of these same functions. Otherwise, use the following syntax:
						"AdminCommands.Account.Description",
						" ",
						// Message: /account accountname <characterName>
						"AdminCommands.Account.Syntax.AccountName",
						// Message: Identifies the account associated with the character. This may be used on offline characters.
						"AdminCommands.Account.Usage.AccountName",
						" ",
						// Message: /account create <accountName> <password>
						"AdminCommands.Account.Syntax.Create",
						// Message: Creates a new account with the specified login credentials.
						"AdminCommands.Account.Usage.Create",
						" ",
						// Message: /account changepassword <accountName> <newPassword>
						"AdminCommands.Account.Syntax.ChangePassword",
						// Message: Changes the password associated with an existing account. If a player requests a password reset, verify ownership of the account.
						"AdminCommands.Account.Usage.ChangePassword",
						" ",
						// Message: /account delete <accountName>
						"AdminCommands.Account.Syntax.Delete",
						// Message: Deletes the specified account, along with any associated characters.
						"AdminCommands.Account.Usage.Delete",
						" ",
						// Message: /account deletecharacter <characterName>
						"AdminCommands.Account.Syntax.DeleteChar",
						// Message: Deletes the matching character from its associated account.
						"AdminCommands.Account.Usage.DeleteChar",
						" ",
						// Message: /account movecharacter <characterName> <destAccount>
						"AdminCommands.Account.Syntax.MoveChar",
						// Message: Moves the specified character to the first available slot of the same realm on the destination account.
						"AdminCommands.Account.Usage.MoveChar",
						" ",
						// Message: /account status <accountName> <status>
						"AdminCommands.Account.Syntax.Status",
						// Message: Sets an account's status (between '0' and '256'), which is used to define custom behaviors.
						"AdminCommands.Account.Usage.Status",
						" ",
						// Message: /account unban <accountName>
						"AdminCommands.Account.Syntax.Unban",
						// Message: Removes an account's ban state, if one is active. This command cannot remove IP-only bans ('/ban ip').
						"AdminCommands.Account.Usage.Unban",
						" ",
						" ",
						// Message: ----- Additional Info -----
						"Dialog.Header.Content.MoreInfo",
						" ",
						// Message: For more information regarding the '/account' slash command, the Slash Commands Library on the HavenDAoC Developer Wiki.
						"AdminCommands.Account.Comm.Desc1",
						" ",
						// Message: https://dev.havendaoc.com/docs/category/command-account
						"Hyperlinks.Haven.CommLibrary.Account",
						" ",
						" ");
					return;
				}
				#endregion Command
            }
		}
        
		/// <summary>
		/// Loads an account
		/// </summary>
		/// <param name="name">The account name</param>
		/// <returns>The matching account name or 'null'</returns>
		private Account GetAccount(string name)
		{
			GameClient client = WorldMgr.GetClientByAccountName(name, true);
			if (client != null)
				return client.Account;
			return GameServer.Database.FindObjectByKey<Account>(name);
		}

		/// <summary>
		/// Returns an active character
		/// </summary>
		/// <param name="charname">The character name</param>
		/// <returns>The matching character name or 'null'</returns>
		private DOLCharacters GetCharacter(string charname)
		{
			GameClient client = WorldMgr.GetClientByPlayerName(charname, true, false);
			
			if (client != null)
				return client.Player.DBCharacter;
			
			return DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(charname));
		}

		/// <summary>
		/// Kicks an active playing account from the server and closes the client
		/// </summary>
		/// <param name="acc">The account</param>
		private void KickAccount(Account acc)
		{
			GameClient playingClient = WorldMgr.GetClientByAccountName(acc.Name, true);
			
			if (playingClient != null)
			{
				playingClient.Out.SendPlayerQuit(true);
				playingClient.Disconnect();
			}
		}

		/// <summary>
		/// Kicks an active playing character from the server and closes the client
		/// </summary>
		/// <param name="cha">The character</param>
		private void KickCharacter(DOLCharacters cha)
		{
			GameClient playingClient = WorldMgr.GetClientByPlayerName(cha.Name, true, false);
			
			if (playingClient != null)
			{
				playingClient.Out.SendPlayerQuit(true);
				playingClient.Disconnect();
			}
		}

		/// <summary>
		/// Returns the account name associated with a character name
		/// </summary>
		/// <param name="charname">The character name</param>
		/// <returns>The account name or 'null'</returns>
		private string GetAccountName(string charname)
		{
			GameClient client = WorldMgr.GetClientByPlayerName(charname, true, false);
			
			if (client != null)
				return client.Account.Name;

			var ch = DOLDB<DOLCharacters>.SelectObject(DB.Column("Name").IsEqualTo(charname));
			
			if (ch != null)
				return ch.AccountName;
			else
				return null;
		}
	}
}