﻿/*
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

using log4net;
using System;
using System.Reflection;

namespace DOL.GS.PacketHandler;

[PacketLib(1127, GameClient.eClientVersion.Version1127)]
public class PacketLib1127 : PacketLib1126
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructs a new PacketLib for Client Version 1.127
    /// </summary>
    /// <param name="client">the gameclient this lib is associated with</param>
    public PacketLib1127(GameClient client)
        : base(client)
    {
    }

    private long m_lastPacketSendTick = 0;
    private long m_packetInterval = 500; //.5s
    private int m_numPacketsSent = 0;
    private int m_packetCap = 10; // packets sent every packetInterval

    /// <summary>
    /// 1127 login granted packet unchanged, work around for server type
    /// </summary>        
    public override void SendLoginGranted(byte color)
    {
        // work around for character screen bugs when server type sent as 00 but player doesnt have a realm
        // 0x07 allows for characters in all realms
        using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.LoginGranted)))
        {
            pak.WritePascalString(m_gameClient.Account.Name);
            pak.WritePascalString(GameServer.Instance.Configuration.ServerNameShort); //server name
            pak.WriteByte(0x05); //Server ID, seems irrelevant
            var type = color == 0 ? 7 : color;
            pak.WriteByte((byte) type); // 00 normal type?, 01 mordred type, 03 gaheris type, 07 ywain type
            pak.WriteByte(0x00); // Trial switch 0x00 - subbed, 0x01 - trial acc
            SendTCP(pak);
        }
    }

    public override void SendMessage(string msg, eChatType type, eChatLoc loc)
    {
        if (m_gameClient.ClientState == GameClient.eClientState.CharScreen)
            return;

        var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.Message));
        pak.WriteByte((byte) type);

        string str;
        if (loc == eChatLoc.CL_ChatWindow)
            str = "@@";
        else if (loc == eChatLoc.CL_PopupWindow)
            str = "##";
        else
            str = "";

        if (m_lastPacketSendTick + m_packetInterval < GameLoop.GameLoopTime) m_numPacketsSent = 0;

        //rate limit spell and damage messages
        //if (type == eChatType.CT_Spell || type == eChatType.CT_Damaged)
        //{
        //	if (m_numPacketsSent < m_packetCap)
        //	{
        //		pak.WriteString(str + msg);
        //		SendTCP(pak);
        //		m_numPacketsSent++;
        //		m_lastPacketSendTick = GameLoop.GameLoopTime;
        //	}				
        //} else
        //{
        pak.WriteString(str + msg);
        SendTCP(pak);
        //}
    }

    [Obsolete("Shouldn't be used in favor of new LoS Check Manager")]
    public override void SendCheckLOS(GameObject Checker, GameObject Target, CheckLOSResponse callback)
    {
        if (m_gameClient.Player == null)
            return;
        var TargetOID = Target != null ? Target.ObjectID : 0;
        var key = string.Format("LOS C:0x{0} T:0x{1}", Checker.ObjectID, TargetOID);
        CheckLOSResponse old_callback = null;
        lock (m_gameClient.Player.TempProperties)
        {
            old_callback = (CheckLOSResponse) m_gameClient.Player.TempProperties.getProperty<object>(key, null);
            m_gameClient.Player.TempProperties.setProperty(key, callback);
        }

        if (old_callback != null)
            old_callback(m_gameClient.Player, 0, 0); // not sure for this,  i want targetOID there

        using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.CheckLOSRequest)))
        {
            pak.WriteShort((ushort) Checker.ObjectID);
            pak.WriteShort((ushort) TargetOID);
            pak.WriteShort(0x00); // ?
            SendTCP(pak);
        }
    }

    public override void SendCheckLOS(GameObject source, GameObject target, CheckLOSMgrResponse callback)
    {
        if (m_gameClient.Player == null)
            return;

        var TargetOID = target != null ? target.ObjectID : 0;
        var SourceOID = source != null ? source.ObjectID : 0;

        var key = string.Format("LOSMGR C:0x{0} T:0x{1}", SourceOID, TargetOID);

        CheckLOSMgrResponse old_callback = null;
        lock (m_gameClient.Player.TempProperties)
        {
            old_callback = (CheckLOSMgrResponse) m_gameClient.Player.TempProperties.getProperty<object>(key, null);
            m_gameClient.Player.TempProperties.setProperty(key, callback);
        }

        if (old_callback != null)
            old_callback(m_gameClient.Player, 0, 0, 0);

        using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.CheckLOSRequest)))
        {
            pak.WriteShort((ushort) SourceOID);
            pak.WriteShort((ushort) TargetOID);
            pak.WriteShort(0x00); // ?
            SendTCP(pak);
        }
    }

    /// <summary>
    /// This is used to build a server side "Position Object"
    /// Usually Position Packet Should only be relayed
    /// This method can be used to refresh postion when there is lag or during a linkdeath to prevent models from disappearing
    /// </summary>
    /// <param name="player"></param>
    public override void SendPlayerForgedPosition(GamePlayer player)
    {
        using (var pak = new GSUDPPacketOut(GetPacketCode(eServerPackets.PlayerPosition)))
        {
            var newHeading = player.Heading;
            ushort steedSeatPosition = 0;

            if (player.Steed != null && player.Steed.ObjectState == GameObject.eObjectState.Active)
            {
                newHeading = (ushort) player.Steed.ObjectID;
                steedSeatPosition = (ushort) player.Steed.RiderSlot(player);
            }

            byte playerOutAction = 0x00;
            if (player.IsDiving)
                playerOutAction |= 0x04;
            if (player.TargetInView)
                playerOutAction |= 0x30;
            if (player.GroundTargetInView)
                playerOutAction |= 0x08;
            if (player.IsTorchLighted)
                playerOutAction |= 0x80;
            if (player.IsStealthed)
                playerOutAction |= 0x02;

            pak.WriteFloatLowEndian(player.X);
            pak.WriteFloatLowEndian(player.Y);
            pak.WriteFloatLowEndian(player.Z);
            pak.WriteFloatLowEndian(player.CurrentSpeed);
            pak.WriteFloatLowEndian(0); // z speed
            pak.WriteShort((ushort) player.Client.SessionID);
            pak.WriteShort((ushort) player.ObjectID);
            pak.WriteShort(player.CurrentZone.ZoneSkinID);
            pak.WriteShort(0); // player state
            pak.WriteShort(steedSeatPosition);
            pak.WriteShort(newHeading);
            pak.WriteByte(playerOutAction);
            pak.WriteByte((byte) (player.RPFlag ? 1 : 0));
            pak.WriteByte(0);
            pak.WriteByte((byte) (player.HealthPercent + (player.attackComponent.AttackState ? 0x80 : 0)));
            pak.WriteByte(player.ManaPercent);
            pak.WriteByte(player.EndurancePercent);
            pak.WriteByte(0);
            pak.WritePacketLength();

            SendUDP(pak);
        }

        // Update Cache
        m_gameClient.GameObjectUpdateArray[
            new Tuple<ushort, ushort>(player.CurrentRegionID, (ushort) player.ObjectID)] = GameTimer.GetTickCount();
    }
}