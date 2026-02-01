using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstroBot.RetroPlanet
{
    public class RetroPlanetResult
    {
        public string PlanetName { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public bool IsCurrentMounth { get; set; }
    }
}
