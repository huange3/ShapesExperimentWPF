﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ShapesExperimentWPF
{
    public class Phase
    {
        public char Label { get; set; }
        public Color BackgroundColor { get; set; }
        public int TrialCount { get; set; }
        public int Observations { get; set; } = 0;
        public decimal Density { get; set; } = 0;
        public int RankType { get; set; } = 0;
        public List<Trial> Trials { get; set; }

        public Phase(char label, Color color, int m, decimal w, int rank, int trialCnt)
        {
            this.Label = label;
            this.BackgroundColor = color;
            this.Observations = m;
            this.Density = w;
            this.RankType = rank;
            this.TrialCount = trialCnt; 
            this.Trials = new List<Trial>();
        }
    }
}
