﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using StardewValley.Projectiles;

namespace StardewModdingAPI.Metadata
{
    /// <summary>Provides metadata about core assets in the game.</summary>
    internal class CoreAssets
    {
        /*********
        ** Properties
        *********/
        /// <summary>Normalises an asset key to match the cache key.</summary>
        protected readonly Func<string, string> GetNormalisedPath;

        /// <summary>The static asset setters.</summary>
        private readonly IDictionary<string, Action<SContentManager, string>> StaticSetters;


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the core asset data.</summary>
        /// <param name="getNormalisedPath">Normalises an asset key to match the cache key.</param>
        public CoreAssets(Func<string, string> getNormalisedPath)
        {
            this.GetNormalisedPath = getNormalisedPath;
            this.StaticSetters =
                new Dictionary<string, Action<SContentManager, string>>
                {
                    // from Game1.loadContent
                    ["LooseSprites\\daybg"] = (content, key) => Game1.daybg = content.Load<Texture2D>(key),
                    ["LooseSprites\\daybg"] = (content, key) => Game1.daybg = content.Load<Texture2D>(key),
                    ["LooseSprites\\nightbg"] = (content, key) => Game1.nightbg = content.Load<Texture2D>(key),
                    ["Maps\\MenuTiles"] = (content, key) => Game1.menuTexture = content.Load<Texture2D>(key),
                    ["LooseSprites\\Lighting\\lantern"] = (content, key) => Game1.lantern = content.Load<Texture2D>(key),
                    ["LooseSprites\\Lighting\\windowLight"] = (content, key) => Game1.windowLight = content.Load<Texture2D>(key),
                    ["LooseSprites\\Lighting\\sconceLight"] = (content, key) => Game1.sconceLight = content.Load<Texture2D>(key),
                    ["LooseSprites\\Lighting\\greenLight"] = (content, key) => Game1.cauldronLight = content.Load<Texture2D>(key),
                    ["LooseSprites\\Lighting\\indoorWindowLight"] = (content, key) => Game1.indoorWindowLight = content.Load<Texture2D>(key),
                    ["LooseSprites\\shadow"] = (content, key) => Game1.shadowTexture = content.Load<Texture2D>(key),
                    ["LooseSprites\\Cursors"] = (content, key) => Game1.mouseCursors = content.Load<Texture2D>(key),
                    ["LooseSprites\\ControllerMaps"] = (content, key) => Game1.controllerMaps = content.Load<Texture2D>(key),
                    ["TileSheets\\animations"] = (content, key) => Game1.animations = content.Load<Texture2D>(key),
                    ["Data\\Achievements"] = (content, key) => Game1.achievements = content.Load<Dictionary<int, string>>(key),
                    ["Data\\NPCGiftTastes"] = (content, key) => Game1.NPCGiftTastes = content.Load<Dictionary<string, string>>(key),
                    ["Fonts\\SpriteFont1"] = (content, key) => Game1.dialogueFont = content.Load<SpriteFont>(key),
                    ["Fonts\\SmallFont"] = (content, key) => Game1.smallFont = content.Load<SpriteFont>(key),
                    ["Fonts\\tinyFont"] = (content, key) => Game1.tinyFont = content.Load<SpriteFont>(key),
                    ["Fonts\\tinyFontBorder"] = (content, key) => Game1.tinyFontBorder = content.Load<SpriteFont>(key),
                    ["Maps\\springobjects"] = (content, key) => Game1.objectSpriteSheet = content.Load<Texture2D>(key),
                    ["TileSheets\\crops"] = (content, key) => Game1.cropSpriteSheet = content.Load<Texture2D>(key),
                    ["TileSheets\\emotes"] = (content, key) => Game1.emoteSpriteSheet = content.Load<Texture2D>(key),
                    ["TileSheets\\debris"] = (content, key) => Game1.debrisSpriteSheet = content.Load<Texture2D>(key),
                    ["TileSheets\\Craftables"] = (content, key) => Game1.bigCraftableSpriteSheet = content.Load<Texture2D>(key),
                    ["TileSheets\\rain"] = (content, key) => Game1.rainTexture = content.Load<Texture2D>(key),
                    ["TileSheets\\BuffsIcons"] = (content, key) => Game1.buffsIcons = content.Load<Texture2D>(key),
                    ["Data\\ObjectInformation"] = (content, key) => Game1.objectInformation = content.Load<Dictionary<int, string>>(key),
                    ["Data\\BigCraftablesInformation"] = (content, key) => Game1.bigCraftablesInformation = content.Load<Dictionary<int, string>>(key),
                    ["Characters\\Farmer\\hairstyles"] = (content, key) => FarmerRenderer.hairStylesTexture = content.Load<Texture2D>(key),
                    ["Characters\\Farmer\\shirts"] = (content, key) => FarmerRenderer.shirtsTexture = content.Load<Texture2D>(key),
                    ["Characters\\Farmer\\hats"] = (content, key) => FarmerRenderer.hatsTexture = content.Load<Texture2D>(key),
                    ["Characters\\Farmer\\accessories"] = (content, key) => FarmerRenderer.accessoriesTexture = content.Load<Texture2D>(key),
                    ["TileSheets\\furniture"] = (content, key) => Furniture.furnitureTexture = content.Load<Texture2D>(key),
                    ["LooseSprites\\font_bold"] = (content, key) => SpriteText.spriteTexture = content.Load<Texture2D>(key),
                    ["LooseSprites\\font_colored"] = (content, key) => SpriteText.coloredTexture = content.Load<Texture2D>(key),
                    ["TileSheets\\weapons"] = (content, key) => Tool.weaponsTexture = content.Load<Texture2D>(key),
                    ["TileSheets\\Projectiles"] = (content, key) => Projectile.projectileSheet = content.Load<Texture2D>(key),

                    // from Farmer constructor
                    ["Characters\\Farmer\\farmer_base"] = (content, key) =>
                    {
                        if (Game1.player != null && Game1.player.isMale)
                            Game1.player.FarmerRenderer = new FarmerRenderer(content.Load<Texture2D>(key));
                    },
                    ["Characters\\Farmer\\farmer_girl_base"] = (content, key) =>
                    {
                        if (Game1.player != null && !Game1.player.isMale)
                            Game1.player.FarmerRenderer = new FarmerRenderer(content.Load<Texture2D>(key));
                    }
                }
                .ToDictionary(p => getNormalisedPath(p.Key), p => p.Value);
        }

        /// <summary>Reload one of the game's core assets (if applicable).</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether an asset was reloaded.</returns>
        public bool ReloadForKey(SContentManager content, string key)
        {
            // static assets
            if (this.StaticSetters.TryGetValue(key, out Action<SContentManager, string> reload))
            {
                reload(content, key);
                return true;
            }

            return false;
        }
    }
}
