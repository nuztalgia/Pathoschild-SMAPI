using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="IWorldEvents.NpcListChanged"/> event.</summary>
    public class WorldNpcListChangedEventArgs : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The location which changed.</summary>
        public GameLocation Location { get; }

        /// <summary>The NPCs added to the location.</summary>
        public IEnumerable<NPC> Added { get; }

        /// <summary>The NPCs removed from the location.</summary>
        public IEnumerable<NPC> Removed { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="location">The location which changed.</param>
        /// <param name="added">The NPCs added to the location.</param>
        /// <param name="removed">The NPCs removed from the location.</param>
        public WorldNpcListChangedEventArgs(GameLocation location, IEnumerable<NPC> added, IEnumerable<NPC> removed)
        {
            this.Location = location;
            this.Added = added.ToArray();
            this.Removed = removed.ToArray();
        }
    }
}
