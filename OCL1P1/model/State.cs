﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCL1P1.model
{
    class State
    {
        private string stateName;
        private bool isEnd;

        public State(string stateName)
        {
            this.StateName = stateName;
            IsEnd = false;
        }

        public string StateName { get => stateName; set => stateName = value; }
        public bool IsEnd { get => isEnd; set => isEnd = value; }
    }
}
