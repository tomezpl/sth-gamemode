using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurviveTheHuntServer.Utils
{
    /// <summary>
    /// Wrapper for a <see cref="CitizenFX.Core.Vector3"/> position component combined with the direction (heading).
    /// </summary>
    public class Coord
    {
        public CitizenFX.Core.Vector3 Position { get; set; } = new CitizenFX.Core.Vector3();

        /// <summary>
        /// Direction (angle) of the coord on the XY plane.
        /// </summary>
        public float Heading { get; set; } = 0f;

        public Coord()
        {
            Position = CitizenFX.Core.Vector3.Zero;
            Heading = 0f;
        }

        public Coord(CitizenFX.Core.Vector3 position, float heading = 0f)
        {
            Position = position;
            Heading = heading;
        }
    }
}
