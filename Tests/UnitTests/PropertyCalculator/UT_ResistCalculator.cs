﻿using DOL.GS;
using DOL.GS.PropertyCalc;
using NUnit.Framework;

namespace DOL.Tests.Unit.Gameserver.PropertyCalc;

[TestFixture]
internal class UT_ResistCalculator
{
    [Test]
    public void CalcValue_50ConBuff_6()
    {
        var npc = NewNPC();
        npc.BaseBuffBonusCategory[eProperty.Constitution] = 50;

        var actual = ResistCalculator.CalcValue(npc, SomeResistProperty);

        Assert.AreEqual(6, actual);
    }

    [Test]
    public void CalcValue_50ConDebuff_Minus6()
    {
        var npc = NewNPC();
        npc.DebuffCategory[eProperty.Constitution] = 50;

        var actual = ResistCalculator.CalcValue(npc, SomeResistProperty);

        Assert.AreEqual(-6, actual);
    }

    private ResistCalculator ResistCalculator => new();

    private FakeNPC NewNPC()
    {
        return new();
    }

    private eProperty SomeResistProperty => eProperty.Resist_First;
}