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

using System;
using DOL.Database.Attributes;

namespace DOL.Database;

/// <summary>
/// Database Storage of ServerStats
/// </summary>
[DataTable(TableName = "serverstats")]
public class ServerStats : DataObject
{
    protected DateTime m_statdate;
    protected int m_clients;
    protected float m_cpu;
    protected int m_upload;
    protected int m_download;
    protected long m_memory;
    protected int m_palbion;
    protected int m_pmidgard;
    protected int m_phibernia;

    public ServerStats()
    {
        m_statdate = DateTime.Now;
        m_clients = 0;
        m_cpu = 0;
        m_upload = 0;
        m_download = 0;
        m_memory = 0;
        m_palbion = 0;
        m_pmidgard = 0;
        m_phibernia = 0;
    }

    [DataElement(AllowDbNull = false)]
    public DateTime StatDate
    {
        get => m_statdate;
        set
        {
            Dirty = true;
            m_statdate = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public int Clients
    {
        get => m_clients;
        set
        {
            Dirty = true;
            m_clients = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public float CPU
    {
        get => m_cpu;
        set
        {
            Dirty = true;
            m_cpu = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public int Upload
    {
        get => m_upload;
        set
        {
            Dirty = true;
            m_upload = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public int Download
    {
        get => m_download;
        set
        {
            Dirty = true;
            m_download = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public long Memory
    {
        get => m_memory;
        set
        {
            Dirty = true;
            m_memory = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public int AlbionPlayers
    {
        get => m_palbion;
        set
        {
            Dirty = true;
            m_palbion = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public int MidgardPlayers
    {
        get => m_pmidgard;
        set
        {
            Dirty = true;
            m_pmidgard = value;
        }
    }

    [DataElement(AllowDbNull = false)]
    public int HiberniaPlayers
    {
        get => m_phibernia;
        set
        {
            Dirty = true;
            m_phibernia = value;
        }
    }
}