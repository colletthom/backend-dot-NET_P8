﻿using RewardCentral.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RewardCentral;

public class RewardCentral
{
    public int GetAttractionRewardPoints(Guid attractionId, Guid userId)
    {
        int randomDelay = new Random().Next(1, 1000);
        //Après avoir retiré le Thread.Sleep(randomDelay) je l'ai remis...cela fait surement parti de l'exercice
        Thread.Sleep(randomDelay);

        int randomInt = new Random().Next(1, 1000);
        return randomInt;
    }
}
