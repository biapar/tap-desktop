﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TheAirline.Model.AirlinerModel
{
    //the class for an airliner order
    public class AirlinerOrder
    {
        public List<AirlinerClass> Classes { get; set; }
        public AirlinerType Type { get; set; }
        public int Amount { get; set; }
        public AirlinerOrder(AirlinerType type, List<AirlinerClass> classes, int amount)
        {
            this.Type = type;
            this.Amount = amount;
            this.Classes = classes;
        }
    }
}