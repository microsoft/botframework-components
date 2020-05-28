using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackToWorkSkill.Models
{
    public class BackToWorkSkillState
    {
        public bool IsAction { get; set; }

        public string Temperature { get; set; }

        public bool Fever { get; set; }

        public bool Cough { get; set; }

        public bool ShortnessOfBreath { get; set; }

        public bool Chills { get; set; }

        public bool MusclePain { get; set; }

        public bool SoreThroat { get; set; }

        public bool LossOfSmell { get; set; }

        public bool LossOfTaste { get; set; }

        public bool Vomitting { get; set; }

        public bool Diarrhea { get; set; }

        public bool NoSymptoms { get; set; }

        public bool Symptomatic { get; set; }

        public bool SymptomFree { get; set; }
    }
}
