// MIT License
// Based off Henry's Source importer https://github.com/Henry00IS/Chisel.Import.Source

using System.Collections.Generic;

namespace Quixotic7.Chisel.Import.Q1
{
    /// <summary>
    /// Represents a Quake 1 Entity.
    /// </summary>
    public class MapEntity
    {
        /// <summary>
        /// The class name of the entity.
        /// </summary>
        public string ClassName;

        public string tbType;
        public string tbName;
        public int tbId = -1;
        public int tbLayerSortIndex = -1;
        public int tbGroup = -1;
        public int tbLayer = -1;

        /// <summary>
        /// The brushes in the entity if available.
        /// </summary>
        public List<MapBrush> Brushes = new List<MapBrush>();

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Quake 1 Entity " + ClassName + " (" + Brushes.Count + " Brushes)";
        }
    }
}