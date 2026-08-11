// Author: František Holubec
// Created: 11.08.2026

#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.GeoToolkit.Coordinates
{
    public static class CoordinatesClipboardUtility
    {
        private const string MGRS_NAME = "MGRS";
        private const string GEOGRAPHIC_FORMAT = "0.#######";
        private const string PROJECTED_FORMAT = "0.###";

        public static void ShowCopyDropdown(GeoCoords coords, Rect dropdownRect)
        {
            if (coords.CoordinateSystem == CoordinateSystemType.Unknown)
            {
                Debug.LogError("Cannot convert coordinates of an unknown coordinate system");
                return;
            }

            var names = Enum.GetValues(typeof(CoordinateSystemType))
                .Cast<CoordinateSystemType>()
                .Where(system => system != CoordinateSystemType.Unknown)
                .Select(system => system.ToName())
                .Append(MGRS_NAME);

            var selector = new GenericSelector<string>("Copy Coordinates", false, x => x, names);
            selector.SetSelection(coords.CoordinateSystem.ToName());
            selector.SelectionTree.Config.DrawSearchToolbar = true;
            selector.SelectionTree.Config.AutoFocusSearchBar = true;
            selector.EnableSingleClickToSelect();
            selector.SelectionConfirmed += selection => Copy(Resolve(coords, selection.FirstOrDefault()));
            selector.ShowInPopup(dropdownRect);
        }

        private static string Resolve(GeoCoords coords, string name)
        {
            if (name == MGRS_NAME)
                return coords.ConvertToMilitaryGrid().Value;

            return CoordinateSystemTypeUtility.TryParse(name, out var targetSystem) ? Format(coords, targetSystem) : null;
        }

        private static string Format(GeoCoords coords, CoordinateSystemType targetSystem)
        {
            var position = coords.ConvertTo(targetSystem).Position;
            var geographic = targetSystem.IsGeographic();
            var format = geographic ? GEOGRAPHIC_FORMAT : PROJECTED_FORMAT;
            var (first, second) = geographic ? (position.y, position.x) : (position.x, position.y);
            return $"{first.ToString(format, CultureInfo.InvariantCulture)}, {second.ToString(format, CultureInfo.InvariantCulture)}";
        }

        private static void Copy(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogError("Failed to convert coordinates");
                return;
            }

            GUIUtility.systemCopyBuffer = value;
            Debug.Log($"Copied coordinates: {value}");
        }
    }
}
#endif
