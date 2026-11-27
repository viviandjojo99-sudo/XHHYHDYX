using System;
using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    public enum RecipeQuality
    {
        Fail = 0,
        Mid = 1,
        High = 2,
        Explode = 3
    }

    [Serializable]
    public class RecipeAttributeBand
    {
        public string attributeId;
        public int min;
        public int max = 99;
    }

    [Serializable]
    public class RecipeData
    {
        public List<RecipeAttributeBand> bands = new List<RecipeAttributeBand>();
        public List<string> requiredHerbTypeIds = new List<string>();
        public CardData highOutput;
        public CardData midOutput;
        public CardData failOutput;
        public string explodeAttributeId = "fire";

        public RecipeQuality Evaluate(Dictionary<string, int> totals)
        {
            if (bands == null || bands.Count == 0)
                return RecipeQuality.Mid;

            bool anyBelow = false;
            bool allInBand = true;
            bool explode = false;

            for (int i = 0; i < bands.Count; i++)
            {
                var band = bands[i];
                if (band == null || string.IsNullOrEmpty(band.attributeId))
                    continue;
                totals.TryGetValue(band.attributeId, out int v);
                if (v < band.min)
                    anyBelow = true;
                if (v < band.min || v > band.max)
                    allInBand = false;
                if (!string.IsNullOrEmpty(explodeAttributeId)
                    && band.attributeId == explodeAttributeId
                    && v > band.max)
                    explode = true;
            }

            if (explode)
                return RecipeQuality.Explode;
            if (anyBelow)
                return RecipeQuality.Fail;
            if (allInBand)
                return RecipeQuality.High;
            return RecipeQuality.Mid;
        }

        public CardData OutputFor(RecipeQuality quality)
        {
            switch (quality)
            {
                case RecipeQuality.High: return highOutput;
                case RecipeQuality.Mid: return midOutput;
                case RecipeQuality.Fail: return failOutput;
                default: return null;
            }
        }
    }
}
