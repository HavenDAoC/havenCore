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
using System.Collections.Generic;
using System.Text;
using DOL.GS;

namespace DOL.Events;

internal class PetSpellEventArgs : EventArgs
{
    private Spell m_spell;
    private SpellLine m_spellLine;
    private GameLiving m_target;
    private Spell m_parentSpell;

    public PetSpellEventArgs(Spell spell, SpellLine spellLine, GameLiving target)
    {
        m_spell = spell;
        m_spellLine = spellLine;
        m_target = target;
    }

    public PetSpellEventArgs(Spell spell, SpellLine spellLine, GameLiving target, Spell parentSpell)
    {
        m_spell = spell;
        m_spellLine = spellLine;
        m_target = target;
        m_parentSpell = parentSpell;
    }

    public Spell Spell => m_spell;

    public SpellLine SpellLine => m_spellLine;

    public GameLiving Target => m_target;

    public Spell ParentSpell => m_parentSpell;
}