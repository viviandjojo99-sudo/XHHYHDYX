#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XHHY.Game.Data;

namespace XHHY.Game.EditorTools
{
    public static class ValidateCardIds
    {
        [MenuItem("炼丹游戏/校验卡牌ID")]
        public static void Validate()
        {
            var errors = new List<string>();
            var attrCatalog = FindAsset<AttributeCatalog>();
            var seen = new HashSet<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:CardData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card == null) continue;

                if (string.IsNullOrEmpty(card.cardId))
                    errors.Add($"{path}: cardId 为空");
                else if (!seen.Add(card.cardId))
                    errors.Add($"{path}: 重复 cardId={card.cardId}");

                if (card.cardType == null)
                    errors.Add($"{path}: cardType 未指定");
                else if (string.IsNullOrEmpty(card.cardType.typeId))
                    errors.Add($"{path}: typeId 为空");

                if (card.attributes != null)
                {
                    for (int i = 0; i < card.attributes.Count; i++)
                    {
                        var a = card.attributes[i];
                        if (a == null || string.IsNullOrEmpty(a.attributeId))
                            continue;
                        if (attrCatalog != null && attrCatalog.FindById(a.attributeId) == null)
                            errors.Add($"{path}: 未知 attributeId={a.attributeId}");
                    }
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:EventData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ev = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (ev == null) continue;
                if (string.IsNullOrEmpty(ev.eventId))
                    errors.Add($"{path}: eventId 为空");
                var reqs = ev.ResolveRequirements();
                for (int i = 0; i < reqs.Count; i++)
                {
                    var r = reqs[i];
                    if (r == null) continue;
                    if (attrCatalog != null && attrCatalog.FindById(r.attributeId) == null)
                        errors.Add($"{path}: 未知需求 attributeId={r.attributeId}");
                }
                if (ev.outcomes == null || ev.outcomes.Count == 0)
                    errors.Add($"{path}: 无结果分支（建议至少一条兜底）");
                else
                {
                    var last = ev.outcomes[ev.outcomes.Count - 1];
                    if (last != null && last.conditions != null && last.conditions.Count > 0)
                        errors.Add($"{path}: 末条分支仍有条件，建议无条件兜底");
                }
            }

            if (errors.Count == 0)
                Debug.Log("校验通过：零错误。");
            else
            {
                Debug.LogError($"校验失败：{errors.Count} 条\n" + string.Join("\n", errors));
            }
        }

        static T FindAsset<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
#endif
