using SkillServiceLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models
{
    public class ComcastPointOfInterestResponse
    {
        public List<PointOfInterestModelSlim> Results { get; set; }

        public string Municipality { get; set; }
    }
}
