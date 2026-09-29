using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace AmongUsPCMod

{
internal static class IEnumerableExtensions
{
    private static readonly Random rng = new();

    internal static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection)
    {
        var list = collection.ToList();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
        return list;
    }

        internal static T? Random<T>(this IEnumerable<T?> collection) where T : class
    {
        if (collection == null || !collection.Any()) return null;

        var shuffled = collection.Where(item => item != null).Shuffle();
        return shuffled.FirstOrDefault();
    }

   
    internal static (T? element, int index) RandomIndex<T>(this IEnumerable<T?> collection) where T : class
    {
        if (collection == null || !collection.Any())
            return (null, -1);

        var indexedItems = collection
            .Select((item, index) => (item, index))
            .Where(x => x.item != null)
            .ToList();

        if (indexedItems.Count == 0)
            return (null, -1);

        var shuffled = indexedItems.Shuffle();
        var selected = shuffled.First();

        return (selected.item, selected.index);
    }

        internal static T Middle<T>(this IEnumerable<T> collection)
    {
        if (collection == null || !collection.Any())
            throw new InvalidOperationException("Collection cannot be null or empty.");

        int middleIndex = collection.Count() / 2;
        return collection.Skip(middleIndex).First();
    }
  }
}