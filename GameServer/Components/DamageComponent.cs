﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class DamageComponent
    {
        //entity casting the spell
        public GameLiving owner;

        public int DamageToDeal = 0;

        //might be better moved to DamageOverTimeComponent
        public bool isRepeating = false;
        public int damageIntervalInMs = 0;


        public DamageComponent(GameLiving owner, int damage)
        {
            this.owner = owner;
            this.DamageToDeal = damage;
        }
    }
}
