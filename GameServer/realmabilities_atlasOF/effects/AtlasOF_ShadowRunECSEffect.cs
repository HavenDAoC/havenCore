using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects;

public class AtlasOF_ShadowRunECSEffect : ECSGameAbilityEffect
{
    public new SpellHandler SpellHandler;

    public AtlasOF_ShadowRunECSEffect(ECSGameEffectInitParams initParams)
        : base(initParams)
    {
        EffectType = eEffect.ShadowRun;
        EffectService.RequestStartEffect(this);
    }

    public override void OnStartEffect()
    {
        base.OnStartEffect();
        if (Owner is GamePlayer p) p.Out.SendUpdateMaxSpeed();
    }

    public override void OnStopEffect()
    {
        base.OnStopEffect();
        if (Owner is GamePlayer p) p.Out.SendUpdateMaxSpeed();
    }

    public override ushort Icon => 4278;

    public override string Name => "Shadow Run";

    public override bool HasPositiveEffect => true;
}